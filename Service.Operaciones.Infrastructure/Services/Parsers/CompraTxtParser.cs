using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

/// <summary>
/// Parser de archivo de Compras en formato TXT (delimitador pipe |).
/// Layout oficial SUNAT: 80 columnas.
/// Encabezado en la primera línea: RUC|Apellidos y Nombres o Razón social|Periodo|CAR SUNAT|...
/// </summary>
public class CompraTxtParser : ArchivoSunatParserBase, IArchivoSunatParser
{
    private static readonly string[] NombresColumnas = new[]
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
        // Nota: CLU1-CLU40 omitidos en el mapeo (se guardan como null por simplicidad)
    };

    public CompraTxtParser()
    {
        Delimitador = '|';
    }

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

            // Saltar encabezado (primera línea válida)
            if (esEncabezado && EsEncabezado(linea))
            {
                esEncabezado = false;
                continue;
            }
            esEncabezado = false;

            var campos = DividirLinea(linea);

            if (campos.Length < 25) // Mínimo de columnas obligatorias aprox
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
                diccionario[NombresColumnas[i]] = Limpiar(campos[i]) ?? string.Empty;
            }
            // Agregar campos restantes en caso de existir más allá de las columnas mapeadas
            if (campos.Length > NombresColumnas.Length)
            {
                // Los CLU1-CLU40 se ignoran en el mapeo, pero no rompen
            }

            // Validar campos obligatorios
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
        // El encabezado typically contiene texto como "RUC|Apellidos"
        // Verificamos que no comience con dígitos de RUC válidos
        var primeraColumna = linea.Split('|')[0];
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
