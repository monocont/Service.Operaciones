using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Services;

public class VentaEmpresaValidationService : IVentaEmpresaValidationService
{
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;

    public VentaEmpresaValidationService(IVentaEmpresaRepository ventaEmpresaRepo)
    {
        _ventaEmpresaRepo = ventaEmpresaRepo;
    }

    public async Task<List<ArchivoCargaError>> ValidarVentasEmpresaAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<VentaEmpresa> ventasEmpresa,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (ventasEmpresa == null || ventasEmpresa.Count == 0)
        {
            return errores;
        }

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var primerDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, 1);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        var comprobantesPorSerieNumero = new Dictionary<string, List<int>>();

        for (var i = 0; i < ventasEmpresa.Count; i++)
        {
            var venta = ventasEmpresa[i];
            var numeroLinea = venta.NumeroLinea > 0 ? venta.NumeroLinea : i + 1;

            var serie = (venta.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (venta.Numero ?? string.Empty).Trim();
            var fechaEmision = venta.FechaEmision;
            var totalCp = venta.TotalCp;

            // Validación 1: Agrupar para duplicados por Serie + Número
            if (!string.IsNullOrWhiteSpace(serie) && !string.IsNullOrWhiteSpace(numero))
            {
                var clave = $"{serie}|{numero}";
                if (!comprobantesPorSerieNumero.ContainsKey(clave))
                {
                    comprobantesPorSerieNumero[clave] = new List<int>();
                }
                comprobantesPorSerieNumero[clave].Add(numeroLinea);
            }

            // Validación 2: Fecha de emisión en el periodo seleccionado
            if (fechaEmision.Date < primerDiaPeriodo.Date || fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                var esFechaSuperior = string.Compare(periodoFecha, periodo, StringComparison.Ordinal) > 0;
                var sevFecha = esFechaSuperior ? SeveridadError.Error : SeveridadError.Advertencia;

                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} correspondiente al periodo '{periodoFecha}' (el periodo seleccionado es '{periodo}')",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: sevFecha));
            }


            // Validación 4: Documento de Identidad del Cliente (DNI, RUC y Genéricos/Extranjeros)
            var tipoDoc = (venta.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            var nroDoc = (venta.NroDocIdentidad ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: Exactamente 8 dígitos numéricos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El comprobante Serie '{serie}', Número '{numero}' tiene el documento '{nroDoc}' que debe contener exactamente 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: Exactamente 11 dígitos numéricos que comiencen con 10, 20, 15 o 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
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
                                numeroLinea,
                                TipoErrorCarga.Negocio,
                                $"Documento RUC con prefijo inválido: El RUC '{nroDoc}' del comprobante Serie '{serie}', Número '{numero}' debe iniciar con 10, 20, 15 o 17.",
                                campoError: "nro_doc_identidad",
                                valorLectura: nroDoc,
                                severidad: SeveridadError.Advertencia));
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(nroDoc)) // Genéricos / Extranjería / Pasaporte
                {
                    if (nroDoc.Length > 15 || !nroDoc.All(char.IsLetterOrDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento inválido: El documento '{nroDoc}' del comprobante Serie '{serie}', Número '{numero}' contiene caracteres no permitidos o excede 15 caracteres.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
            }
        }

        // Validación 5: Registro de Duplicados
        foreach (var kvp in comprobantesPorSerieNumero.Where(kvp => kvp.Value.Count > 1))
        {
            var partes = kvp.Key.Split('|');
            var s = partes[0];
            var n = partes[1];
            var cant = kvp.Value.Count;
            var primeraLinea = kvp.Value.First();

            errores.Add(ArchivoCargaError.Crear(
                idCarga,
                primeraLinea,
                TipoErrorCarga.Duplicado,
                $"Comprobante duplicado: La Serie '{s}', Número '{n}' se encuentra registrada {cant} veces en el archivo",
                campoError: "serie_numero",
                valorLectura: $"{s}-{n}",
                severidad: SeveridadError.Error));
        }

        // Validación 6: Correlatividad interna entre comprobantes (saltos y números faltantes)
        var advertenciasSecuenciaInterna = ValidarSeriesConsecutivas(ventasEmpresa, idCarga);
        errores.AddRange(advertenciasSecuenciaInterna);

        // Validación 7: Correlatividad contra Periodo Anterior
        var periodoAnterior = ObtenerPeriodoAnterior(periodo);
        var advertenciasPeriodoAnterior = await ValidarCorrelativoPeriodoAnteriorAsync(
            empresaRuc, periodoAnterior, ventasEmpresa, idCarga, cancellationToken);
        errores.AddRange(advertenciasPeriodoAnterior);

        return errores;
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

    private static List<ArchivoCargaError> ValidarSeriesConsecutivas(List<VentaEmpresa> ventas, Guid idCarga)
    {
        var errores = new List<ArchivoCargaError>();

        var porSerie = ventas
            .GroupBy(v => new
            {
                TipoCp = v.CodigoTipoCp ?? string.Empty,
                Serie = (v.Serie ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numeros = grupo
                .Select(v => new
                {
                    NumeroStr = v.Numero ?? string.Empty,
                    Parseado = long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null
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

        return errores;
    }

    private async Task<List<ArchivoCargaError>> ValidarCorrelativoPeriodoAnteriorAsync(
        string empresaRuc,
        string periodoAnterior,
        List<VentaEmpresa> ventas,
        Guid idCarga,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();

        var porSerie = ventas
            .GroupBy(v => new
            {
                TipoCp = v.CodigoTipoCp ?? string.Empty,
                Serie = (v.Serie ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numerosCargados = grupo
                .Select(v => long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();

            if (numerosCargados.Count == 0)
            {
                continue;
            }

            var menorCargado = numerosCargados.First();

            var numerosPeriodoAnterior = await _ventaEmpresaRepo.ObtenerNumerosPorSerieYPeriodoAsync(
                empresaRuc, periodoAnterior, grupo.Key.TipoCp, grupo.Key.Serie, cancellationToken);

            if (numerosPeriodoAnterior == null || numerosPeriodoAnterior.Count == 0)
            {
                continue;
            }

            var mayoresPeriodoAnterior = numerosPeriodoAnterior
                .Select(numStr => long.TryParse(numStr?.Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderByDescending(n => n)
                .ToList();

            if (mayoresPeriodoAnterior.Count == 0)
            {
                continue;
            }

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

        return errores;
    }
}
