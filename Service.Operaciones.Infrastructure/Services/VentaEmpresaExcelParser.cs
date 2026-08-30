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
                else if (DateTime.TryParse(fechaRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                         DateTime.TryParseExact(fechaRaw, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    resultadoLinea.FechaEmision = dt;
                }
                else
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = $"Formato de Fecha inválido: '{fechaRaw}'";
                    resultadoLinea.CampoError = "FECHA EMISION";
                }

                // Col 1: TIPO CP
                var tipoCpRaw = ObtenerValorCelda(reader, 1)?.Trim();
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

                // Col 2: SERIE
                var serieRaw = ObtenerValorCelda(reader, 2)?.Trim();
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

                // Col 3: NUMERO
                var numeroRaw = ObtenerValorCelda(reader, 3)?.Trim();
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

                // Col 4: TIPO DOC
                var tipoDocRaw = ObtenerValorCelda(reader, 4)?.Trim();
                resultadoLinea.CodigoTipoDocIdentidad = string.IsNullOrWhiteSpace(tipoDocRaw) ? "0" : tipoDocRaw;

                // Col 5: RUC / DOC CLIENTE
                var nroDocRaw = ObtenerValorCelda(reader, 5)?.Trim();
                resultadoLinea.NroDocIdentidad = string.IsNullOrWhiteSpace(nroDocRaw) ? "-" : nroDocRaw;

                // Col 6: TOTAL CP
                var totalCpRaw = ObtenerValorCelda(reader, 6)?.Trim();
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

                // Col 7: MONEDA
                var monedaRaw = ObtenerValorCelda(reader, 7)?.Trim().ToUpperInvariant();
                resultadoLinea.CodigoMoneda = string.IsNullOrWhiteSpace(monedaRaw) ? "PEN" : monedaRaw;

                // Col 8: TIPO CAMBIO
                var tcRaw = ObtenerValorCelda(reader, 8)?.Trim();
                if (string.IsNullOrWhiteSpace(tcRaw))
                {
                    resultadoLinea.TipoCambio = 1.0000m;
                }
                else if (decimal.TryParse(tcRaw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var tc))
                {
                    resultadoLinea.TipoCambio = tc;
                }
                else
                {
                    resultadoLinea.TipoCambio = 1.0000m;
                }
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
