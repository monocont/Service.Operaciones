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

    public async Task<List<Venta>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.Venta
            .AsNoTracking()
            .Where(v => v.IdCarga == idCarga)
            .OrderBy(v => v.FechaEmision)
            .ThenBy(v => v.Serie)
            .ThenBy(v => v.Numero)
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

    public async Task<List<string>> ObtenerNumerosPorSerieYPeriodoAsync(string empresaRuc, string periodo, string codigoTipoCp, string serie, CancellationToken cancellationToken)
    {
        return await _context.Venta.AsNoTracking()
            .Where(v => v.EmpresaRuc == empresaRuc
                     && v.Periodo == periodo
                     && v.CodigoTipoCp == codigoTipoCp
                     && v.Serie == serie)
            .Select(v => v.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarRangoAsync(List<Venta> ventas, CancellationToken cancellationToken)
    {
        await _context.Venta.AddRangeAsync(ventas, cancellationToken);
    }

    public async Task EliminarRangoFisicoAsync(List<Guid> idsVenta, CancellationToken cancellationToken)
    {
        if (idsVenta == null || idsVenta.Count == 0) return;

        var ventasAEliminar = await _context.Venta
            .Where(v => idsVenta.Contains(v.IdVenta))
            .ToListAsync(cancellationToken);

        if (ventasAEliminar.Count > 0)
        {
            _context.Venta.RemoveRange(ventasAEliminar);
        }
    }
}
