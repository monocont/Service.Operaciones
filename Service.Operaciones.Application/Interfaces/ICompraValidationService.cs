using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface ICompraValidationService
{
    Task<List<ArchivoCargaError>> ValidarComprasAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<Compra> compras,
        CancellationToken cancellationToken);
}
