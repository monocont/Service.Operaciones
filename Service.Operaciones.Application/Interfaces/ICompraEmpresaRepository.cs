using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface ICompraEmpresaRepository
{
    Task<List<CompraEmpresa>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<CompraEmpresa> compras, CancellationToken cancellationToken);
    Task EliminarRangoFisicoAsync(List<Guid> idsCompraEmpresa, CancellationToken cancellationToken);
    Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<List<CompraEmpresa>> ObtenerPorIdsAsync(List<Guid> idsCompraEmpresa, CancellationToken cancellationToken);
}
