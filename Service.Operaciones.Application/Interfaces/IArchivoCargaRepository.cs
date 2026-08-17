using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Interfaces;

public interface IArchivoCargaRepository
{
    Task<ArchivoCarga?> ObtenerPorIdAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<ArchivoCarga?> ObtenerDuplicadoAsync(string empresaRuc, string periodo, TipoArchivo tipoArchivo, string hashDocumento, CancellationToken cancellationToken);
    Task<bool> ExisteCargaAsync(string empresaRuc, string periodo, TipoArchivo tipoArchivo, string creadoPor, CancellationToken cancellationToken);
    Task<List<ArchivoCarga>> ListarAsync(string empresaRuc, TipoArchivo tipoArchivo, string? periodo, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<int> ContarAsync(string empresaRuc, TipoArchivo tipoArchivo, CancellationToken cancellationToken);
    Task AgregarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken);
    Task ActualizarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken);
}
