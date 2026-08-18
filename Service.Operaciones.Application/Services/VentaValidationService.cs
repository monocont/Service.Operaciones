using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Services;

public class VentaValidationService : IVentaValidationService
{
    private readonly IVentaRepository _ventaRepo;

    public VentaValidationService(IVentaRepository ventaRepo)
    {
        _ventaRepo = ventaRepo;
    }

    public async Task<List<ArchivoCargaError>> ValidarVentasAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<Venta> ventas,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (ventas == null || ventas.Count == 0)
        {
            return errores;
        }

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        var comprobantesPorSerieNumero = new Dictionary<string, List<int>>();

        for (var i = 0; i < ventas.Count; i++)
        {
            var venta = ventas[i];
            var numeroLinea = i + 1;

            var serie = (venta.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (venta.Numero ?? string.Empty).Trim();
            var periodoVenta = (venta.Periodo ?? string.Empty).Trim();
            var fechaEmision = venta.FechaEmision;

            // Validación 1: Duplicados por Serie + Número
            if (!string.IsNullOrWhiteSpace(serie) && !string.IsNullOrWhiteSpace(numero))
            {
                var clave = $"{serie}|{numero}";
                if (!comprobantesPorSerieNumero.ContainsKey(clave))
                {
                    comprobantesPorSerieNumero[clave] = new List<int>();
                }
                comprobantesPorSerieNumero[clave].Add(numeroLinea);
            }

            // Validación 4: Periodo
            if (!string.IsNullOrWhiteSpace(periodoVenta) && periodoVenta != periodo)
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"Periodo inconsistente: El comprobante Serie '{serie}', Número '{numero}' corresponde al periodo '{periodoVenta}', difiere del periodo cargado '{periodo}'",
                    campoError: "periodo",
                    valorLectura: periodoVenta,
                    severidad: SeveridadError.Warning));
            }

            // Validación 5: Fecha Máxima de Emisión
            if (fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} correspondiente al periodo '{periodoFecha}' (posterior al cierre {ultimoDiaPeriodo:dd/MM/yyyy})",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: SeveridadError.Warning));
            }
        }

        // Validación 1 (Registro de Duplicados)
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
                severidad: SeveridadError.Warning));
        }

        // Validación 2: Correlatividad interna entre comprobantes
        var advertenciasSecuenciaInterna = ValidarSeriesConsecutivas(ventas, idCarga);
        errores.AddRange(advertenciasSecuenciaInterna);

        // Validación 3: Correlatividad contra Periodo Anterior
        var periodoAnterior = ObtenerPeriodoAnterior(periodo);
        var advertenciasPeriodoAnterior = await ValidarCorrelativoPeriodoAnteriorAsync(
            empresaRuc, periodoAnterior, ventas, idCarga, cancellationToken);
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

    private static List<ArchivoCargaError> ValidarSeriesConsecutivas(List<Venta> ventas, Guid idCarga)
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
                        severidad: SeveridadError.Warning));
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
                        severidad: SeveridadError.Warning));
                }
            }
        }

        return errores;
    }

    private async Task<List<ArchivoCargaError>> ValidarCorrelativoPeriodoAnteriorAsync(
        string empresaRuc,
        string periodoAnterior,
        List<Venta> ventas,
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

            var numerosPeriodoAnterior = await _ventaRepo.ObtenerNumerosPorSerieYPeriodoAsync(
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
                    severidad: SeveridadError.Warning));
            }
        }

        return errores;
    }
}
