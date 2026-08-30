using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class VentaEmpresaRepository : IVentaEmpresaRepository
{
    private readonly Database.OperacionesDbContext _context;

    public VentaEmpresaRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<VentaEmpresa>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.VentaEmpresa
            .AsNoTracking()
            .Where(v => v.IdCarga == idCarga)
            .OrderBy(v => v.FechaEmision)
            .ThenBy(v => v.Serie)
            .ThenBy(v => v.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.VentaEmpresa.AsNoTracking().CountAsync(v => v.IdCarga == idCarga, cancellationToken);
    }

    public async Task<List<string>> ObtenerNumerosPorSerieYPeriodoAsync(
        string empresaRuc,
        string periodo,
        string codigoTipoCp,
        string serie,
        CancellationToken cancellationToken)
    {
        return await _context.VentaEmpresa.AsNoTracking()
            .Where(v => v.EmpresaRuc == empresaRuc
                     && v.Periodo == periodo
                     && v.CodigoTipoCp == codigoTipoCp
                     && v.Serie == serie)
            .Select(v => v.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarRangoAsync(List<VentaEmpresa> ventas, CancellationToken cancellationToken)
    {
        // En cumplimiento del patrón Unit of Work: Solo agrega a ChangeTracker en memoria
        await _context.VentaEmpresa.AddRangeAsync(ventas, cancellationToken);
    }

    public async Task EliminarRangoFisicoAsync(List<Guid> idsVentaEmpresa, CancellationToken cancellationToken)
    {
        if (idsVentaEmpresa == null || idsVentaEmpresa.Count == 0) return;

        var ventasAEliminar = await _context.VentaEmpresa
            .Where(v => idsVentaEmpresa.Contains(v.IdVentaEmpresa))
            .ToListAsync(cancellationToken);

        if (ventasAEliminar.Count > 0)
        {
            _context.VentaEmpresa.RemoveRange(ventasAEliminar);
        }
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.VentaEmpresa
            .Where(v => v.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<VentaEmpresa>> ObtenerPorIdsAsync(List<Guid> idsVentaEmpresa, CancellationToken cancellationToken)
    {
        if (idsVentaEmpresa == null || idsVentaEmpresa.Count == 0) return new List<VentaEmpresa>();

        return await _context.VentaEmpresa
            .Where(v => idsVentaEmpresa.Contains(v.IdVentaEmpresa))
            .ToListAsync(cancellationToken);
    }
}
