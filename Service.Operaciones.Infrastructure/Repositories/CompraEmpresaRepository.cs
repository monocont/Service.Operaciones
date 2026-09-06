using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class CompraEmpresaRepository : ICompraEmpresaRepository
{
    private readonly Database.OperacionesDbContext _context;

    public CompraEmpresaRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompraEmpresa>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraEmpresa
            .AsNoTracking()
            .Where(c => c.IdCarga == idCarga)
            .OrderBy(c => c.FechaEmision)
            .ThenBy(c => c.Serie)
            .ThenBy(c => c.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraEmpresa.AsNoTracking().CountAsync(c => c.IdCarga == idCarga, cancellationToken);
    }

    public async Task AgregarRangoAsync(List<CompraEmpresa> compras, CancellationToken cancellationToken)
    {
        await _context.CompraEmpresa.AddRangeAsync(compras, cancellationToken);
    }

    public async Task EliminarRangoFisicoAsync(List<Guid> idsCompraEmpresa, CancellationToken cancellationToken)
    {
        if (idsCompraEmpresa == null || idsCompraEmpresa.Count == 0) return;

        var comprasAEliminar = await _context.CompraEmpresa
            .Where(c => idsCompraEmpresa.Contains(c.IdCompraEmpresa))
            .ToListAsync(cancellationToken);

        if (comprasAEliminar.Count > 0)
        {
            _context.CompraEmpresa.RemoveRange(comprasAEliminar);
        }
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.CompraEmpresa
            .Where(c => c.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<CompraEmpresa>> ObtenerPorIdsAsync(List<Guid> idsCompraEmpresa, CancellationToken cancellationToken)
    {
        if (idsCompraEmpresa == null || idsCompraEmpresa.Count == 0) return new List<CompraEmpresa>();

        return await _context.CompraEmpresa
            .Where(c => idsCompraEmpresa.Contains(c.IdCompraEmpresa))
            .ToListAsync(cancellationToken);
    }
}
