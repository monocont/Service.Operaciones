using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

/// <summary>
/// Parser de archivo de Compras SIRE (RCE) en formato CSV (delimitador coma ,).
/// Reutiliza el mapeo oficial de 41 columnas con delimitador coma y omite CLU1..39.
/// </summary>
public class CompraSireCsvParser : CompraSireTxtParser, IArchivoSunatParser
{
    private static readonly string[] NombresColumnasCsv = new[]
    {
        "ruc", "razon_social_empresa", "periodo", "car_sunat",
        "fecha_emision", "fecha_vencimiento", "tipo_cp", "serie", "anio_documento",
        "numero", "numero_final", "tipo_doc_identidad", "nro_doc_identidad", "razon_social",
        "bi_gravado_dg", "igv_ipm_dg", "bi_gravado_dgng", "igv_ipm_dgng",
        "bi_gravado_dng", "igv_ipm_dng", "valor_adq_ng",
        "monto_isc", "monto_icbper", "monto_otros_tributos", "total_cp",
        "moneda", "tipo_cambio",
        "fecha_emision_doc_modificado", "tipo_cp_modificado", "serie_cp_modificado",
        "cod_dam_dsi", "numero_cp_modificado", "clasif_bss_sss",
        "id_proyecto_op", "porc_part", "imb", "car_orig_ind_e_i", "detraccion",
        "tipo_nota", "estado_comprobante", "incal"
    };

    public CompraSireCsvParser()
    {
        Delimitador = ',';
    }

    public new async Task<List<ResultadoParseoLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken)
    {
        var resultados = new List<ResultadoParseoLinea>();
        using var reader = new StreamReader(stream);

        var numeroLinea = 0;
        var esEncabezado = true;

        string? linea;
        while ((linea = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            numeroLinea++;
            linea = linea.Trim();

            if (string.IsNullOrEmpty(linea))
            {
                continue;
            }

            if (esEncabezado && EsEncabezadoCsv(linea))
            {
                esEncabezado = false;
                continue;
            }
            esEncabezado = false;

            var campos = linea.Split(',');

            if (campos.Length < 25)
            {
                resultados.Add(new ResultadoParseoLinea
                {
                    NumeroLinea = numeroLinea,
                    EsValido = false,
                    MensajeError = $"La línea tiene {campos.Length} columnas, se esperaban al menos 25"
                });
                continue;
            }

            var diccionario = new Dictionary<string, string>();
            var limite = Math.Min(campos.Length, NombresColumnasCsv.Length);
            for (var i = 0; i < limite; i++)
            {
                diccionario[NombresColumnasCsv[i]] = campos[i].Trim();
            }

            if (!diccionario.ContainsKey("estado_comprobante") || string.IsNullOrWhiteSpace(diccionario["estado_comprobante"]))
            {
                diccionario["estado_comprobante"] = "1";
            }

            var errores = ValidarCamposObligatorios(diccionario);
            if (errores.Count > 0)
            {
                resultados.Add(new ResultadoParseoLinea
                {
                    NumeroLinea = numeroLinea,
                    EsValido = false,
                    MensajeError = string.Join("; ", errores)
                });
                continue;
            }

            resultados.Add(new ResultadoParseoLinea
            {
                NumeroLinea = numeroLinea,
                EsValido = true,
                Campos = diccionario
            });
        }

        return resultados;
    }

    private static bool EsEncabezadoCsv(string linea)
    {
        var primeraColumna = linea.Split(',')[0].Trim().TrimStart('\uFEFF');
        return !primeraColumna.All(char.IsDigit) || primeraColumna.Length != 11;
    }

    private static List<string> ValidarCamposObligatorios(Dictionary<string, string> c)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("tipo_cp")))
        {
            errores.Add("tipo_cp es obligatorio");
        }
        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("serie")))
        {
            errores.Add("serie es obligatoria");
        }
        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("numero")))
        {
            errores.Add("numero es obligatorio");
        }
        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("nro_doc_identidad")))
        {
            errores.Add("nro_doc_identidad es obligatorio");
        }
        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("razon_social")))
        {
            errores.Add("razon_social es obligatoria");
        }

        return errores;
    }
}
