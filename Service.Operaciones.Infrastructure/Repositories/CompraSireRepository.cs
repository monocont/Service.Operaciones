using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Repositories;

public class CompraSireRepository : ICompraSireRepository
{
    private readonly Database.OperacionesDbContext _context;

    public CompraSireRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompraSire>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraSire
            .AsNoTracking()
            .Where(c => c.IdCarga == idCarga)
            .OrderBy(c => c.NumeroLinea)
            .ThenBy(c => c.FechaEmision)
            .ThenBy(c => c.Serie)
            .ThenBy(c => c.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return await _context.CompraSire.AsNoTracking().CountAsync(c => c.IdCarga == idCarga, cancellationToken);
    }

    public Task<bool> ExistePorCarSunatAsync(string empresaRuc, string periodo, string carSunat, CancellationToken cancellationToken)
    {
        return _context.CompraSire.AsNoTracking()
            .AnyAsync(c => c.EmpresaRuc == empresaRuc
                        && c.Periodo == periodo
                        && c.CarSunat == carSunat,
                      cancellationToken);
    }

    public async Task AgregarRangoAsync(List<CompraSire> compras, CancellationToken cancellationToken)
    {
        await _context.CompraSire.AddRangeAsync(compras, cancellationToken);
    }

    public async Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.CompraSire
            .Where(c => c.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<CompraSire>> ObtenerPorIdsAsync(List<Guid> idsCompra, CancellationToken cancellationToken)
    {
        if (idsCompra == null || idsCompra.Count == 0) return new List<CompraSire>();

        return await _context.CompraSire
            .Where(c => idsCompra.Contains(c.IdCompra))
            .ToListAsync(cancellationToken);
    }

    public async Task EliminarRangoFisicoAsync(List<Guid> idsCompra, CancellationToken cancellationToken)
    {
        if (idsCompra == null || idsCompra.Count == 0) return;

        await _context.CompraSire
            .Where(c => idsCompra.Contains(c.IdCompra))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<string>> ObtenerNumerosPorSerieYPeriodoAsync(
        string empresaRuc,
        string periodo,
        string tipoCp,
        string serie,
        CancellationToken cancellationToken)
    {
        return await _context.CompraSire
            .AsNoTracking()
            .Where(c => c.EmpresaRuc == empresaRuc
                     && c.Periodo == periodo
                     && c.CodigoTipoCp == tipoCp
                     && c.Serie == serie)
            .Select(c => c.Numero)
            .ToListAsync(cancellationToken);
    }
}
