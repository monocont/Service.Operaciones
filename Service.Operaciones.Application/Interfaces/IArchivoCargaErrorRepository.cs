using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IArchivoCargaErrorRepository
{
    Task<List<ArchivoCargaError>> ObtenerPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<ArchivoCargaError> errores, CancellationToken cancellationToken);
}
