using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

/// <summary>
/// Parser de archivo de Ventas en formato CSV (delimitador coma ,).
/// Reutiliza el mapeo del VentaTxtParser con delimitador distinto.
/// </summary>
public class VentaCsvParser : IArchivoSunatParser
{
    private static readonly string[] NombresColumnas = new[]
    {
        "ruc", "razon_social_empresa", "periodo", "car_sunat",
        "fecha_emision", "fecha_vcto_pago", "tipo_cp", "serie",
        "numero", "numero_final",
        "tipo_doc_identidad", "nro_doc_identidad", "razon_social",
        "valor_fact_exp", "bi_gravada", "dscto_bi", "igv_ipm", "dscto_igv_ipm",
        "monto_exonerado", "monto_inafecto", "isc",
        "bi_grav_ivap", "ivap", "icbper", "otros_tributos", "total_cp",
        "moneda", "tipo_cambio",
        "fecha_emision_doc_modif", "tipo_cp_modificado", "serie_cp_modificado", "nro_cp_modificado",
        "id_proyecto_op_attr", "tipo_nota", "estado_comprobante",
        "valor_fob_embar", "valor_op_gratuitas",
        "tipo_operacion", "dam_cp", "campos_libres"
    };

    public async Task<List<ResultadoParseoLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken)
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

            if (esEncabezado && EsEncabezado(linea))
            {
                esEncabezado = false;
                continue;
            }
            esEncabezado = false;

            // Detectar delimitador dinámicamente (soporta comas ',' y punto y coma ';')
            var delimitador = linea.Contains(';') ? ';' : ',';
            var campos = linea.Split(delimitador);

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
            for (var i = 0; i < Math.Min(campos.Length, NombresColumnas.Length); i++)
            {
                var valor = campos[i].Trim();
                if (valor == "-")
                {
                    valor = string.Empty;
                }
                diccionario[NombresColumnas[i]] = valor;
            }

            if (!diccionario.ContainsKey("estado_comprobante"))
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

    private static bool EsEncabezado(string linea)
    {
        var delimitador = linea.Contains(';') ? ';' : ',';
        var primeraColumna = linea.Split(delimitador)[0].Trim().TrimStart('\uFEFF');
        return !primeraColumna.All(char.IsDigit) || primeraColumna.Length != 11;
    }

    private static List<string> ValidarCamposObligatorios(Dictionary<string, string> c)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(c.GetValueOrDefault("car_sunat")))
        {
            errores.Add("car_sunat es obligatorio");
        }
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
