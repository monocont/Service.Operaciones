using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IArchivoCargaErrorRepository
{
    Task<List<ArchivoCargaError>> ObtenerPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>> ContarPorCargasAsync(List<Guid> idsCarga, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<ArchivoCargaError> errores, CancellationToken cancellationToken);
    Task EliminarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
}
