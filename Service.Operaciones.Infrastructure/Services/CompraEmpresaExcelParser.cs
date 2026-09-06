using System.Globalization;
using System.Text;
using ExcelDataReader;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

public class CompraEmpresaExcelParser : ICompraEmpresaParser
{
    static CompraEmpresaExcelParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Task<List<ResultadoParseoCompraEmpresaLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken)
    {
        var resultados = new List<ResultadoParseoCompraEmpresaLinea>();

        using var reader = ExcelReaderFactory.CreateReader(stream);
        var numeroFila = 0;
        var mapaColumnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            numeroFila++;

            // Leer cabecera (Fila 1) para mapear columnas por nombre de forma dinámica
            if (numeroFila == 1)
            {
                for (var c = 0; c < reader.FieldCount; c++)
                {
                    var nombreCabecera = reader.GetValue(c)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(nombreCabecera))
                    {
                        var keyNormalizada = NormalizarCabecera(nombreCabecera);
                        if (!mapaColumnas.ContainsKey(keyNormalizada))
                        {
                            mapaColumnas[keyNormalizada] = c;
                        }
                    }
                }
                continue;
            }

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

            var esFormato27Cols = reader.FieldCount >= 25;

            var resultadoLinea = new ResultadoParseoCompraEmpresaLinea
            {
                NumeroLinea = numeroFila,
                EsValido = true
            };

            try
            {
                // 1. FECHA EMISION
                var fechaRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 0, "FECHA EMISION", "FECHA DE EMISION", "FECHA");
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
                    resultadoLinea.ErrorMensaje = $"Formato de Fecha de Emisión inválido: '{fechaRaw}'";
                    resultadoLinea.CampoError = "FECHA EMISION";
                }

                // 2. FECHA VCTO
                var fVctoRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 1, "FECHA VCTO", "FECHA DE VENCIMIENTO", "FECHA VENCIMIENTO")?.Trim();
                if (!string.IsNullOrWhiteSpace(fVctoRaw) && TryParseFecha(fVctoRaw, out var dtVcto))
                {
                    resultadoLinea.FechaVencimiento = dtVcto;
                }

                // 3. TIPO CP
                var tipoCpRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 2, "TIPO CP", "TIPO DOC", "TIPO COMPROBANTE")?.Trim();
                if (string.IsNullOrWhiteSpace(tipoCpRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Tipo de Comprobante es obligatorio";
                    resultadoLinea.CampoError = "TIPO CP";
                }
                else
                {
                    resultadoLinea.CodigoTipoCp = tipoCpRaw.PadLeft(2, '0');
                }

                // 4. SERIE
                var serieRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 3, "SERIE", "SERIE CP")?.Trim();
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

                // 5. AÑO DUA
                resultadoLinea.AnioDocumento = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 4, "AÑO DUA", "ANO DUA", "ANIO DUA", "AÑO", "ANO")?.Trim();

                // 6. NUMERO
                var numeroRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 5, "NUMERO", "NUMERO DOC", "NUMERO CP")?.Trim();
                if (string.IsNullOrWhiteSpace(numeroRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Número de Comprobante es obligatorio";
                    resultadoLinea.CampoError = "NUMERO";
                }
                else
                {
                    resultadoLinea.Numero = numeroRaw;
                }

                // 7. TIPO DOC (Tipo doc proveedor)
                var tipoDocProvRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 6, "TIPO DOC", "TIPO ANEXO", "TIPO DOC PROVEEDOR", "TIPO DOCUMENTO PROVEEDOR")?.Trim();

                // 8. RUC / DOC PROVEEDOR (ANEXO)
                var nroDocProvRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 7, "RUC / DOC PROVEEDOR", "RUC DOC PROVEEDOR", "ANEXO", "RUC PROVEEDOR", "DOC PROVEEDOR", "NRO DOC PROVEEDOR")?.Trim();
                if (string.IsNullOrWhiteSpace(nroDocProvRaw))
                {
                    resultadoLinea.EsValido = false;
                    resultadoLinea.ErrorMensaje = "El Documento / RUC del proveedor es obligatorio";
                    resultadoLinea.CampoError = "RUC / DOC PROVEEDOR";
                    resultadoLinea.NroDocIdentidad = "-";
                }
                else
                {
                    resultadoLinea.NroDocIdentidad = nroDocProvRaw;
                }

                // Deducción de tipo de documento
                if (!string.IsNullOrWhiteSpace(tipoDocProvRaw))
                {
                    resultadoLinea.CodigoTipoDocIdentidad = tipoDocProvRaw;
                }
                else if (!string.IsNullOrWhiteSpace(nroDocProvRaw))
                {
                    if (nroDocProvRaw.Length == 11 && nroDocProvRaw.All(char.IsDigit))
                        resultadoLinea.CodigoTipoDocIdentidad = "6";
                    else if (nroDocProvRaw.Length == 8 && nroDocProvRaw.All(char.IsDigit))
                        resultadoLinea.CodigoTipoDocIdentidad = "1";
                    else
                        resultadoLinea.CodigoTipoDocIdentidad = "0";
                }
                else
                {
                    resultadoLinea.CodigoTipoDocIdentidad = "0";
                }

                // 9. RAZON SOCIAL PROVEEDOR
                resultadoLinea.RazonSocial = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 8, "RAZON SOCIAL PROVEEDOR", "PROVEEDOR", "RAZON SOCIAL", "NOMBRE PROVEEDOR")?.Trim() ?? string.Empty;

                // 10. BASE IMPONIBLE GRAVADA
                resultadoLinea.BiGravadoDg = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 9, "BASE IMPONIBLE GRAVADA", "BASE IMPONIBLE OPER. GRAVADAS", "BASE IMPONIBLE OPER GRAVADAS", "BASE IMPONIBLE", "BI GRAVADO DG"));

                // 11. IGV / IPM
                resultadoLinea.IgvIpmDg = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 10, "IGV / IPM", "IGV", "IGV IPM DG"));

                if (esFormato27Cols)
                {
                    // 12. BI OPERACIONES MIXTAS
                    resultadoLinea.BiGravadoDgng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 11, "BI OPERACIONES MIXTAS", "BASE IMPONIBLE OPER. MIXTAS", "BASE IMPONIBLE OPER MIXTAS", "BI GRAVADO DGNG"));

                    // 13. IGV OPERACIONES MIXTAS
                    resultadoLinea.IgvIpmDgng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 12, "IGV OPERACIONES MIXTAS", "IGV OPER. MIXTAS", "IGV OPER MIXTAS", "IGV IPM DGNG"));

                    // 14. BI SIN CREDITO FISCAL
                    resultadoLinea.BiGravadoDng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 13, "BI SIN CREDITO FISCAL", "BASE IMPONIBLE (SIN CRÉDITO)", "BASE IMPONIBLE (SIN CREDITO)", "BASE IMPONIBLE SIN CREDITO", "BI GRAVADO DNG"));

                    // 15. IGV SIN CREDITO FISCAL
                    resultadoLinea.IgvIpmDng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 14, "IGV SIN CREDITO FISCAL", "IGV (SIN CRÉDITO)", "IGV (SIN CREDITO)", "IGV SIN CREDITO", "IGV IPM DNG"));

                    // 16. NO GRAVADAS
                    resultadoLinea.ValorAdqNg = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 15, "NO GRAVADAS", "VALOR ADQUISICION NO GRAVADA", "VALOR ADQ NG"));

                    // 17. ISC
                    resultadoLinea.MontoIsc = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 16, "ISC", "MONTO ISC"));

                    // 18. OTROS TRIBUTOS Y CARGOS
                    resultadoLinea.MontoOtrosTributos = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 17, "OTROS TRIBUTOS Y CARGOS", "OTROS TRIBUTOS", "MONTO OTROS TRIBUTOS"));

                    // 19. TOTAL CP
                    var totalCpRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 18, "TOTAL CP", "IMPORTE TOTAL", "TOTAL")?.Trim();
                    if (string.IsNullOrWhiteSpace(totalCpRaw27))
                    {
                        resultadoLinea.EsValido = false;
                        resultadoLinea.ErrorMensaje = "El Importe Total es obligatorio";
                        resultadoLinea.CampoError = "TOTAL CP";
                    }
                    else if (decimal.TryParse(totalCpRaw27.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var total27))
                    {
                        resultadoLinea.TotalCp = total27;
                    }
                    else
                    {
                        resultadoLinea.EsValido = false;
                        resultadoLinea.ErrorMensaje = $"Importe Total inválido: '{totalCpRaw27}'";
                        resultadoLinea.CampoError = "TOTAL CP";
                    }

                    // 20. COMPR NO DOMICIL
                    var comprNoDom = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 19, "COMPROBANTE NO DOMICILIADO", "COMPR NO DOMICIL", "NO DOMICILIADO")?.Trim();
                    resultadoLinea.CarOrigIndEI = comprNoDom;

                    // 21 & 22. DETRACCION (Número y Fecha)
                    var nroDetraccionRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 20, "NUMERO DETRACCION", "NÚMERO DETRACCION", "NRO DETRACCION")?.Trim();
                    var fDetraccionRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 21, "FECHA DETRACCION", "FECHA DE EMISIÓN DETRACCION", "FECHA EMISION DETRACCION")?.Trim();
                    if (!string.IsNullOrWhiteSpace(nroDetraccionRaw27) || !string.IsNullOrWhiteSpace(fDetraccionRaw27))
                    {
                        var partes = new List<string>();
                        if (!string.IsNullOrWhiteSpace(nroDetraccionRaw27)) partes.Add($"Nro: {nroDetraccionRaw27}");
                        if (!string.IsNullOrWhiteSpace(fDetraccionRaw27)) partes.Add($"Fecha: {fDetraccionRaw27}");
                        resultadoLinea.Detraccion = string.Join(" | ", partes);
                    }

                    // 23. TIPO CAMBIO
                    var tcRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 22, "TIPO CAMBIO", "TC")?.Trim();
                    if (string.IsNullOrWhiteSpace(tcRaw27))
                    {
                        resultadoLinea.TipoCambio = 1.0000m;
                    }
                    else if (decimal.TryParse(tcRaw27.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var tc27))
                    {
                        resultadoLinea.TipoCambio = tc27 > 0 ? tc27 : 1.0000m;
                    }
                    else
                    {
                        resultadoLinea.TipoCambio = 1.0000m;
                    }

                    // 24. FECHA EMISION DOC MODIFICADO
                    var fRefRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 23, "FECHA EMISION DOC MODIFICADO", "FECHA DOC REF")?.Trim();
                    if (!string.IsNullOrWhiteSpace(fRefRaw27) && TryParseFecha(fRefRaw27, out var dtRef27))
                    {
                        resultadoLinea.FechaEmisionDocModificado = dtRef27;
                    }

                    // 25. TIPO CP MODIFICADO
                    var tipoRefRaw27 = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 24, "TIPO CP MODIFICADO", "TIPO DOC REF")?.Trim();
                    if (!string.IsNullOrWhiteSpace(tipoRefRaw27))
                    {
                        resultadoLinea.CodigoTipoCpModificado = tipoRefRaw27.PadLeft(2, '0');
                    }

                    // 26. SERIE CP MODIFICADO
                    resultadoLinea.SerieCpModificado = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 25, "SERIE CP MODIFICADO", "SERIE DOC REF")?.Trim().ToUpperInvariant();

                    // 27. NUMERO CP MODIFICADO
                    resultadoLinea.NumeroCpModificado = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 26, "NUMERO CP MODIFICADO", "NUMERO DOC REF")?.Trim();
                }
                else
                {
                    // Formato compacto de 20 columnas
                    resultadoLinea.BiGravadoDgng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "BI OPERACIONES MIXTAS", "BASE IMPONIBLE OPER. MIXTAS", "BASE IMPONIBLE OPER MIXTAS", "BI GRAVADO DGNG"));
                    resultadoLinea.IgvIpmDgng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "IGV OPERACIONES MIXTAS", "IGV OPER. MIXTAS", "IGV OPER MIXTAS", "IGV IPM DGNG"));
                    resultadoLinea.BiGravadoDng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "BI SIN CREDITO FISCAL", "BASE IMPONIBLE (SIN CRÉDITO)", "BASE IMPONIBLE (SIN CREDITO)", "BASE IMPONIBLE SIN CREDITO", "BI GRAVADO DNG"));
                    resultadoLinea.IgvIpmDng = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "IGV SIN CREDITO FISCAL", "IGV (SIN CRÉDITO)", "IGV (SIN CREDITO)", "IGV SIN CREDITO", "IGV IPM DNG"));

                    // 12. NO GRAVADAS
                    resultadoLinea.ValorAdqNg = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, 11, "NO GRAVADAS", "VALOR ADQUISICION NO GRAVADA", "VALOR ADQ NG"));

                    // 13. ISC Y OTROS
                    resultadoLinea.MontoIsc = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "ISC", "MONTO ISC"));
                    resultadoLinea.MontoOtrosTributos = ParseDecimal(ObtenerValorPorNombreOFallback(reader, mapaColumnas, -1, "OTROS TRIBUTOS Y CARGOS", "OTROS TRIBUTOS", "MONTO OTROS TRIBUTOS"));

                    // 14. TOTAL CP
                    var totalCpRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 12, "TOTAL CP", "IMPORTE TOTAL", "TOTAL")?.Trim();
                    if (string.IsNullOrWhiteSpace(totalCpRaw))
                    {
                        resultadoLinea.EsValido = false;
                        resultadoLinea.ErrorMensaje = "El Importe Total es obligatorio";
                        resultadoLinea.CampoError = "TOTAL CP";
                    }
                    else if (decimal.TryParse(totalCpRaw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                    {
                        resultadoLinea.TotalCp = total;
                    }
                    else
                    {
                        resultadoLinea.EsValido = false;
                        resultadoLinea.ErrorMensaje = $"Importe Total inválido: '{totalCpRaw}'";
                        resultadoLinea.CampoError = "TOTAL CP";
                    }

                    // DETRACCION (Número y Fecha)
                    var nroDetraccionRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 13, "NUMERO DETRACCION", "NÚMERO DETRACCION", "NRO DETRACCION")?.Trim();
                    var fDetraccionRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 14, "FECHA DETRACCION", "FECHA DE EMISIÓN DETRACCION", "FECHA EMISION DETRACCION")?.Trim();
                    if (!string.IsNullOrWhiteSpace(nroDetraccionRaw) || !string.IsNullOrWhiteSpace(fDetraccionRaw))
                    {
                        var partes = new List<string>();
                        if (!string.IsNullOrWhiteSpace(nroDetraccionRaw)) partes.Add($"Nro: {nroDetraccionRaw}");
                        if (!string.IsNullOrWhiteSpace(fDetraccionRaw)) partes.Add($"Fecha: {fDetraccionRaw}");
                        resultadoLinea.Detraccion = string.Join(" | ", partes);
                    }

                    // TIPO CAMBIO
                    var tcRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 15, "TIPO CAMBIO", "TC")?.Trim();
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

                    // DOCUMENTO REFERENCIA MODIFICADO
                    var fRefRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 16, "FECHA EMISION DOC MODIFICADO", "FECHA DOC REF")?.Trim();
                    if (!string.IsNullOrWhiteSpace(fRefRaw) && TryParseFecha(fRefRaw, out var dtRef))
                    {
                        resultadoLinea.FechaEmisionDocModificado = dtRef;
                    }

                    var tipoRefRaw = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 17, "TIPO CP MODIFICADO", "TIPO DOC REF")?.Trim();
                    if (!string.IsNullOrWhiteSpace(tipoRefRaw))
                    {
                        resultadoLinea.CodigoTipoCpModificado = tipoRefRaw.PadLeft(2, '0');
                    }

                    resultadoLinea.SerieCpModificado = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 18, "SERIE CP MODIFICADO", "SERIE DOC REF")?.Trim().ToUpperInvariant();
                    resultadoLinea.NumeroCpModificado = ObtenerValorPorNombreOFallback(reader, mapaColumnas, 19, "NUMERO CP MODIFICADO", "NUMERO DOC REF")?.Trim();
                }

                // Valores complementarios
                resultadoLinea.NumeroFinal = string.Empty;
                resultadoLinea.MontoIcbper = 0;
                resultadoLinea.Imb = 0;
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

    private static string NormalizarCabecera(string cabecera)
    {
        return cabecera
            .Trim()
            .ToUpperInvariant()
            .Replace("Á", "A")
            .Replace("É", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ú", "U")
            .Replace(".", "")
            .Replace("_", " ")
            .Replace("  ", " ");
    }

    private static string? ObtenerValorPorNombreOFallback(
        IExcelDataReader reader,
        Dictionary<string, int> mapa,
        int indiceFallback,
        params string[] nombresPosibles)
    {
        foreach (var nombre in nombresPosibles)
        {
            var keyNormalizada = NormalizarCabecera(nombre);
            if (mapa.TryGetValue(keyNormalizada, out var indice) && indice < reader.FieldCount)
            {
                var val = ObtenerValorCelda(reader, indice);
                if (val != null) return val;
            }
        }

        if (indiceFallback >= 0 && indiceFallback < reader.FieldCount)
        {
            return ObtenerValorCelda(reader, indiceFallback);
        }

        return null;
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
        if (raw.Trim() == "-" || raw.Trim() == "—") return 0;
        if (decimal.TryParse(raw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
        {
            return valor;
        }
        return valorDefecto;
    }

    private static string? ObtenerValorCelda(IExcelDataReader reader, int indice)
    {
        if (indice < 0 || indice >= reader.FieldCount) return null;
        var valor = reader.GetValue(indice);
        if (valor == null) return null;

        if (valor is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return valor.ToString();
    }
}
