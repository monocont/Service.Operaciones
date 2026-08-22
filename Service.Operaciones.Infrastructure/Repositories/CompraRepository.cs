using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly Database.OperacionesDbContext _context;

    public CompraRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Compra>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.Compra
            .AsNoTracking()
            .Where(c => c.IdCarga == idCarga)
            .OrderBy(c => c.FechaEmision)
            .ThenBy(c => c.Serie)
            .ThenBy(c => c.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.Compra.AsNoTracking().CountAsync(c => c.IdCarga == idCarga, cancellationToken);
    }

    public Task<bool> ExistePorCarSunatAsync(string empresaRuc, string periodo, string carSunat, CancellationToken cancellationToken)
    {
        return _context.Compra.AsNoTracking()
            .AnyAsync(c => c.EmpresaRuc == empresaRuc
                        && c.Periodo == periodo
                        && c.CarSunat == carSunat,
                      cancellationToken);
    }

    public async Task AgregarRangoAsync(List<Compra> compras, CancellationToken cancellationToken)
    {
        await _context.Compra.AddRangeAsync(compras, cancellationToken);
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.Compra
            .Where(c => c.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<Compra>> ObtenerPorIdsAsync(List<Guid> idsCompra, CancellationToken cancellationToken)
    {
        if (idsCompra == null || idsCompra.Count == 0) return new List<Compra>();

        return await _context.Compra
            .Where(c => idsCompra.Contains(c.IdCompra))
            .ToListAsync(cancellationToken);
    }

    public async Task EliminarRangoFisicoAsync(List<Guid> idsCompra, CancellationToken cancellationToken)
    {
        if (idsCompra == null || idsCompra.Count == 0) return;

        await _context.Compra
            .Where(c => idsCompra.Contains(c.IdCompra))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
