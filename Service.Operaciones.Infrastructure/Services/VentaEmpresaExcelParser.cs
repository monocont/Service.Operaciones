using System.Globalization;
using System.Text;
using ExcelDataReader;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

public class VentaEmpresaExcelParser : IVentaEmpresaParser
{
    static VentaEmpresaExcelParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Task<List<ResultadoParseoVentaEmpresaLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken)
    {
        var resultados = new List<ResultadoParseoVentaEmpresaLinea>();

        using var reader = ExcelReaderFactory.CreateReader(stream);
        var numeroFila = 0;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            numeroFila++;

            // Omitir cabecera (Fila 1)
            if (numeroFila == 1) continue;

            // Verificar si toda la fila está vacía
            var vacia = true;
            for (var c = 0; c < reader.FieldCount; c++)
            {
                if (reader.GetValue(c) != null && !string.IsNullOrWhiteSpace(reader.GetValue(c)?.ToString()))
                {
                    vacia = false;
                    break;
                }
            }
            if (vacia) continue;

            var resultadoLinea = new ResultadoParseoVentaEmpresaLinea
            {
                NumeroLinea = numeroFila,
                EsValido = true
            };

            try
            {
                // Col 0: FECHA EMISION
                var fechaRaw = ObtenerValorCelda(reader, 0);
                if (string.IsNullOrWhiteSpace(fechaRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "La Fecha de Emisión es obligatoria";
                    resultadoLinea.CampoError = "FECHA EMISION";
                }
                else if (TryParseFecha(fechaRaw, out var dt))
                {
                    resultadoLinea.FechaEmision = dt;
                }
                else
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = $"Formato de Fecha inválido: '{fechaRaw}'";
                    resultadoLinea.CampoError = "FECHA EMISION";
                }

                // Col 1: FECHA VCTO
                var fVctoRaw = ObtenerValorCelda(reader, 1)?.Trim();
                if (!string.IsNullOrWhiteSpace(fVctoRaw) && TryParseFecha(fVctoRaw, out var dtVcto))
                {
                    resultadoLinea.FechaVencimiento = dtVcto;
                }

                // Col 2: TIPO CP
                var tipoCpRaw = ObtenerValorCelda(reader, 2)?.Trim();
                if (string.IsNullOrWhiteSpace(tipoCpRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Tipo de CP es obligatorio";
                    resultadoLinea.CampoError = "TIPO CP";
                }
                else
                {
                    resultadoLinea.CodigoTipoCp = tipoCpRaw.PadLeft(2, '0');
                }

                // Col 3: SERIE
                var serieRaw = ObtenerValorCelda(reader, 3)?.Trim();
                if (string.IsNullOrWhiteSpace(serieRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "La Serie es obligatoria";
                    resultadoLinea.CampoError = "SERIE";
                }
                else
                {
                    resultadoLinea.Serie = serieRaw.ToUpperInvariant();
                }

                // Col 4: NUMERO
                var numeroRaw = ObtenerValorCelda(reader, 4)?.Trim();
                if (string.IsNullOrWhiteSpace(numeroRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Número es obligatorio";
                    resultadoLinea.CampoError = "NUMERO";
                }
                else
                {
                    resultadoLinea.Numero = numeroRaw;
                }

                // Col 5: TIPO DOC CLIENTE
                var tipoDocRaw = ObtenerValorCelda(reader, 5)?.Trim();
                resultadoLinea.CodigoTipoDocIdentidad = string.IsNullOrWhiteSpace(tipoDocRaw) ? "0" : tipoDocRaw;

                // Col 6: RUC / DOC CLIENTE
                var nroDocRaw = ObtenerValorCelda(reader, 6)?.Trim();
                resultadoLinea.NroDocIdentidad = string.IsNullOrWhiteSpace(nroDocRaw) ? "-" : nroDocRaw;

                // Col 7: RAZON SOCIAL CLIENTE
                resultadoLinea.RazonSocial = ObtenerValorCelda(reader, 7)?.Trim() ?? string.Empty;

                // Col 8: VALOR FACTURADO EXPORTACION
                resultadoLinea.ValorFacturadoExportacion = ParseDecimal(ObtenerValorCelda(reader, 8));

                // Col 9: BASE IMPONIBLE GRAVADA
                resultadoLinea.BiGravada = ParseDecimal(ObtenerValorCelda(reader, 9));

                // Col 10: EXONERADO
                resultadoLinea.MontoExonerado = ParseDecimal(ObtenerValorCelda(reader, 10));

                // Col 11: INAFECTO
                resultadoLinea.MontoInafecto = ParseDecimal(ObtenerValorCelda(reader, 11));

                // Col 12: ISC
                resultadoLinea.MontoIsc = ParseDecimal(ObtenerValorCelda(reader, 12));

                // Col 13: IGV / IPM
                resultadoLinea.IgvIpm = ParseDecimal(ObtenerValorCelda(reader, 13));

                // Col 14: OTROS TRIBUTOS
                resultadoLinea.MontoOtrosTributos = ParseDecimal(ObtenerValorCelda(reader, 14));

                // Col 15: TOTAL CP
                var totalCpRaw = ObtenerValorCelda(reader, 15)?.Trim();
                if (string.IsNullOrWhiteSpace(totalCpRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Total CP es obligatorio";
                    resultadoLinea.CampoError = "TOTAL CP";
                }
                else if (decimal.TryParse(totalCpRaw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                {
                    resultadoLinea.TotalCp = total;
                }
                else
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = $"Total CP inválido: '{totalCpRaw}'";
                    resultadoLinea.CampoError = "TOTAL CP";
                }

                // Col 16: TIPO CAMBIO
                var tcRaw = ObtenerValorCelda(reader, 16)?.Trim();
                if (string.IsNullOrWhiteSpace(tcRaw))
                {
                    resultadoLinea.TipoCambio = 1.0000m;
                }
                else if (decimal.TryParse(tcRaw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var tc))
                {
                    resultadoLinea.TipoCambio = tc > 0 ? tc : 1.0000m;
                }
                else
                {
                    resultadoLinea.TipoCambio = 1.0000m;
                }

                // Col 17: FECHA EMISION DOC MODIFICADO
                var fModRaw = ObtenerValorCelda(reader, 17)?.Trim();
                if (!string.IsNullOrWhiteSpace(fModRaw) && TryParseFecha(fModRaw, out var dtMod))
                {
                    resultadoLinea.FechaEmisionDocModificado = dtMod;
                }

                // Col 18: TIPO CP MODIFICADO
                var tipoModRaw = ObtenerValorCelda(reader, 18)?.Trim();
                if (!string.IsNullOrWhiteSpace(tipoModRaw))
                {
                    resultadoLinea.CodigoTipoCpModificado = tipoModRaw.PadLeft(2, '0');
                }

                // Col 19: SERIE CP MODIFICADO
                resultadoLinea.SerieCpModificado = ObtenerValorCelda(reader, 19)?.Trim().ToUpperInvariant();

                // Col 20: NUMERO CP MODIFICADO
                resultadoLinea.NumeroCpModificado = ObtenerValorCelda(reader, 20)?.Trim();

                // Campos complementarios con valores por defecto
                resultadoLinea.NumeroFinal = string.Empty;
                resultadoLinea.DescuentoBi = 0;
                resultadoLinea.DescuentoIgv = 0;
                resultadoLinea.BiGravadaIvap = 0;
                resultadoLinea.MontoIvap = 0;
                resultadoLinea.MontoIcbper = 0;
                resultadoLinea.CodigoMoneda = (resultadoLinea.TipoCambio.HasValue && resultadoLinea.TipoCambio.Value > 1.0000m) ? "USD" : "PEN";
                resultadoLinea.CodigoTipoNota = string.Empty;
                resultadoLinea.CodigoEstadoComprobante = "1";
            }
            catch (Exception ex)
            {
                resultadoLinea.EsValido = false;
                resultadoLinea.ErrorMensaje = $"Error al leer la fila {numeroFila}: {ex.Message}";
            }

            resultados.Add(resultadoLinea);
        }

        return Task.FromResult(resultados);
    }

    private static readonly string[] FormatosFecha = new[]
    {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy HH:mm:ss",
        "d/M/yyyy HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss"
    };

    private static readonly CultureInfo CulturaPeru = CultureInfo.GetCultureInfo("es-PE");

    private static bool TryParseFecha(string raw, out DateTime fecha)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            fecha = default;
            return false;
        }

        var clean = raw.Trim();
        return DateTime.TryParseExact(clean, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha) ||
               DateTime.TryParseExact(clean, FormatosFecha, CulturaPeru, DateTimeStyles.None, out fecha) ||
               DateTime.TryParse(clean, CulturaPeru, DateTimeStyles.None, out fecha) ||
               DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private static decimal ParseDecimal(string? raw, decimal valorDefecto = 0)
    {
        if (string.IsNullOrWhiteSpace(raw)) return valorDefecto;
        if (decimal.TryParse(raw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
        {
            return valor;
        }
        return valorDefecto;
    }

    private static string? ObtenerValorCelda(IExcelDataReader reader, int indice)
    {
        if (indice >= reader.FieldCount) return null;
        var valor = reader.GetValue(indice);
        if (valor == null) return null;

        if (valor is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return valor.ToString();
    }
}
