using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using VentaEntity = Service.Operaciones.Domain.Entities.Venta;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoVentas;

public class CargarArchivoVentasCommandHandler : IRequestHandler<CargarArchivoVentasCommand, CargarArchivoSunatDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaRepository _ventaRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IHashService _hashService;
    private readonly IArchivoSunatParserFactory _parserFactory;
    private readonly IVentaValidationService _ventaValidationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoVentasCommandHandler> _logger;

    private const TipoArchivo Tipo = TipoArchivo.Ventas;

    public CargarArchivoVentasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaRepository ventaRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IHashService hashService,
        IArchivoSunatParserFactory parserFactory,
        IVentaValidationService ventaValidationService,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoVentasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaRepo = ventaRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _hashService = hashService;
        _parserFactory = parserFactory;
        _ventaValidationService = ventaValidationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoVentasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo VENTAS. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
            request.EmpresaRuc, request.Periodo);

        // 1. Validar empresa y acceso del usuario (multi-tenant)
        if (!await _empresaService.ExisteEmpresaAsync(request.EmpresaRuc, cancellationToken))
        {
            throw new ValidationException($"La empresa con RUC {request.EmpresaRuc} no esta registrada en el sistema");
        }

        await _accesoValidator.ValidarAccesoAsync(request.EmpresaRuc, cancellationToken);

        // 1.1 Validar si ya existe un registro de carga para el mismo periodo, empresa, tipo y usuario
        if (await _archivoCargaRepo.ExisteCargaAsync(request.EmpresaRuc, request.Periodo, Tipo, request.Usuario, cancellationToken))
        {
            throw new ValidationException(
                $"Ya existe una carga registrada de {Tipo} para la empresa con RUC {request.EmpresaRuc} en el periodo {request.Periodo}. No es posible volver a cargar dicho periodo.");
        }

        // 2. Determinar formato
        var formato = Path.GetExtension(request.NombreArchivo).ToLower() == ".txt"
            ? FormatoArchivo.Txt
            : FormatoArchivo.Csv;

        // 3. Calcular hash
        var hash = await _hashService.CalcularSha256Async(request.ArchivoStream, cancellationToken);
        request.ArchivoStream.Position = 0;

        // 4. Verificar duplicados por hash
        var duplicado = await _archivoCargaRepo.ObtenerDuplicadoAsync(
            request.EmpresaRuc, request.Periodo, Tipo, hash, cancellationToken);

        if (duplicado is not null)
        {
            return new CargarArchivoSunatDTO
            {
                IdCarga = duplicado.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TipoArchivo = Tipo,
                Formato = formato,
                NombreOriginal = request.NombreArchivo,
                Estado = EstadoCarga.Duplicado,
                NumRegistros = duplicado.NumRegistros,
                NumRegistrosValidos = duplicado.NumRegistrosValidos,
                NumRegistrosError = duplicado.NumRegistrosError,
                Observaciones = $"Archivo ya cargado anteriormente el {duplicado.FechaCreacion:dd/MM/yyyy HH:mm}"
            };
        }

        // Iniciar transacción de Unit of Work para garantizar atomicidad y rollback total
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 5. Crear ArchivoCarga en memoria
            var archivoCarga = ArchivoCarga.Crear(
                request.EmpresaRuc, request.Periodo, Tipo, formato,
                request.NombreArchivo, hash, request.Usuario);

            await _archivoCargaRepo.AgregarAsync(archivoCarga, cancellationToken);

            // 6. Parsear archivo
            var parser = _parserFactory.ObtenerParser(Tipo, formato);
            List<ResultadoParseoLinea> resultados;
            try
            {
                resultados = await parser.ParsearAsync(request.ArchivoStream, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al parsear archivo de ventas: {Message}", ex.Message);
                throw;
            }

            // 7. Instanciar el 100% de los comprobantes como entidades Venta
            var ventas = new List<VentaEntity>();

            foreach (var resultado in resultados)
            {
                try
                {
                    var venta = ConstruirVenta(resultado.Campos, request.EmpresaRuc, request.Periodo, archivoCarga.IdCarga, request.Usuario);
                    ventas.Add(venta);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al instanciar entidad Venta en línea {Linea}: {Message}", resultado.NumeroLinea, ex.Message);
                }
            }

            // 7.1 Ejecutar TODAS las validaciones de negocio de forma centralizada a través de IVentaValidationService
            var errores = await _ventaValidationService.ValidarVentasAsync(
                archivoCarga.IdCarga,
                request.EmpresaRuc,
                request.Periodo,
                ventas,
                cancellationToken);

            // 8. Persistir TODO dentro de la transacción del Unit of Work
            if (ventas.Count > 0)
            {
                await _ventaRepo.AgregarRangoAsync(ventas, cancellationToken);
            }

            if (errores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(errores, cancellationToken);
            }

            var totalBi = ventas.Sum(v => v.BiGravada);
            var totalIgv = ventas.Sum(v => v.IgvIpm);
            var totalGen = ventas.Sum(v => v.TotalCp);

            archivoCarga.ActualizarConteoYMontos(resultados.Count, ventas.Count, errores.Count, totalBi, totalIgv, totalGen);
            await _archivoCargaRepo.ActualizarAsync(archivoCarga, cancellationToken);

            // Guardar cambios y confirmar transacción atómicamente
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Carga VENTAS completada exitosamente. IdCarga: {IdCarga}, Total: {Total}, Insertados: {Insertados}, Errores/Alertas: {Errores}, TotalGeneral: {TotalGeneral}",
                archivoCarga.IdCarga, resultados.Count, ventas.Count, errores.Count, totalGen);

            return new CargarArchivoSunatDTO
            {
                IdCarga = archivoCarga.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TipoArchivo = Tipo,
                Formato = formato,
                NombreOriginal = request.NombreArchivo,
                Estado = archivoCarga.Estado,
                NumRegistros = archivoCarga.NumRegistros,
                NumRegistrosValidos = archivoCarga.NumRegistrosValidos,
                NumRegistrosError = archivoCarga.NumRegistrosError,
                TotalBaseImponible = archivoCarga.TotalBaseImponible,
                TotalIgv = archivoCarga.TotalIgv,
                TotalGeneral = archivoCarga.TotalGeneral,
                Observaciones = archivoCarga.Observaciones
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción durante la carga de ventas. Ejecutando Rollback en UnitOfWork.");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static VentaEntity ConstruirVenta(Dictionary<string, string> c, string empresaRuc, string periodo, Guid idCarga, string usuario)
    {
        return VentaEntity.Crear(
            empresaRuc: empresaRuc,
            periodo: periodo,
            idCarga: idCarga,
            carSunat: c.GetValueOrDefault("car_sunat") ?? string.Empty,
            codigoTipoCp: c.GetValueOrDefault("tipo_cp") ?? string.Empty,
            serie: c.GetValueOrDefault("serie") ?? string.Empty,
            numero: c.GetValueOrDefault("numero") ?? string.Empty,
            fechaEmision: DateTime.TryParse(c.GetValueOrDefault("fecha_emision"), out var fe) ? fe : DateTime.UtcNow,
            codigoTipoDocIdentidad: c.GetValueOrDefault("tipo_doc_identidad") ?? "1",
            nroDocIdentidad: c.GetValueOrDefault("nro_doc_identidad") ?? string.Empty,
            razonSocial: c.GetValueOrDefault("razon_social") ?? string.Empty,
            totalCp: decimal.TryParse(c.GetValueOrDefault("total_cp"), out var tc) ? tc : 0,
            codigoMoneda: c.GetValueOrDefault("moneda") ?? "PEN",
            tipoCambio: decimal.TryParse(c.GetValueOrDefault("tipo_cambio"), out var tca) ? tca : 1.0000m,
            codigoEstadoComprobante: c.GetValueOrDefault("estado_comprobante") ?? "1",
            usuarioCreacion: usuario,
            numeroFinal: string.IsNullOrEmpty(c.GetValueOrDefault("numero_final")) ? null : c.GetValueOrDefault("numero_final"),
            fechaVctoPago: DateTime.TryParse(c.GetValueOrDefault("fecha_vcto_pago"), out var fv) ? fv : null,
            valorFactExp: decimal.TryParse(c.GetValueOrDefault("valor_fact_exp"), out var vfe) ? vfe : 0,
            biGravada: decimal.TryParse(c.GetValueOrDefault("bi_gravada"), out var bg) ? bg : 0,
            dsctoBi: decimal.TryParse(c.GetValueOrDefault("dscto_bi"), out var db) ? db : 0,
            igvIpm: decimal.TryParse(c.GetValueOrDefault("igv_ipm"), out var ig) ? ig : 0,
            dsctoIgvIpm: decimal.TryParse(c.GetValueOrDefault("dscto_igv_ipm"), out var di) ? di : 0,
            montoExonerado: decimal.TryParse(c.GetValueOrDefault("monto_exonerado"), out var me) ? me : 0,
            montoInafecto: decimal.TryParse(c.GetValueOrDefault("monto_inafecto"), out var mi) ? mi : 0,
            isc: decimal.TryParse(c.GetValueOrDefault("isc"), out var isc) ? isc : 0,
            biGravIvap: decimal.TryParse(c.GetValueOrDefault("bi_grav_ivap"), out var bgi) ? bgi : 0,
            ivap: decimal.TryParse(c.GetValueOrDefault("ivap"), out var iv) ? iv : 0,
            icbper: decimal.TryParse(c.GetValueOrDefault("icbper"), out var icb) ? icb : 0,
            otrosTributos: decimal.TryParse(c.GetValueOrDefault("otros_tributos"), out var ot) ? ot : 0,
            fechaEmisionDocModif: DateTime.TryParse(c.GetValueOrDefault("fecha_emision_doc_modif"), out var fem) ? fem : null,
            tipoCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_cp_modificado")) ? null : c.GetValueOrDefault("tipo_cp_modificado"),
            serieCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("serie_cp_modificado")) ? null : c.GetValueOrDefault("serie_cp_modificado"),
            nroCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("nro_cp_modificado")) ? null : c.GetValueOrDefault("nro_cp_modificado"),
            idProyectoOpAttr: string.IsNullOrEmpty(c.GetValueOrDefault("id_proyecto_op_attr")) ? null : c.GetValueOrDefault("id_proyecto_op_attr"),
            valorFobEmbar: decimal.TryParse(c.GetValueOrDefault("valor_fob_embar"), out var vf) ? vf : 0,
            valorOpGratuitas: decimal.TryParse(c.GetValueOrDefault("valor_op_gratuitas"), out var vog) ? vog : 0,
            tipoOperacion: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_operacion")) ? null : c.GetValueOrDefault("tipo_operacion"),
            damCp: string.IsNullOrEmpty(c.GetValueOrDefault("dam_cp")) ? null : c.GetValueOrDefault("dam_cp"),
            codigoTipoNota: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_nota")) ? null : c.GetValueOrDefault("tipo_nota"),
            camposLibres: string.IsNullOrEmpty(c.GetValueOrDefault("campos_libres")) ? null : c.GetValueOrDefault("campos_libres")
        );
    }
}
