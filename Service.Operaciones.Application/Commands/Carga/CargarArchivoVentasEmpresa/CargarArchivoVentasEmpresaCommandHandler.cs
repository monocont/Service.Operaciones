using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using VentaEmpresaEntity = Service.Operaciones.Domain.Entities.VentaEmpresa;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoVentasEmpresa;

public class CargarArchivoVentasEmpresaCommandHandler : IRequestHandler<CargarArchivoVentasEmpresaCommand, CargarArchivoSunatDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IHashService _hashService;
    private readonly IVentaEmpresaParser _parser;
    private readonly IVentaEmpresaValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoVentasEmpresaCommandHandler> _logger;

    private const TipoOperacion Tipo = TipoOperacion.VentaEmpresa;

    public CargarArchivoVentasEmpresaCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaEmpresaRepository ventaEmpresaRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IHashService hashService,
        IVentaEmpresaParser parser,
        IVentaEmpresaValidationService validationService,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoVentasEmpresaCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaEmpresaRepo = ventaEmpresaRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _hashService = hashService;
        _parser = parser;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoVentasEmpresaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo VENTAS EMPRESA. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
            request.EmpresaRuc, request.Periodo);

        // 1. Validar empresa y multi-tenant
        if (!await _empresaService.ExisteEmpresaAsync(request.EmpresaRuc, cancellationToken))
        {
            throw new ValidationException($"La empresa con RUC {request.EmpresaRuc} no está registrada en el sistema");
        }

        await _accesoValidator.ValidarAccesoAsync(request.EmpresaRuc, cancellationToken);

        // 1.1 Validar si ya existe carga previa para el mismo periodo y empresa
        if (await _archivoCargaRepo.ExisteCargaAsync(request.EmpresaRuc, request.Periodo, Tipo, request.Usuario, cancellationToken))
        {
            throw new ValidationException(
                $"Ya existe una carga registrada de Ventas de Empresa para el RUC {request.EmpresaRuc} en el periodo {request.Periodo}. Elimina la carga previa si deseas reemplazarla.");
        }

        // 2. Determinar formato
        var ext = Path.GetExtension(request.NombreArchivo).ToLower();
        var formato = ext switch
        {
            ".xlsx" => FormatoArchivo.Xlsx,
            ".xls" => FormatoArchivo.Xls,
            ".csv" => FormatoArchivo.Csv,
            _ => FormatoArchivo.Xlsx
        };

        // 3. Hash SHA256
        var hash = await _hashService.CalcularSha256Async(request.ArchivoStream, cancellationToken);
        request.ArchivoStream.Position = 0;

        // 4. Iniciar transacción Unit of Work
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 5. Crear cabecera ArchivoCarga
            var archivoCarga = ArchivoCarga.Crear(
                request.EmpresaRuc, request.Periodo, Tipo, formato,
                request.NombreArchivo, hash, request.Usuario);

            await _archivoCargaRepo.AgregarAsync(archivoCarga, cancellationToken);

            // 6. Parsear archivo Excel
            var resultados = await _parser.ParsearAsync(request.ArchivoStream, cancellationToken);

            if (resultados.Count == 0)
            {
                var errorVacio = ArchivoCargaError.Crear(
                    archivoCarga.IdCarga, 1, TipoErrorCarga.Formato,
                    "El archivo no contiene filas de datos o está vacío",
                    severidad: SeveridadError.Error);

                await _archivoCargaErrorRepo.AgregarRangoAsync(new List<ArchivoCargaError> { errorVacio }, cancellationToken);
                archivoCarga.ActualizarConteoYMontos(0, 0, 1, 0, 0, 0);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new CargarArchivoSunatDTO
                {
                    IdCarga = archivoCarga.IdCarga,
                    EmpresaRuc = request.EmpresaRuc,
                    Periodo = request.Periodo,
                    TipoOperacion = Tipo,
                    Formato = formato,
                    NombreOriginal = request.NombreArchivo,
                    Estado = EstadoCarga.Error,
                    NumRegistros = 0,
                    NumRegistrosValidos = 0,
                    NumRegistrosError = 1,
                    TotalGeneral = 0,
                    Observaciones = "El archivo cargado está vacío."
                };
            }

            var erroresParseo = new List<ArchivoCargaError>();
            var entidadesValidas = new List<VentaEmpresaEntity>();
            decimal totalGeneral = 0;

            foreach (var r in resultados)
            {
                if (!r.EsValido)
                {
                    erroresParseo.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga,
                        r.NumeroLinea,
                        TipoErrorCarga.Formato,
                        r.ErrorMensaje ?? "Error de formato en la línea",
                        campoError: r.CampoError,
                        severidad: SeveridadError.Error));
                }
                else
                {
                    var entidad = VentaEmpresaEntity.Crear(
                        archivoCarga.IdCarga,
                        request.EmpresaRuc,
                        request.Periodo,
                        r.NumeroLinea,
                        r.FechaEmision!.Value,
                        r.CodigoTipoCp ?? "01",
                        r.Serie ?? "",
                        r.Numero ?? "",
                        r.CodigoTipoDocIdentidad ?? "0",
                        r.NroDocIdentidad ?? "-",
                        r.TotalCp ?? 0,
                        r.CodigoMoneda ?? "PEN",
                        r.TipoCambio ?? 1.0000m,
                        request.Usuario);

                    entidadesValidas.Add(entidad);
                    totalGeneral += r.TotalCp ?? 0;
                }
            }

            // 7. Guardar errores de parseo
            if (erroresParseo.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresParseo, cancellationToken);
            }

            // 8. Validaciones de Negocio e Integridad
            var erroresNegocio = await _validationService.ValidarVentasEmpresaAsync(
                archivoCarga.IdCarga, request.EmpresaRuc, request.Periodo, entidadesValidas, cancellationToken);

            if (erroresNegocio.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresNegocio, cancellationToken);
            }

            // 9. Persistir registros de ventas válidos en memoria
            if (entidadesValidas.Count > 0)
            {
                await _ventaEmpresaRepo.AgregarRangoAsync(entidadesValidas, cancellationToken);
            }

            // 10. Actualizar métricas en la cabecera
            var totalErrores = erroresParseo.Count + erroresNegocio.Count;
            archivoCarga.ActualizarConteoYMontos(
                resultados.Count,
                entidadesValidas.Count,
                totalErrores,
                0,
                0,
                totalGeneral);

            // 11. Commit final y atómico a la base de datos
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new CargarArchivoSunatDTO
            {
                IdCarga = archivoCarga.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TipoOperacion = Tipo,
                Formato = formato,
                NombreOriginal = request.NombreArchivo,
                Estado = totalErrores > 0 ? EstadoCarga.Ok : EstadoCarga.Ok,
                NumRegistros = resultados.Count,
                NumRegistrosValidos = entidadesValidas.Count,
                NumRegistrosError = totalErrores,
                TotalGeneral = totalGeneral,
                Observaciones = totalErrores > 0
                    ? $"Carga completada con {totalErrores} observaciones detectadas."
                    : "Carga completada exitosamente sin errores."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la carga de ventas empresa para {EmpresaRuc} - {Periodo}",
                request.EmpresaRuc, request.Periodo);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
