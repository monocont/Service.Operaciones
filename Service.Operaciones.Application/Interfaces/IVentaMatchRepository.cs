using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaMatchRepository
{
    Task<List<VentaMatch>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<VentaMatch> ventasMatch, CancellationToken cancellationToken);
    Task ActualizarRangoAsync(List<VentaMatch> ventasMatch, CancellationToken cancellationToken);
    Task EliminarRangoPorIdsAsync(List<Guid> ids, CancellationToken cancellationToken);
    Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken);
}
