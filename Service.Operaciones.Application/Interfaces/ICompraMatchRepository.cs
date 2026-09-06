using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface ICompraMatchRepository
{
    Task<List<CompraMatch>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<CompraMatch> comprasMatch, CancellationToken cancellationToken);
    Task ActualizarRangoAsync(List<CompraMatch> comprasMatch, CancellationToken cancellationToken);
    Task EliminarRangoPorIdsAsync(List<Guid> ids, CancellationToken cancellationToken);
    Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken);
}
