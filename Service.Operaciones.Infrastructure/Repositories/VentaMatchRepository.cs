using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class VentaMatchRepository : IVentaMatchRepository
{
    private readonly Database.OperacionesDbContext _context;

    public VentaMatchRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<VentaMatch>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.VentaMatch
            .AsNoTracking()
            .Where(v => v.IdCarga == idCarga)
            .OrderBy(v => v.NumeroLinea)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.VentaMatch
            .AsNoTracking()
            .CountAsync(v => v.IdCarga == idCarga, cancellationToken);
    }

    public async Task AgregarRangoAsync(List<VentaMatch> ventasMatch, CancellationToken cancellationToken)
    {
        await _context.VentaMatch.AddRangeAsync(ventasMatch, cancellationToken);
    }

    public Task ActualizarRangoAsync(List<VentaMatch> ventasMatch, CancellationToken cancellationToken)
    {
        _context.VentaMatch.UpdateRange(ventasMatch);
        return Task.CompletedTask;
    }

    public async Task EliminarRangoPorIdsAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return;
        await _context.VentaMatch
            .Where(v => ids.Contains(v.IdVentaMatch))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.VentaMatch
            .Where(v => v.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
