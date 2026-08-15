using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly Database.OperacionesDbContext _context;

    public VentaRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Venta>> ListarPorCargaAsync(Guid idCarga, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await _context.Venta
            .AsNoTracking()
            .Where(v => v.IdCarga == idCarga)
            .OrderBy(v => v.FechaEmision)
            .ThenBy(v => v.Serie)
            .ThenBy(v => v.Numero)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.Venta.AsNoTracking().CountAsync(v => v.IdCarga == idCarga, cancellationToken);
    }

    public Task<bool> ExistePorCarSunatAsync(string empresaRuc, string periodo, string carSunat, CancellationToken cancellationToken)
    {
        return _context.Venta.AsNoTracking()
            .AnyAsync(v => v.EmpresaRuc == empresaRuc
                        && v.Periodo == periodo
                        && v.CarSunat == carSunat,
                      cancellationToken);
    }

    public async Task AgregarRangoAsync(List<Venta> ventas, CancellationToken cancellationToken)
    {
        await _context.Venta.AddRangeAsync(ventas, cancellationToken);
    }
}
