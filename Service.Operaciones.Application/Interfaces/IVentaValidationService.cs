using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaValidationService
{
    Task<List<ArchivoCargaError>> ValidarVentasAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<Venta> ventas,
        CancellationToken cancellationToken);
}
