using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Services;

public class CompraSireValidationService : ICompraSireValidationService
{
    private readonly ICompraSireRepository _compraSireRepo;

    public CompraSireValidationService(ICompraSireRepository compraSireRepo)
    {
        _compraSireRepo = compraSireRepo;
    }

    public async Task<List<ArchivoCargaError>> ValidarComprasSireAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<CompraSire> compras,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (compras == null || compras.Count == 0)
        {
            return errores;
        }

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var primerDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, 1);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        var comprobantesPorClave = new Dictionary<string, List<int>>();
        var carSunatsVistos = new Dictionary<string, List<int>>();

        for (var i = 0; i < compras.Count; i++)
        {
            var compra = compras[i];
            var numeroLinea = compra.NumeroLinea > 0 ? compra.NumeroLinea : i + 1;

            var serie = (compra.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (compra.Numero ?? string.Empty).Trim();
            var tipoCp = (compra.CodigoTipoCp ?? string.Empty).Trim();
            var nroDoc = (compra.NroDocIdentidad ?? string.Empty).Trim();
            var periodoCompra = (compra.Periodo ?? string.Empty).Trim();
            var fechaEmision = compra.FechaEmision;
            var carSunat = (compra.CarSunat ?? string.Empty).Trim();

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

            // 1.1 Detección de CAR SUNAT Duplicado interno
            if (!string.IsNullOrWhiteSpace(carSunat))
            {
                if (!carSunatsVistos.ContainsKey(carSunat))
                {
                    carSunatsVistos[carSunat] = new List<int>();
                }
                carSunatsVistos[carSunat].Add(numeroLinea);
            }

            // 2. Consistencia de Periodo
            if (!string.IsNullOrWhiteSpace(periodoCompra) && periodoCompra != periodo)
            {
                var esPeriodoSuperior = string.Compare(periodoCompra, periodo, StringComparison.Ordinal) > 0;
                var sevPeriodo = esPeriodoSuperior ? SeveridadError.Error : SeveridadError.Advertencia;

                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"Periodo inconsistente: El comprobante Serie '{serie}', Número '{numero}' corresponde al periodo '{periodoCompra}', difiere del periodo cargado '{periodo}'",
                    campoError: "periodo",
                    valorLectura: periodoCompra,
                    severidad: sevPeriodo));
            }

            // 3. Consistencia de RUC de Empresa declarante
            var rucEmpresaFila = (compra.EmpresaRuc ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(rucEmpresaFila) && rucEmpresaFila != empresaRuc)
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numeroLinea,
                    TipoErrorCarga.Negocio,
                    $"RUC inconsistente: El comprobante Serie '{serie}', Número '{numero}' pertenece al RUC '{rucEmpresaFila}', difiere de la empresa seleccionada '{empresaRuc}'",
                    campoError: "ruc",
                    valorLectura: rucEmpresaFila,
                    severidad: SeveridadError.Advertencia));
            }

            // 4. Validación de Fecha de Emisión
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

            // 5. Validación de Documento de Identidad del Proveedor (DNI y RUC)
            var tipoDoc = (compra.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: 8 dígitos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene documento '{nroDoc}' que debe contener 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: 11 dígitos y prefijos 10, 20, 15, 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numeroLinea,
                            TipoErrorCarga.Formato,
                            $"Documento RUC inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene RUC '{nroDoc}' que debe contener 11 dígitos numéricos.",
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
            }

            // 6. Validación de Razón Social obligatoria
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

        // Registro de Errores por Duplicados
        foreach (var kvp in comprobantesPorClave.Where(kvp => kvp.Value.Count > 1))
        {
            var partes = kvp.Key.Split('|');
            var rucProv = partes[0];
            var s = partes[2];
            var n = partes[3];
            var cant = kvp.Value.Count;
            var primeraLinea = kvp.Value.First();

            errores.Add(ArchivoCargaError.Crear(
                idCarga,
                primeraLinea,
                TipoErrorCarga.Duplicado,
                $"Comprobante duplicado: El comprobante del proveedor '{rucProv}', Serie '{s}', Número '{n}' se encuentra registrado {cant} veces en el archivo.",
                campoError: "serie_numero",
                valorLectura: $"{s}-{n}",
                severidad: SeveridadError.Error));
        }

        return errores;
    }
}

