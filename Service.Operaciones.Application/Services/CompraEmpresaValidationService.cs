using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Services;

public class CompraEmpresaValidationService : ICompraEmpresaValidationService
{
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;

    public CompraEmpresaValidationService(ICompraEmpresaRepository compraEmpresaRepo)
    {
        _compraEmpresaRepo = compraEmpresaRepo;
    }

    public async Task<List<ArchivoCargaError>> ValidarComprasEmpresaAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<CompraEmpresa> compras,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (compras == null || compras.Count == 0)
        {
            return errores;
        }

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        var comprobantesPorClave = new Dictionary<string, List<int>>();

        for (var i = 0; i < compras.Count; i++)
        {
            var compra = compras[i];
            var numeroLinea = compra.NumeroLinea > 0 ? compra.NumeroLinea : i + 1;

            var serie = (compra.Serie ?? string.Empty).Trim().ToUpperInvariant();
            var numero = (compra.Numero ?? string.Empty).Trim();
            var tipoCp = (compra.CodigoTipoCp ?? string.Empty).Trim();
            var nroDoc = (compra.NroDocIdentidad ?? string.Empty).Trim();
            var fechaEmision = compra.FechaEmision;

            // 1. Detección de Duplicados Internos por Proveedor + Tipo + Serie + Número
            if (!string.IsNullOrWhiteSpace(nroDoc) && !string.IsNullOrWhiteSpace(serie) && !string.IsNullOrWhiteSpace(numero))
            {
                var clave = $"{nroDoc}|{tipoCp}|{serie}|{numero}";
                if (!comprobantesPorClave.ContainsKey(clave))
                {
                    comprobantesPorClave[clave] = new List<int>();
                }
                comprobantesPorClave[clave].Add(numeroLinea);
            }

            // 2. Validación de Fecha de Emisión respecto al periodo
            // En compras se permite registrar comprobantes de periodos anteriores. Solo es error si la fecha es posterior al periodo.
            if (fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' del proveedor '{nroDoc}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} posterior al periodo seleccionado '{periodo}'",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: SeveridadError.Error));
            }

            // 3. Validación de Documento de Identidad del Proveedor (DNI, RUC y Genéricos/Extranjeros)
            var tipoDoc = (compra.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: 8 dígitos numéricos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene documento '{nroDoc}' que debe contener exactamente 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: 11 dígitos numéricos que comiencen con 10, 20, 15 o 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento RUC inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene RUC '{nroDoc}' que debe contener exactamente 11 dígitos numéricos.",
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
                                $"Documento RUC con prefijo inválido: El RUC '{nroDoc}' del proveedor del comprobante Serie '{serie}', Número '{numero}' debe iniciar con 10, 20, 15 o 17.",
                                campoError: "nro_doc_identidad",
                                valorLectura: nroDoc,
                                severidad: SeveridadError.Advertencia));
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(nroDoc) && nroDoc != "-")
                {
                    if (nroDoc.Length > 15 || !nroDoc.All(char.IsLetterOrDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento inválido: El documento '{nroDoc}' del proveedor del comprobante Serie '{serie}', Número '{numero}' contiene caracteres no permitidos o excede 15 caracteres.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
            }

            // 4. Validación de Razón Social obligatoria
            if (string.IsNullOrWhiteSpace(compra.RazonSocial))
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"La Razón Social del proveedor en el comprobante Serie '{serie}', Número '{numero}' es obligatoria.",
                    campoError: "razon_social",
                    valorLectura: string.Empty,
                    severidad: SeveridadError.Advertencia));
            }
        }

        // 5. Registro de Errores por Duplicados Internos
        foreach (var kvp in comprobantesPorClave.Where(kvp => kvp.Value.Count > 1))
        {
            var partes = kvp.Key.Split('|');
            var rucProv = partes[0];
            var tipoCp = partes[1];
            var s = partes[2];
            var n = partes[3];
            var cant = kvp.Value.Count;
            var primeraLinea = kvp.Value.First();

            errores.Add(ArchivoCargaError.Crear(
                idCarga,
                primeraLinea,
                TipoErrorCarga.Duplicado,
                $"Comprobante duplicado: El comprobante del proveedor '{rucProv}', Tipo '{tipoCp}', Serie '{s}', Número '{n}' se encuentra registrado {cant} veces en el archivo.",
                campoError: "serie_numero",
                valorLectura: $"{s}-{n}",
                severidad: SeveridadError.Error));
        }

        return await Task.FromResult(errores);
    }
}
