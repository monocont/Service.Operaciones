using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

/// <summary>
/// Parser de archivo de Compras en formato CSV (delimitador coma ,).
/// Reutiliza el mapeo del CompraTxtParser con delimitador distinto.
/// </summary>
public class CompraCsvParser : CompraTxtParser, IArchivoSunatParser
{
    public CompraCsvParser()
    {
        // Override del delimitador: en CSV es coma.
        // Nota: el base constructor ya ejecutó con pipe, lo reajustamos aquí.
    }

    protected new char Delimitador => ',';

    public new async Task<List<ResultadoParseoLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken)
    {
        // Implementación que usa el delimitador ',' en lugar de '|'
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
            var nombres = new[]
            {
                "ruc", "razon_social_empresa", "periodo", "car_sunat",
                "fecha_emision", "fecha_vcto_pago", "tipo_cp", "serie", "anio_documento",
                "numero", "numero_final", "tipo_doc_identidad", "nro_doc_identidad", "razon_social",
                "bi_gravado_dg", "igv_ipm_dg", "bi_gravado_dgng", "igv_ipm_dgng",
                "bi_gravado_dng", "igv_ipm_dng", "valor_adq_ng",
                "isc", "icbper", "otros_trib_cargos", "total_cp",
                "moneda", "tipo_cambio",
                "fecha_emision_doc_modif", "tipo_cp_modificado", "serie_cp_modificado",
                "cod_dam_dsi", "nro_cp_modificado", "clasif_bss_sss",
                "id_proyecto_op", "porc_part", "imb", "car_orig_ind_e_i", "detraccion",
                "tipo_nota", "estado_comprobante", "incal"
            };

            for (var i = 0; i < Math.Min(campos.Length, nombres.Length); i++)
            {
                diccionario[nombres[i]] = campos[i].Trim();
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
        var primeraColumna = linea.Split(',')[0];
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
