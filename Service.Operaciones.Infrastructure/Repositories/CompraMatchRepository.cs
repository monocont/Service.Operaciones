using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class CompraMatchRepository : ICompraMatchRepository
{
    private readonly Database.OperacionesDbContext _context;

    public CompraMatchRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompraMatch>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraMatch
            .AsNoTracking()
            .Where(c => c.IdCarga == idCarga)
            .OrderBy(c => c.NumeroLinea)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraMatch
            .AsNoTracking()
            .CountAsync(c => c.IdCarga == idCarga, cancellationToken);
    }

    public async Task AgregarRangoAsync(List<CompraMatch> comprasMatch, CancellationToken cancellationToken)
    {
        await _context.CompraMatch.AddRangeAsync(comprasMatch, cancellationToken);
    }

    public Task ActualizarRangoAsync(List<CompraMatch> comprasMatch, CancellationToken cancellationToken)
    {
        foreach (var entity in comprasMatch)
        {
            var tracked = _context.CompraMatch.Local.FirstOrDefault(e => e.IdCompraMatch == entity.IdCompraMatch);
            if (tracked != null)
            {
                if (!ReferenceEquals(tracked, entity))
                {
                    _context.Entry(tracked).CurrentValues.SetValues(entity);
                }
            }
            else
            {
                _context.CompraMatch.Update(entity);
            }
        }
        return Task.CompletedTask;
    }

    public async Task EliminarRangoPorIdsAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return;
        await _context.CompraMatch
            .Where(c => ids.Contains(c.IdCompraMatch))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.CompraMatch
            .Where(c => c.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
