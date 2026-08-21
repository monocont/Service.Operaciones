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
    private readonly ICompraRepository _compraRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IHashService _hashService;
    private readonly IArchivoSunatParserFactory _parserFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CargarArchivoComprasCommandHandler> _logger;

    private const TipoArchivo Tipo = TipoArchivo.Compras;

    public CargarArchivoComprasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraRepository compraRepo,
        IEmpresaService empresaService,
        IHashService hashService,
        IArchivoSunatParserFactory parserFactory,
        IUnitOfWork unitOfWork,
        ILogger<CargarArchivoComprasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraRepo = compraRepo;
        _empresaService = empresaService;
        _hashService = hashService;
        _parserFactory = parserFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CargarArchivoSunatDTO> Handle(CargarArchivoComprasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando carga de archivo COMPRAS. Empresa: {EmpresaRuc}, Periodo: {Periodo}",
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
                _logger.LogError(ex, "Error crítico al parsear archivo de compras: {Message}", ex.Message);
                throw;
            }

            // 7. Procesar cada linea
            var errores = new List<ArchivoCargaError>();
            var compras = new List<Compra>();
            var lineasValidas = new List<Dictionary<string, string>>();
            var carSunatsVistos = new HashSet<string>();
            var validosCount = 0;
            var erroresCount = 0;

            var anioPeriodo = int.Parse(request.Periodo[..4]);
            var mesPeriodo = int.Parse(request.Periodo[4..]);

            foreach (var resultado in resultados)
            {
                if (!resultado.EsValido)
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga, resultado.NumeroLinea,
                        TipoErrorCarga.Formato, resultado.MensajeError ?? "Error de formato"));
                    erroresCount++;
                    continue;
                }

                // Validar RUC del archivo contra empresa seleccionada
                var rucArchivo = resultado.Campos.GetValueOrDefault("ruc") ?? string.Empty;
                if (rucArchivo != request.EmpresaRuc)
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga, resultado.NumeroLinea,
                        TipoErrorCarga.Negocio,
                        $"El RUC del archivo ({rucArchivo}) no coincide con la empresa seleccionada ({request.EmpresaRuc})",
                        "ruc", rucArchivo));
                    erroresCount++;
                    continue;
                }

                // Validar periodo: fecha_emision dentro del mes/año indicado
                if (DateTime.TryParse(resultado.Campos.GetValueOrDefault("fecha_emision"), out var fechaEmision))
                {
                    if (fechaEmision.Year != anioPeriodo || fechaEmision.Month != mesPeriodo)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            archivoCarga.IdCarga, resultado.NumeroLinea,
                            TipoErrorCarga.Negocio,
                            $"La fecha de emision {fechaEmision:dd/MM/yyyy} no pertenece al periodo {request.Periodo}",
                            "fecha_emision", fechaEmision.ToString("dd/MM/yyyy")));
                        erroresCount++;
                        continue;
                    }
                }

                // Verificar duplicado car_sunat en BD
                var carSunat = resultado.Campos.GetValueOrDefault("car_sunat") ?? string.Empty;
                if (await _compraRepo.ExistePorCarSunatAsync(request.EmpresaRuc, request.Periodo, carSunat, cancellationToken))
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga, resultado.NumeroLinea,
                        TipoErrorCarga.Duplicado,
                        $"El comprobante con CAR_SUNAT '{carSunat}' ya existe en la base de datos",
                        "car_sunat", carSunat));
                    erroresCount++;
                    continue;
                }

                // Verificar duplicado car_sunat DENTRO del archivo
                if (!carSunatsVistos.Add(carSunat))
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga, resultado.NumeroLinea,
                        TipoErrorCarga.Duplicado,
                        $"El comprobante con CAR_SUNAT '{carSunat}' aparece duplicado dentro del archivo",
                        "car_sunat", carSunat));
                    erroresCount++;
                    continue;
                }

                // Construir Compra
                try
                {
                    var compra = ConstruirCompra(resultado.Campos, request.EmpresaRuc, request.Periodo, archivoCarga.IdCarga, request.Usuario);
                    compras.Add(compra);
                    lineasValidas.Add(resultado.Campos);
                    validosCount++;
                }
                catch (Exception ex)
                {
                    errores.Add(ArchivoCargaError.Crear(
                        archivoCarga.IdCarga, resultado.NumeroLinea,
                        TipoErrorCarga.Negocio, $"Error al construir entidad: {ex.Message}",
                        severidad: SeveridadError.Error));
                    erroresCount++;
                }
            }

            // 8. Validar series consecutivas (advertencias, no bloquean)
            var advertenciasSecuencia = ValidarSeriesConsecutivas(lineasValidas);
            foreach (var advertencia in advertenciasSecuencia)
            {
                errores.Add(ArchivoCargaError.Crear(
                    archivoCarga.IdCarga, 0,
                    TipoErrorCarga.Secuencia, advertencia,
                    severidad: SeveridadError.Advertencia));
            }

            // 9. Persistir TODO dentro de la transacción del Unit of Work
            if (compras.Count > 0)
            {
                await _compraRepo.AgregarRangoAsync(compras, cancellationToken);
            }

            if (errores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(errores, cancellationToken);
            }

            var totalBiCompras = compras.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            var totalIgvCompras = compras.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);
            var totalGenCompras = compras.Sum(c => c.TotalCp);

            archivoCarga.ActualizarConteoYMontos(resultados.Count, validosCount, erroresCount, totalBiCompras, totalIgvCompras, totalGenCompras);
            await _archivoCargaRepo.ActualizarAsync(archivoCarga, cancellationToken);

            // Guardar cambios y confirmar transacción atómicamente
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Carga COMPRAS completada exitosamente con UoW. IdCarga: {IdCarga}, Total: {Total}, Validos: {Validos}, Errores: {Errores}",
                archivoCarga.IdCarga, resultados.Count, validosCount, erroresCount);

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
            _logger.LogError(ex, "Excepción durante la carga de compras. Ejecutando Rollback en UnitOfWork.");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static List<string> ValidarSeriesConsecutivas(List<Dictionary<string, string>> lineas)
    {
        var advertencias = new List<string>();

        var porSerie = lineas
            .GroupBy(l => l.GetValueOrDefault("serie") ?? string.Empty)
            .Where(g => g.Key.Length > 0);

        foreach (var grupo in porSerie)
        {
            var numeros = grupo
                .Select(l => l.GetValueOrDefault("numero") ?? string.Empty)
                .Where(n => int.TryParse(n, out _))
                .Select(int.Parse)
                .OrderBy(n => n)
                .ToList();

            if (numeros.Count < 2)
            {
                continue;
            }

            for (var i = 1; i < numeros.Count; i++)
            {
                var salto = numeros[i] - numeros[i - 1];
                if (salto > 1)
                {
                    advertencias.Add(
                        $"Serie {grupo.Key}: salto entre {numeros[i - 1]} y {numeros[i]} (faltan {salto - 1} numeros)");
                }
            }
        }

        return advertencias;
    }

    private static Compra ConstruirCompra(Dictionary<string, string> c, string empresaRuc, string periodo, Guid idCarga, string usuario)
    {
        return Compra.Crear(
            empresaRuc: empresaRuc,
            periodo: periodo,
            idCarga: idCarga,
            carSunat: c.GetValueOrDefault("car_sunat") ?? string.Empty,
            codigoTipoCp: c.GetValueOrDefault("tipo_cp") ?? string.Empty,
            serie: c.GetValueOrDefault("serie") ?? string.Empty,
            numero: c.GetValueOrDefault("numero") ?? string.Empty,
            fechaEmision: DateTime.TryParse(c.GetValueOrDefault("fecha_emision"), out var fe) ? fe : DateTime.UtcNow,
            codigoTipoDocIdentidad: c.GetValueOrDefault("tipo_doc_identidad") ?? "6",
            nroDocIdentidad: c.GetValueOrDefault("nro_doc_identidad") ?? string.Empty,
            razonSocial: c.GetValueOrDefault("razon_social") ?? string.Empty,
            totalCp: decimal.TryParse(c.GetValueOrDefault("total_cp"), out var tc) ? tc : 0,
            codigoMoneda: c.GetValueOrDefault("moneda") ?? "PEN",
            tipoCambio: decimal.TryParse(c.GetValueOrDefault("tipo_cambio"), out var tca) ? tca : 1.0000m,
            codigoEstadoComprobante: c.GetValueOrDefault("estado_comprobante") ?? "1",
            usuarioCreacion: usuario,
            numeroFinal: string.IsNullOrEmpty(c.GetValueOrDefault("numero_final")) ? null : c.GetValueOrDefault("numero_final"),
            anioDocumento: string.IsNullOrEmpty(c.GetValueOrDefault("anio_documento")) ? null : c.GetValueOrDefault("anio_documento"),
            fechaVctoPago: DateTime.TryParse(c.GetValueOrDefault("fecha_vcto_pago"), out var fv) ? fv : null,
            biGravadoDg: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dg"), out var bg1) ? bg1 : 0,
            igvIpmDg: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dg"), out var ig1) ? ig1 : 0,
            biGravadoDgng: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dgng"), out var bg2) ? bg2 : 0,
            igvIpmDgng: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dgng"), out var ig2) ? ig2 : 0,
            biGravadoDng: decimal.TryParse(c.GetValueOrDefault("bi_gravado_dng"), out var bg3) ? bg3 : 0,
            igvIpmDng: decimal.TryParse(c.GetValueOrDefault("igv_ipm_dng"), out var ig3) ? ig3 : 0,
            valorAdqNg: decimal.TryParse(c.GetValueOrDefault("valor_adq_ng"), out var vn) ? vn : 0,
            isc: decimal.TryParse(c.GetValueOrDefault("isc"), out var isc) ? isc : 0,
            icbper: decimal.TryParse(c.GetValueOrDefault("icbper"), out var icb) ? icb : 0,
            otrosTribCargos: decimal.TryParse(c.GetValueOrDefault("otros_trib_cargos"), out var otc) ? otc : 0,
            fechaEmisionDocModif: DateTime.TryParse(c.GetValueOrDefault("fecha_emision_doc_modif"), out var fem) ? fem : null,
            tipoCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_cp_modificado")) ? null : c.GetValueOrDefault("tipo_cp_modificado"),
            serieCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("serie_cp_modificado")) ? null : c.GetValueOrDefault("serie_cp_modificado"),
            nroCpModificado: string.IsNullOrEmpty(c.GetValueOrDefault("nro_cp_modificado")) ? null : c.GetValueOrDefault("nro_cp_modificado"),
            codDamDsi: string.IsNullOrEmpty(c.GetValueOrDefault("cod_dam_dsi")) ? null : c.GetValueOrDefault("cod_dam_dsi"),
            clasifBssSss: string.IsNullOrEmpty(c.GetValueOrDefault("clasif_bss_sss")) ? null : c.GetValueOrDefault("clasif_bss_sss"),
            idProyectoOp: string.IsNullOrEmpty(c.GetValueOrDefault("id_proyecto_op")) ? null : c.GetValueOrDefault("id_proyecto_op"),
            porcPart: decimal.TryParse(c.GetValueOrDefault("porc_part"), out var pp) ? pp : null,
            imb: decimal.TryParse(c.GetValueOrDefault("imb"), out var imb) ? imb : null,
            carOrigIndEI: string.IsNullOrEmpty(c.GetValueOrDefault("car_orig_ind_e_i")) ? null : c.GetValueOrDefault("car_orig_ind_e_i"),
            detraccion: string.IsNullOrEmpty(c.GetValueOrDefault("detraccion")) ? null : c.GetValueOrDefault("detraccion"),
            codigoTipoNota: string.IsNullOrEmpty(c.GetValueOrDefault("tipo_nota")) ? null : c.GetValueOrDefault("tipo_nota"),
            incal: string.IsNullOrEmpty(c.GetValueOrDefault("incal")) ? null : c.GetValueOrDefault("incal")
        );
    }
}
