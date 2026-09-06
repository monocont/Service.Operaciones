using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface ICompraEmpresaValidationService
{
    Task<List<ArchivoCargaError>> ValidarComprasEmpresaAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<CompraEmpresa> compras,
        CancellationToken cancellationToken);
}
