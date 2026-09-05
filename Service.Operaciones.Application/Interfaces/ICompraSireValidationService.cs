using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface ICompraSireValidationService
{
    Task<List<ArchivoCargaError>> ValidarComprasSireAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<CompraSire> compras,
        CancellationToken cancellationToken);
}
