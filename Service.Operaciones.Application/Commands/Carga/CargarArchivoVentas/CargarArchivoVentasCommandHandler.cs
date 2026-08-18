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
    private readonly IHashService _hashService;
    private readonly IArchivoSunatParserFactory _parserFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoVentasCommandHandler> _logger;

    private const TipoArchivo Tipo = TipoArchivo.Ventas;

    public CargarArchivoVentasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaRepository ventaRepo,
        IEmpresaService empresaService,
        IHashService hashService,
        IArchivoSunatParserFactory parserFactory,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoVentasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaRepo = ventaRepo;
        _empresaService = empresaService;
        _hashService = hashService;
        _parserFactory = parserFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoVentasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo VENTAS. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
            request.EmpresaRuc, request.Periodo);

        // 1. Validar empresa
        if (!await _empresaService.ExisteEmpresaAsync(request.EmpresaRuc, cancellationToken))
        {
            throw new ValidationException($"La empresa con RUC {request.EmpresaRuc} no esta registrada en el sistema");
        }

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

            // 7. Procesar y guardar el 100% de los comprobantes en operaciones.venta
            var errores = new List<ArchivoCargaError>();
            var ventas = new List<VentaEntity>();
            var lineasValidas = new List<Dictionary<string, string>>();

            var anioPeriodo = int.Parse(request.Periodo[..4]);
            var mesPeriodo = int.Parse(request.Periodo[4..]);
            var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

            // Mapa para contar duplicados por Serie + Número dentro del archivo: clave -> lista de líneas
            var comprobantesPorSerieNumero = new Dictionary<string, List<int>>();

            foreach (var resultado in resultados)
            {
                var numeroLinea = resultado.NumeroLinea;
                var campos = resultado.Campos;

                // 7.1 Construir Venta y agregarla SIEMPRE al listado a persistir (100% de los datos)
                try
                {
                    var venta = ConstruirVenta(campos, request.EmpresaRuc, request.Periodo, archivoCarga.IdCarga, request.Usuario);
                    ventas.Add(venta);
                    lineasValidas.Add(campos);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al instanciar entidad Venta en línea {Linea}: {Message}", numeroLinea, ex.Message);
                }

                // Obtener datos clave para validaciones
                var serie = campos.GetValueOrDefault("serie") ?? string.Empty;
                var numero = campos.GetValueOrDefault("numero") ?? string.Empty;
                var periodoFila = campos.GetValueOrDefault("periodo") ?? string.Empty;
                var fechaEmisionStr = campos.GetValueOrDefault("fecha_emision") ?? string.Empty;

                // Validacion 1: Agrupar para detectar duplicados por Serie + Número
                if (!string.IsNullOrWhiteSpace(serie) && !string.IsNullOrWhiteSpace(numero))
                {
                    var claveSerieNum = $"{serie.Trim().ToUpper()}|{numero.Trim().ToUpper()}";
                    if (!comprobantesPorSerieNumero.ContainsKey(claveSerieNum))
                    {
                        comprobantesPorSerieNumero[claveSerieNum] = new List<int>();
                    }
                    comprobantesPorSerieNumero[claveSerieNum].Add(numeroLinea);
                }

                // Validacion 4: Verificar que el periodo cargado en la fila corresponda al periodo actual
                if (!string.IsNullOrWhiteSpace(periodoFila) && periodoFila != request.Periodo)
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga,
                        numeroLinea,
                        TipoErrorCarga.Negocio,
                        $"Periodo inconsistente: El comprobante Serie '{serie}', Número '{numero}' corresponde al periodo '{periodoFila}', difiere del periodo cargado '{request.Periodo}'",
                        campoError: "periodo",
                        valorLectura: periodoFila,
                        severidad: SeveridadError.Warning));
                }

                // Validacion 5: Verificar que la fecha de emisión sea como máximo el último día del mes del periodo
                if (DateTime.TryParse(fechaEmisionStr, out var fechaEmision))
                {
                    if (fechaEmision.Date > ultimoDiaPeriodo.Date)
                    {
                        var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                        errores.Add(ArchivoCargaError.Crear(
                            archivoCarga.IdCarga,
                            numeroLinea,
                            TipoErrorCarga.Negocio,
                            $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} correspondiente al periodo '{periodoFecha}' (posterior al cierre {ultimoDiaPeriodo:dd/MM/yyyy})",
                            campoError: "fecha_emision",
                            valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                            severidad: SeveridadError.Warning));
                    }
                }
            }

            // Validacion 1 (Registro): Reportar comprobantes duplicados por Serie + Número
            foreach (var kvp in comprobantesPorSerieNumero.Where(kvp => kvp.Value.Count > 1))
            {
                var partes = kvp.Key.Split('|');
                var s = partes[0];
                var n = partes[1];
                var cant = kvp.Value.Count;
                var primeraLinea = kvp.Value.First();

                errores.Add(ArchivoCargaError.Crear(
                    archivoCarga.IdCarga,
                    primeraLinea,
                    TipoErrorCarga.Duplicado,
                    $"Comprobante duplicado: La Serie '{s}', Número '{n}' se encuentra registrada {cant} veces en el archivo",
                    campoError: "serie_numero",
                    valorLectura: $"{s}-{n}",
                    severidad: SeveridadError.Warning));
            }

            // Validacion 2: Validar correlatividad y saltos de secuencia dentro del archivo (por tipo_cp y serie)
            var advertenciasSecuenciaInterna = ValidarSeriesConsecutivas(lineasValidas, archivoCarga.IdCarga);
            errores.AddRange(advertenciasSecuenciaInterna);

            // Validacion 3: Validar correlativo contra el periodo anterior inmediato
            var periodoAnterior = ObtenerPeriodoAnterior(request.Periodo);
            var advertenciasPeriodoAnterior = await ValidarCorrelativoPeriodoAnteriorAsync(
                request.EmpresaRuc, periodoAnterior, lineasValidas, archivoCarga.IdCarga, cancellationToken);
            errores.AddRange(advertenciasPeriodoAnterior);

            // 8. Persistir TODO dentro de la transacción del Unit of Work
            if (ventas.Count > 0)
            {
                await _ventaRepo.AgregarRangoAsync(ventas, cancellationToken);
            }

            if (errores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(errores, cancellationToken);
            }

            archivoCarga.ActualizarConteo(resultados.Count, ventas.Count, errores.Count);
            await _archivoCargaRepo.ActualizarAsync(archivoCarga, cancellationToken);

            // Guardar cambios y confirmar transacción atómicamente
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Carga VENTAS completada exitosamente. IdCarga: {IdCarga}, Total: {Total}, Insertados: {Insertados}, Errores/Alertas: {Errores}",
                archivoCarga.IdCarga, resultados.Count, ventas.Count, errores.Count);

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

    private static string ObtenerPeriodoAnterior(string periodoActual)
    {
        var anio = int.Parse(periodoActual[..4]);
        var mes = int.Parse(periodoActual[4..]);

        if (mes == 1)
        {
            anio--;
            mes = 12;
        }
        else
        {
            mes--;
        }

        return $"{anio}{mes:D2}";
    }

    private static List<ArchivoCargaError> ValidarSeriesConsecutivas(List<Dictionary<string, string>> lineas, Guid idCarga)
    {
        var errores = new List<ArchivoCargaError>();

        var porSerie = lineas
            .GroupBy(l => new
            {
                TipoCp = l.GetValueOrDefault("tipo_cp") ?? string.Empty,
                Serie = (l.GetValueOrDefault("serie") ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numeros = grupo
                .Select(l => new
                {
                    NumeroStr = l.GetValueOrDefault("numero") ?? string.Empty,
                    Parseado = long.TryParse(l.GetValueOrDefault("numero")?.Trim(), out var n) ? (long?)n : null
                })
                .Where(x => x.Parseado.HasValue)
                .Select(x => x.Parseado!.Value)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (numeros.Count < 2)
            {
                continue;
            }

            for (var i = 1; i < numeros.Count; i++)
            {
                var anterior = numeros[i - 1];
                var actual = numeros[i];
                var diferencia = actual - anterior;

                if (diferencia > 1)
                {
                    if (diferencia == 2)
                    {
                        var faltante = anterior + 1;
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            0,
                            TipoErrorCarga.Secuencia,
                            $"Registro faltante: Serie '{grupo.Key.Serie}', Número: {faltante}",
                            campoError: "numero",
                            valorLectura: faltante.ToString(),
                            severidad: SeveridadError.Warning));
                    }
                    else
                    {
                        var desde = anterior + 1;
                        var hasta = actual - 1;
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            0,
                            TipoErrorCarga.Secuencia,
                            $"Múltiples registros faltantes: Serie '{grupo.Key.Serie}', del Número: {desde} al Número: {hasta}",
                            campoError: "numero",
                            valorLectura: $"{desde}-{hasta}",
                            severidad: SeveridadError.Warning));
                    }
                }
            }
        }

        return errores;
    }

    private async Task<List<ArchivoCargaError>> ValidarCorrelativoPeriodoAnteriorAsync(
        string empresaRuc,
        string periodoAnterior,
        List<Dictionary<string, string>> lineasActuales,
        Guid idCarga,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();

        var porSerie = lineasActuales
            .GroupBy(l => new
            {
                TipoCp = l.GetValueOrDefault("tipo_cp") ?? string.Empty,
                Serie = (l.GetValueOrDefault("serie") ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numerosActuales = grupo
                .Select(l => long.TryParse(l.GetValueOrDefault("numero")?.Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();

            if (numerosActuales.Count == 0) continue;

            var numeroMasBajoActual = numerosActuales.First();

            // Consultar en la base de datos los comprobantes del periodo anterior para esta serie y tipo
            var numerosPeriodoAnterior = await _ventaRepo.ObtenerNumerosPorSerieYPeriodoAsync(
                empresaRuc, periodoAnterior, grupo.Key.TipoCp, grupo.Key.Serie, cancellationToken);

            if (numerosPeriodoAnterior == null || numerosPeriodoAnterior.Count == 0)
            {
                // Si no existen registros en el periodo anterior para esta serie, no se aplica validación
                continue;
            }

            var numerosAnterioresParseados = numerosPeriodoAnterior
                .Select(numStr => long.TryParse(numStr?.Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();

            if (numerosAnterioresParseados.Count == 0) continue;

            var ultimoCorrelativoAnterior = numerosAnterioresParseados.Last();
            var correlativoEsperado = ultimoCorrelativoAnterior + 1;

            if (numeroMasBajoActual != correlativoEsperado)
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    0,
                    TipoErrorCarga.Secuencia,
                    $"Discontinuidad con periodo anterior: Para la Serie '{grupo.Key.Serie}', el último correlativo del periodo {periodoAnterior} fue {ultimoCorrelativoAnterior}, pero en este periodo inicia en {numeroMasBajoActual} (esperado: {correlativoEsperado})",
                    campoError: "numero",
                    valorLectura: $"Anterior: {ultimoCorrelativoAnterior}, Actual: {numeroMasBajoActual}",
                    severidad: SeveridadError.Warning));
            }
        }

        return errores;
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
