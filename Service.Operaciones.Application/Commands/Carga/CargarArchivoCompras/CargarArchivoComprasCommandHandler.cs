using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoCompras;

public class CargarArchivoComprasCommandHandler : IRequestHandler<CargarArchivoComprasCommand, CargarArchivoSunatDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraSireRepository _compraSireRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IHashService _hashService;
    private readonly IArchivoSunatParserFactory _parserFactory;
    private readonly ICompraSireValidationService _compraValidationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoComprasCommandHandler> _logger;

    private const TipoOperacion Tipo = TipoOperacion.CompraSire;

    public CargarArchivoComprasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraSireRepository compraSireRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IHashService hashService,
        IArchivoSunatParserFactory parserFactory,
        ICompraSireValidationService compraValidationService,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoComprasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraSireRepo = compraSireRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _hashService = hashService;
        _parserFactory = parserFactory;
        _compraValidationService = compraValidationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoComprasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo COMPRAS SIRE. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
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
                TipoOperacion = Tipo,
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
                _logger.LogError(ex, "Error crítico al parsear archivo de compras SIRE: {Message}", ex.Message);
                throw;
            }

            // 7. Instanciar el 100% de los comprobantes como entidades CompraSire
            var compras = new List<CompraSire>();

            foreach (var resultado in resultados)
            {
                try
                {
                    var compra = ConstruirCompra(resultado.NumeroLinea, resultado.Campos, request.EmpresaRuc, request.Periodo, archivoCarga.IdCarga, request.Usuario);
                    compras.Add(compra);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al instanciar entidad CompraSire en línea {Linea}: {Message}", resultado.NumeroLinea, ex.Message);
                }
            }

            // 7.1 Ejecutar TODAS las validaciones de negocio de forma centralizada a través de ICompraSireValidationService
            var errores = await _compraValidationService.ValidarComprasSireAsync(
                archivoCarga.IdCarga,
                request.EmpresaRuc,
                request.Periodo,
                compras,
                cancellationToken);

            // 8. Persistir TODO dentro de la transacción del Unit of Work
            if (compras.Count > 0)
            {
                await _compraSireRepo.AgregarRangoAsync(compras, cancellationToken);
            }

            if (errores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(errores, cancellationToken);
            }

            var totalBiCompras = compras.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            var totalIgvCompras = compras.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);
            var totalGenCompras = compras.Sum(c => c.TotalCp);

            archivoCarga.ActualizarConteoYMontos(resultados.Count, compras.Count, errores.Count, totalBiCompras, totalIgvCompras, totalGenCompras);
            await _archivoCargaRepo.ActualizarAsync(archivoCarga, cancellationToken);

            // Guardar cambios y confirmar transacción atómicamente
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Carga COMPRAS SIRE completada exitosamente. IdCarga: {IdCarga}, Total: {Total}, Insertados: {Insertados}, Errores/Alertas: {Errores}, TotalGeneral: {TotalGeneral}",
                archivoCarga.IdCarga, resultados.Count, compras.Count, errores.Count, totalGenCompras);

            return new CargarArchivoSunatDTO
            {
                IdCarga = archivoCarga.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TipoOperacion = Tipo,
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
            _logger.LogError(ex, "Excepción durante la carga de compras SIRE. Ejecutando Rollback en UnitOfWork.");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static CompraSire ConstruirCompra(int numeroLinea, Dictionary<string, string> c, string empresaRuc, string periodo, Guid idCarga, string usuario)
    {
        var rucFila = c.GetValueOrDefault("ruc");
        var periodoFila = c.GetValueOrDefault("periodo");

        return CompraSire.Crear(
            empresaRuc: !string.IsNullOrWhiteSpace(rucFila) ? rucFila.Trim() : empresaRuc,
            periodo: !string.IsNullOrWhiteSpace(periodoFila) ? periodoFila.Trim() : periodo,
            idCarga: idCarga,
            numeroLinea: numeroLinea,
            carSunat: !string.IsNullOrWhiteSpace(c.GetValueOrDefault("car_sunat")) ? c["car_sunat"].Trim() : null,
            fechaEmision: DateTime.TryParse(c.GetValueOrDefault("fecha_emision"), out var fe) ? fe : DateTime.UtcNow,
            codigoTipoCp: c.GetValueOrDefault("tipo_cp") ?? string.Empty,
            serie: c.GetValueOrDefault("serie") ?? string.Empty,
            numero: c.GetValueOrDefault("numero") ?? string.Empty,
            codigoTipoDocIdentidad: c.GetValueOrDefault("tipo_doc_identidad") ?? "6",
            nroDocIdentidad: c.GetValueOrDefault("nro_doc_identidad") ?? string.Empty,
            razonSocial: c.GetValueOrDefault("razon_social") ?? string.Empty,
            totalCp: decimal.TryParse(c.GetValueOrDefault("total_cp"), out var tc) ? tc : 0,
            codigoMoneda: c.GetValueOrDefault("moneda") ?? "PEN",
            tipoCambio: decimal.TryParse(c.GetValueOrDefault("tipo_cambio"), out var tca) ? tca : 1.0000m,
            codigoEstadoComprobante: c.GetValueOrDefault("estado_comprobante") ?? "1",
            usuarioCreacion: usuario,
            fechaVencimiento: DateTime.TryParse(c.GetValueOrDefault("fecha_vencimiento"), out var fv) ? fv : null,
            anioDocumento: string.IsNullOrEmpty(c.GetValueOrDefault("anio_documento")) ? null : c.GetValueOrDefault("anio_documento"),
            numeroFinal: string.IsNullOrEmpty(c.GetValueOrDefault("numero_final")) ? null : c.GetValueOrDefault("numero_final"),
            biGravadoDg: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dg"), out var bg1) ? bg1 : 0,
            igvIpmDg: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dg"), out var ig1) ? ig1 : 0,
            biGravadoDgng: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dgng"), out var bg2) ? bg2 : 0,
            igvIpmDgng: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dgng"), out var ig2) ? ig2 : 0,
            biGravadoDng: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dng"), out var bg3) ? bg3 : 0,
            igvIpmDng: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dng"), out var ig3) ? ig3 : 0,
            valorAdqNg: decimal.TryParse(c.GetValueOrDefault("valor_adq_ng"), out var vn) ? vn : 0,
            montoIsc: decimal.TryParse(c.GetValueOrDefault("monto_isc"), out var isc) ? isc : 0,
            montoIcbper: decimal.TryParse(c.GetValueOrDefault("monto_icbper"), out var icb) ? icb : 0,
            montoOtrosTributos: decimal.TryParse(c.GetValueOrDefault("monto_otros_tributos"), out var otc) ? otc : 0,
            fechaEmisionDocModificado: DateTime.TryParse(c.GetValueOrDefault("fecha_emision_doc_modificado"), out var fem) ? fem : null,
            codigoTipoCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_cp_modificado")) ? null : c.GetValueOrDefault("tipo_cp_modificado"),
            serieCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("serie_cp_modificado")) ? null : c.GetValueOrDefault("serie_cp_modificado"),
            codDamDsi: string.IsNullOrEmpty(c.GetValueOrDefault("cod_dam_dsi")) ? null : c.GetValueOrDefault("cod_dam_dsi"),
            numeroCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("numero_cp_modificado")) ? null : c.GetValueOrDefault("numero_cp_modificado"),
            clasifBssSss: string.IsNullOrEmpty(c.GetValueOrDefault("clasif_bss_sss")) ? null : c.GetValueOrDefault("clasif_bss_sss"),
            idProyectoOp: string.IsNullOrEmpty(c.GetValueOrDefault("id_proyecto_op")) ? null : c.GetValueOrDefault("id_proyecto_op"),
            porcPart: decimal.TryParse(c.GetValueOrDefault("porc_part"), out var pp) ? pp : null,
            imb: decimal.TryParse(c.GetValueOrDefault("imb"), out var imb) ? imb : 0,
            carOrigIndEI: string.IsNullOrEmpty(c.GetValueOrDefault("car_orig_ind_e_i")) ? null : c.GetValueOrDefault("car_orig_ind_e_i"),
            detraccion: string.IsNullOrEmpty(c.GetValueOrDefault("detraccion")) ? null : c.GetValueOrDefault("detraccion"),
            codigoTipoNota: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_nota")) ? null : c.GetValueOrDefault("tipo_nota"),
            incal: string.IsNullOrEmpty(c.GetValueOrDefault("incal")) ? null : c.GetValueOrDefault("incal"),
            camposLibres: string.IsNullOrEmpty(c.GetValueOrDefault("campos_libres")) ? null : c.GetValueOrDefault("campos_libres")
        );
    }
}
