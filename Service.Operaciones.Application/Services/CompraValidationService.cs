using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Services;

/// <summary>
/// Validaciones de negocio para comprobantes de compra (equivalente a VentaValidationService).
/// Genera observaciones (ArchivoCargaError) sin abortar el proceso.
/// </summary>
public class CompraValidationService : ICompraValidationService
{
    public Task<List<ArchivoCargaError>> ValidarComprasAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<Compra> compras,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        var linea = 1;

        foreach (var compra in compras)
        {
            if (string.IsNullOrWhiteSpace(compra.RazonSocial))
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga, linea, Domain.Enums.TipoErrorCarga.Validacion,
                    "La Razón Social del proveedor es obligatoria.",
                    "razon_social", compra.RazonSocial));
            }

            if (!string.IsNullOrWhiteSpace(compra.NroDocIdentidad))
            {
                var doc = compra.NroDocIdentidad.Trim();
                var tipo = compra.CodigoTipoDocIdentidad?.Trim();

                if (tipo == "6" && !System.Text.RegularExpressions.Regex.IsMatch(doc, @"^\d{11}$"))
                {
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga, linea, Domain.Enums.TipoErrorCarga.Validacion,
                        $"El RUC del proveedor '{doc}' debe tener 11 dígitos.",
                        "nro_doc_identidad", doc));
                }
                else if (tipo == "1" && !System.Text.RegularExpressions.Regex.IsMatch(doc, @"^\d{8}$"))
                {
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga, linea, Domain.Enums.TipoErrorCarga.Validacion,
                        $"El DNI del proveedor '{doc}' debe tener 8 dígitos.",
                        "nro_doc_identidad", doc));
                }
            }

            if (compra.TotalCp < 0)
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga, linea, Domain.Enums.TipoErrorCarga.Validacion,
                    "El Total del comprobante no puede ser negativo.",
                    "total_comprobante", compra.TotalCp.ToString()));
            }

            linea++;
        }

        return Task.FromResult(errores);
    }
}
