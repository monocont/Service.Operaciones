using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using CompraEmpresaEntity = Service.Operaciones.Domain.Entities.CompraEmpresa;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoComprasEmpresa;

public class CargarArchivoComprasEmpresaCommandHandler : IRequestHandler<CargarArchivoComprasEmpresaCommand, CargarArchivoSunatDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IHashService _hashService;
    private readonly ICompraEmpresaParser _parser;
    private readonly ICompraEmpresaValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoComprasEmpresaCommandHandler> _logger;

    private const TipoOperacion Tipo = TipoOperacion.CompraEmpresa;

    public CargarArchivoComprasEmpresaCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraEmpresaRepository compraEmpresaRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IHashService hashService,
        ICompraEmpresaParser parser,
        ICompraEmpresaValidationService validationService,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoComprasEmpresaCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraEmpresaRepo = compraEmpresaRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _hashService = hashService;
        _parser = parser;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoComprasEmpresaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo COMPRAS EMPRESA. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
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
                $"Ya existe una carga registrada de Compras de Empresa para el RUC {request.EmpresaRuc} en el periodo {request.Periodo}. Elimina la carga previa si deseas reemplazarla.");
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
            var entidadesValidas = new List<CompraEmpresaEntity>();
            decimal totalBi = 0;
            decimal totalIgv = 0;
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
                    var entidad = CompraEmpresaEntity.Crear(
                        idCarga: archivoCarga.IdCarga,
                        empresaRuc: request.EmpresaRuc,
                        periodo: request.Periodo,
                        numeroLinea: r.NumeroLinea,
                        fechaEmision: r.FechaEmision!.Value,
                        codigoTipoCp: r.CodigoTipoCp ?? "01",
                        serie: r.Serie ?? "",
                        numero: r.Numero ?? "",
                        codigoTipoDocIdentidad: r.CodigoTipoDocIdentidad ?? "6",
                        nroDocIdentidad: r.NroDocIdentidad ?? "-",
                        totalCp: r.TotalCp ?? 0,
                        codigoMoneda: r.CodigoMoneda ?? "PEN",
                        tipoCambio: r.TipoCambio ?? 1.0000m,
                        usuarioCreacion: request.Usuario,
                        razonSocial: r.RazonSocial ?? string.Empty,
                        carSunat: r.CarSunat,
                        fechaVencimiento: r.FechaVencimiento,
                        anioDocumento: r.AnioDocumento,
                        numeroFinal: r.NumeroFinal,
                        biGravadoDg: r.BiGravadoDg ?? 0,
                        igvIpmDg: r.IgvIpmDg ?? 0,
                        biGravadoDgng: r.BiGravadoDgng ?? 0,
                        igvIpmDgng: r.IgvIpmDgng ?? 0,
                        biGravadoDng: r.BiGravadoDng ?? 0,
                        igvIpmDng: r.IgvIpmDng ?? 0,
                        valorAdqNg: r.ValorAdqNg ?? 0,
                        montoIsc: r.MontoIsc ?? 0,
                        montoIcbper: r.MontoIcbper ?? 0,
                        montoOtrosTributos: r.MontoOtrosTributos ?? 0,
                        fechaEmisionDocModificado: r.FechaEmisionDocModificado,
                        codigoTipoCpModificado: r.CodigoTipoCpModificado,
                        serieCpModificado: r.SerieCpModificado,
                        codDamDsi: r.CodDamDsi,
                        numeroCpModificado: r.NumeroCpModificado,
                        clasifBssSss: r.ClasifBssSss,
                        idProyectoOp: r.IdProyectoOp,
                        porcPart: r.PorcPart,
                        imb: r.Imb ?? 0,
                        carOrigIndEI: r.CarOrigIndEI,
                        detraccion: r.Detraccion,
                        codigoTipoNota: r.CodigoTipoNota,
                        codigoEstadoComprobante: r.CodigoEstadoComprobante ?? "1",
                        incal: r.Incal,
                        camposLibres: r.CamposLibres);

                    entidadesValidas.Add(entidad);
                    totalBi += (r.BiGravadoDg ?? 0) + (r.BiGravadoDgng ?? 0) + (r.BiGravadoDng ?? 0);
                    totalIgv += (r.IgvIpmDg ?? 0) + (r.IgvIpmDgng ?? 0) + (r.IgvIpmDng ?? 0);
                    totalGeneral += r.TotalCp ?? 0;
                }
            }

            // 7. Guardar errores de parseo
            if (erroresParseo.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresParseo, cancellationToken);
            }

            // 8. Validaciones de Negocio e Integridad
            var erroresNegocio = await _validationService.ValidarComprasEmpresaAsync(
                archivoCarga.IdCarga, request.EmpresaRuc, request.Periodo, entidadesValidas, cancellationToken);

            if (erroresNegocio.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresNegocio, cancellationToken);
            }

            // 9. Persistir registros de compras válidos
            if (entidadesValidas.Count > 0)
            {
                await _compraEmpresaRepo.AgregarRangoAsync(entidadesValidas, cancellationToken);
            }

            // 10. Actualizar métricas en la cabecera
            var totalErrores = erroresParseo.Count + erroresNegocio.Count;
            archivoCarga.ActualizarConteoYMontos(
                resultados.Count,
                entidadesValidas.Count,
                totalErrores,
                totalBi,
                totalIgv,
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
            _logger.LogError(ex, "Error al procesar la carga de compras empresa para {EmpresaRuc} - {Periodo}",
                request.EmpresaRuc, request.Periodo);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
