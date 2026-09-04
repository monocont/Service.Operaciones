using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentasMatch;

public class ActualizarVentasMatchCommandHandler : IRequestHandler<ActualizarVentasMatchCommand, ActualizarVentasMatchResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaMatchRepository _ventaMatchRepo;
    private readonly IVentaRepository _ventaRepo;
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarVentasMatchCommandHandler> _logger;

    public ActualizarVentasMatchCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IAccesoEmpresaValidator accesoValidator,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaMatchRepository ventaMatchRepo,
        IVentaRepository ventaRepo,
        IVentaEmpresaRepository ventaEmpresaRepo,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarVentasMatchCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _accesoValidator = accesoValidator;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaMatchRepo = ventaMatchRepo;
        _ventaRepo = ventaRepo;
        _ventaEmpresaRepo = ventaEmpresaRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarVentasMatchResponseDTO> Handle(ActualizarVentasMatchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización de ventas_match para carga {IdCarga}. Eliminados: {Eliminados}, Nuevos: {Nuevos}, Modificados: {Modificados}",
            request.IdCarga, request.EliminadosIds?.Count ?? 0, request.Nuevos?.Count ?? 0, request.Modificados?.Count ?? 0);

        var carga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (carga is null)
        {
            throw new NotFoundException("Carga", request.IdCarga);
        }

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Eliminar comprobantes seleccionados
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _ventaMatchRepo.EliminarRangoPorIdsAsync(request.EliminadosIds, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Obtener lista actual de comprobantes vigentes en base de datos
            var comprobantesActuales = await _ventaMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);
            var dictActuales = comprobantesActuales.ToDictionary(v => v.IdVentaMatch);

            // 3. Procesar Modificaciones
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var paraActualizar = new List<VentaMatch>();
                foreach (var m in request.Modificados)
                {
                    if (dictActuales.TryGetValue(m.IdVentaMatch, out var entidad))
                    {
                        var numeroNormalizado = NormalizarNumeroComprobante(m.Numero ?? entidad.Numero);
                        var serieNormalizada = (m.Serie ?? entidad.Serie)?.Trim().ToUpper() ?? string.Empty;

                        entidad.ActualizarDatos(
                            origenDato: m.OrigenDato ?? entidad.OrigenDato,
                            esCoincidenciaExacta: entidad.EsCoincidenciaExacta,
                            esDiferencia: entidad.EsDiferencia,
                            esSoloUnOrigen: entidad.EsSoloUnOrigen,
                            codigoTipoCp: m.CodigoTipoCp?.Trim() ?? entidad.CodigoTipoCp,
                            serie: serieNormalizada,
                            numero: numeroNormalizado,
                            fechaEmision: m.FechaEmision ?? entidad.FechaEmision,
                            codigoTipoDocIdentidad: m.CodigoTipoDocIdentidad?.Trim() ?? entidad.CodigoTipoDocIdentidad,
                            nroDocIdentidad: m.NroDocIdentidad?.Trim() ?? entidad.NroDocIdentidad,
                            razonSocial: (m.RazonSocial?.Trim() ?? entidad.RazonSocial).ToUpperInvariant(),
                            totalCp: m.TotalCp,
                            usuarioModificacion: request.Usuario ?? "sistema",
                            carSunat: m.CarSunat ?? entidad.CarSunat,
                            biGravada: m.BiGravada,
                            igvIpm: m.IgvIpm,
                            codigoMoneda: string.IsNullOrWhiteSpace(m.CodigoMoneda) ? entidad.CodigoMoneda : m.CodigoMoneda.Trim().ToUpper(),
                            tipoCambio: (m.TipoCambio.HasValue && m.TipoCambio.Value > 0) ? m.TipoCambio.Value : entidad.TipoCambio,
                            codigoEstadoComprobante: string.IsNullOrWhiteSpace(m.CodigoEstadoComprobante) ? entidad.CodigoEstadoComprobante : m.CodigoEstadoComprobante.Trim()
                        );

                        paraActualizar.Add(entidad);
                    }
                }

                if (paraActualizar.Count > 0)
                {
                    await _ventaMatchRepo.ActualizarRangoAsync(paraActualizar, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // 4. Procesar Inserciones Nuevas
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var nuevosEntidades = new List<VentaMatch>();
                var maxLinea = comprobantesActuales.Count > 0 ? comprobantesActuales.Max(c => c.NumeroLinea) : 0;

                foreach (var n in request.Nuevos)
                {
                    if (!n.FechaEmision.HasValue)
                        throw new ValidationException("La fecha de emisión es obligatoria para nuevos registros.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.Serie))
                        throw new ValidationException("La Serie es obligatoria.");
                    if (string.IsNullOrWhiteSpace(n.Numero))
                        throw new ValidationException("El Número de comprobante es obligatorio.");

                    var numeroNormalizado = NormalizarNumeroComprobante(n.Numero);
                    var serieNormalizada = n.Serie.Trim().ToUpper();
                    var moneda = string.IsNullOrWhiteSpace(n.CodigoMoneda) ? "PEN" : n.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (n.TipoCambio.HasValue && n.TipoCambio.Value > 0) ? n.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(n.CodigoEstadoComprobante) ? "1" : n.CodigoEstadoComprobante.Trim();

                    maxLinea++;
                    var nuevo = VentaMatch.Crear(
                        idCarga: carga.IdCarga,
                        empresaRuc: carga.EmpresaRuc,
                        periodo: carga.Periodo,
                        numeroLinea: maxLinea,
                        origenDato: string.IsNullOrWhiteSpace(n.OrigenDato) ? "SIRE" : n.OrigenDato.Trim().ToUpper(),
                        esCoincidenciaExacta: true,
                        esDiferencia: false,
                        esSoloUnOrigen: false,
                        codigoTipoCp: n.CodigoTipoCp.Trim(),
                        serie: serieNormalizada,
                        numero: numeroNormalizado,
                        fechaEmision: n.FechaEmision.Value,
                        codigoTipoDocIdentidad: n.CodigoTipoDocIdentidad?.Trim() ?? "6",
                        nroDocIdentidad: n.NroDocIdentidad?.Trim() ?? string.Empty,
                        razonSocial: (n.RazonSocial?.Trim() ?? string.Empty).ToUpperInvariant(),
                        totalCp: n.TotalCp,
                        usuarioCreacion: request.Usuario ?? "sistema",
                        carSunat: n.CarSunat,
                        biGravada: n.BiGravada,
                        igvIpm: n.IgvIpm,
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp
                    );

                    nuevosEntidades.Add(nuevo);
                }

                await _ventaMatchRepo.AgregarRangoAsync(nuevosEntidades, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Recuperar todos los comprobantes consolidados vigentes tras mutaciones
            var todosConsolidados = await _ventaMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

            // 6. Obtener orígenes SIRE y EMPRESA para revalidar discrepancias de match
            var cargasSire = await _archivoCargaRepo.ListarAsync(carga.EmpresaRuc, TipoOperacion.VentaSire, carga.Periodo, 1, 1, cancellationToken);
            var cargaSire = cargasSire.FirstOrDefault();

            var cargasEmpresa = await _archivoCargaRepo.ListarAsync(carga.EmpresaRuc, TipoOperacion.VentaEmpresa, carga.Periodo, 1, 1, cancellationToken);
            var cargaEmpresa = cargasEmpresa.FirstOrDefault();

            var ventasSire = cargaSire != null ? await _ventaRepo.ListarPorCargaAsync(cargaSire.IdCarga, cancellationToken) : new List<Service.Operaciones.Domain.Entities.Venta>();
            var ventasEmpresa = cargaEmpresa != null ? await _ventaEmpresaRepo.ListarPorCargaAsync(cargaEmpresa.IdCarga, cancellationToken) : new List<VentaEmpresa>();

            var dictSire = new Dictionary<string, Service.Operaciones.Domain.Entities.Venta>(StringComparer.OrdinalIgnoreCase);
            foreach (var vs in ventasSire)
            {
                var k = $"{vs.Serie?.Trim().ToUpper()}|{NormalizarNumeroComprobante(vs.Numero)}";
                dictSire[k] = vs;
            }

            var dictEmp = new Dictionary<string, VentaEmpresa>(StringComparer.OrdinalIgnoreCase);
            foreach (var ve in ventasEmpresa)
            {
                var k = $"{ve.Serie?.Trim().ToUpper()}|{NormalizarNumeroComprobante(ve.Numero)}";
                dictEmp[k] = ve;
            }

            var nuevosErrores = new List<ArchivoCargaError>();

            // 6.1 Validar discrepancias de match entre orígenes
            foreach (var vm in todosConsolidados)
            {
                var key = $"{vm.Serie?.Trim().ToUpper()}|{NormalizarNumeroComprobante(vm.Numero)}";
                dictSire.TryGetValue(key, out var s);
                dictEmp.TryGetValue(key, out var e);

                if (s != null && e != null)
                {
                    var diffs = new List<string>();
                    if (s.FechaEmision.Date != e.FechaEmision.Date) diffs.Add("No coincide la Fecha de Emisión");
                    if ((s.CodigoTipoCp ?? string.Empty).Trim().PadLeft(2, '0') != (e.CodigoTipoCp ?? string.Empty).Trim().PadLeft(2, '0')) diffs.Add("No coincide el Tipo de Comprobante");
                    if ((s.CodigoTipoDocIdentidad ?? string.Empty).Trim() != (e.CodigoTipoDocIdentidad ?? string.Empty).Trim()) diffs.Add("No coincide el Tipo de Documento de Identidad");
                    if ((s.NroDocIdentidad ?? string.Empty).Trim() != (e.NroDocIdentidad ?? string.Empty).Trim()) diffs.Add("No coincide el RUC / Documento del Cliente");
                    if (s.TotalCp != e.TotalCp) diffs.Add("No coincide el Total CP");

                    foreach (var d in diffs)
                    {
                        nuevosErrores.Add(ArchivoCargaError.Crear(
                            idCarga: carga.IdCarga,
                            numeroLinea: vm.NumeroLinea,
                            tipoError: TipoErrorCarga.Validacion,
                            mensaje: $"{d} en comprobante {vm.Serie}-{vm.Numero}",
                            campoError: "match_discrepancia",
                            valorLectura: $"{vm.Serie}-{vm.Numero}",
                            severidad: SeveridadError.Error
                        ));
                    }
                }
            }

            // 6.2 Validaciones de negocio y correlatividades completas
            var erroresNegocio = await EjecutarValidacionesPosterioresAsync(carga.IdCarga, carga.EmpresaRuc, carga.Periodo, todosConsolidados, cancellationToken);
            nuevosErrores.AddRange(erroresNegocio);

            // 7. Refrescar archivo_carga_error
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(carga.IdCarga, cancellationToken);
            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 8. Actualizar cabecera archivo_carga
            decimal totalGeneral = todosConsolidados.Sum(v => v.TotalCp);
            decimal totalBi = todosConsolidados.Sum(v => v.BiGravada);
            decimal totalIgv = todosConsolidados.Sum(v => v.IgvIpm);

            carga.ActualizarConteoYMontos(
                todosConsolidados.Count,
                todosConsolidados.Count,
                nuevosErrores.Count,
                totalBi,
                totalIgv,
                totalGeneral);

            await _archivoCargaRepo.ActualizarAsync(carga, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Actualización de ventas_match finalizada con éxito. Carga: {IdCarga}, TotalRegs: {Regs}, Obs: {Obs}",
                carga.IdCarga, todosConsolidados.Count, nuevosErrores.Count);

            return new ActualizarVentasMatchResponseDTO
            {
                IdCarga = carga.IdCarga,
                Exito = true,
                Mensaje = "Registros de match actualizados y comprobantes revalidados correctamente.",
                NumRegistros = todosConsolidados.Count,
                NumObservaciones = nuevosErrores.Count,
                TotalBaseImponible = totalBi,
                TotalIgv = totalIgv,
                TotalGeneral = totalGeneral
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar ventas_match para carga {IdCarga}", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<ArchivoCargaError>> EjecutarValidacionesPosterioresAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<VentaMatch> ventasMatch,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (ventasMatch.Count == 0) return errores;

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var primerDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, 1);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        for (var i = 0; i < ventasMatch.Count; i++)
        {
            var v = ventasMatch[i];
            var numLinea = v.NumeroLinea;
            var serie = (v.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (v.Numero ?? string.Empty).Trim();
            var fechaEmision = v.FechaEmision;

            // 1. Fecha de emisión dentro del periodo
            if (fechaEmision.Date < primerDiaPeriodo.Date || fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                var esFechaSuperior = string.Compare(periodoFecha, periodo, StringComparison.Ordinal) > 0;
                var sevFecha = esFechaSuperior ? SeveridadError.Error : SeveridadError.Advertencia;

                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} correspondiente al periodo '{periodoFecha}' (el periodo seleccionado es '{periodo}')",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: sevFecha));
            }

            // 2. Documento de Identidad (DNI / RUC)
            var tipoDoc = (v.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            var nroDoc = (v.NroDocIdentidad ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: 8 dígitos numéricos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El comprobante Serie '{serie}', Número '{numero}' tiene el documento '{nroDoc}' que debe contener exactamente 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: 11 dígitos numéricos que inicien con 10, 20, 15 o 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento RUC inválido: El comprobante Serie '{serie}', Número '{numero}' tiene el RUC '{nroDoc}' que debe contener exactamente 11 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                    else
                    {
                        var prefijo = nroDoc.Substring(0, 2);
                        if (prefijo != "10" && prefijo != "20" && prefijo != "15" && prefijo != "17")
                        {
                            errores.Add(ArchivoCargaError.Crear(
                                idCarga,
                                numLinea,
                                TipoErrorCarga.Negocio,
                                $"Documento RUC con prefijo inválido: El RUC '{nroDoc}' del comprobante Serie '{serie}', Número '{numero}' debe iniciar con 10, 20, 15 o 17.",
                                campoError: "nro_doc_identidad",
                                valorLectura: nroDoc,
                                severidad: SeveridadError.Advertencia));
                        }
                    }
                }
            }
        }

        // 3. Correlatividad interna de series
        var porSerie = ventasMatch
            .GroupBy(v => new
            {
                TipoCp = v.CodigoTipoCp ?? string.Empty,
                Serie = (v.Serie ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numeros = grupo
                .Select(v => long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (numeros.Count < 2) continue;

            for (var i = 1; i < numeros.Count; i++)
            {
                var anterior = numeros[i - 1];
                var actual = numeros[i];
                var diferencia = actual - anterior;

                if (diferencia == 2)
                {
                    var faltante = anterior + 1;
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga,
                        1,
                        TipoErrorCarga.Secuencia,
                        $"Salto de correlatividad: No se encontró el registro para el número correlativo '{faltante}' de la Serie '{grupo.Key.Serie}'",
                        campoError: "numero",
                        valorLectura: faltante.ToString(),
                        severidad: SeveridadError.Error));
                }
                else if (diferencia > 2)
                {
                    var desde = anterior + 1;
                    var hasta = actual - 1;
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga,
                        1,
                        TipoErrorCarga.Secuencia,
                        $"Salto múltiple de correlatividad: No se encontraron los registros de los números correlativos desde '{desde}' hasta '{hasta}' de la Serie '{grupo.Key.Serie}'",
                        campoError: "numero",
                        valorLectura: $"{desde}-{hasta}",
                        severidad: SeveridadError.Error));
                }
            }
        }

        // 4. Correlatividad respecto al periodo anterior
        var periodoAnterior = ObtenerPeriodoAnterior(periodo);
        foreach (var grupo in porSerie)
        {
            var numerosCargados = grupo
                .Select(v => long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();

            if (numerosCargados.Count == 0) continue;

            var menorCargado = numerosCargados.First();

            var numerosPeriodoAnterior = await _ventaEmpresaRepo.ObtenerNumerosPorSerieYPeriodoAsync(
                empresaRuc, periodoAnterior, grupo.Key.TipoCp, grupo.Key.Serie, cancellationToken);

            if (numerosPeriodoAnterior != null && numerosPeriodoAnterior.Count > 0)
            {
                var mayoresPeriodoAnterior = numerosPeriodoAnterior
                    .Select(numStr => long.TryParse(numStr?.Trim(), out var n) ? (long?)n : null)
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .OrderByDescending(n => n)
                    .ToList();

                if (mayoresPeriodoAnterior.Count > 0)
                {
                    var mayorAnterior = mayoresPeriodoAnterior.First();
                    var esperado = mayorAnterior + 1;

                    if (menorCargado != esperado)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            1,
                            TipoErrorCarga.Secuencia,
                            $"Discontinuidad con periodo anterior: El número inicial de la Serie '{grupo.Key.Serie}' es '{menorCargado}', pero el último número del periodo anterior ({periodoAnterior}) fue '{mayorAnterior}' (se esperaba '{esperado}')",
                            campoError: "numero",
                            valorLectura: menorCargado.ToString(),
                            severidad: SeveridadError.Advertencia));
                    }
                }
            }
        }

        return errores;
    }

    private static string NormalizarNumeroComprobante(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return string.Empty;

        var limpio = numero.Trim();
        if (limpio.All(char.IsDigit))
        {
            var sinCeros = limpio.TrimStart('0');
            return string.IsNullOrEmpty(sinCeros) ? "0" : sinCeros;
        }

        return limpio.ToUpperInvariant();
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
}
