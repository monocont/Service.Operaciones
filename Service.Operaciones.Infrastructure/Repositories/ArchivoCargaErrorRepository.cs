using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class ArchivoCargaErrorRepository : IArchivoCargaErrorRepository
{
    private readonly Database.OperacionesDbContext _context;

    public ArchivoCargaErrorRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArchivoCargaError>> ObtenerPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.ArchivoCargaError
            .AsNoTracking()
            .Where(e => e.IdCarga == idCarga)
            .OrderBy(e => e.NumeroLinea)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> ContarPorCargasAsync(List<Guid> idsCarga, CancellationToken cancellationToken)
    {
        if (idsCarga == null || idsCarga.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _context.ArchivoCargaError
            .AsNoTracking()
            .Where(e => idsCarga.Contains(e.IdCarga))
            .GroupBy(e => e.IdCarga)
            .Select(g => new { IdCarga = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.IdCarga, x => x.Total, cancellationToken);
    }

    public async Task AgregarRangoAsync(List<ArchivoCargaError> errores, CancellationToken cancellationToken)
    {
        await _context.ArchivoCargaError.AddRangeAsync(errores, cancellationToken);
    }
}
