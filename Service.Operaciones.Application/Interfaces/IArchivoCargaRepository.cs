using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Interfaces;

public interface IArchivoCargaRepository
{
    Task<ArchivoCarga?> ObtenerPorIdAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<ArchivoCarga?> ObtenerDuplicadoAsync(string empresaRuc, string periodo, TipoOperacion tipoOperacion, string hashDocumento, CancellationToken cancellationToken);
    Task<bool> ExisteCargaAsync(string empresaRuc, string periodo, TipoOperacion tipoOperacion, string creadoPor, CancellationToken cancellationToken);
    Task<List<ArchivoCarga>> ListarAsync(string empresaRuc, TipoOperacion tipoOperacion, string? periodo, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<int> ContarAsync(string empresaRuc, TipoOperacion tipoOperacion, CancellationToken cancellationToken);
    Task AgregarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken);
    Task ActualizarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken);
    Task EliminarFisicoAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<List<ArchivoCarga>> ObtenerCargasPorRucsYAnioAsync(List<string> rucs, int anio, CancellationToken cancellationToken);
}
