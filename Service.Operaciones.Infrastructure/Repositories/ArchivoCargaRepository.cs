using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Infrastructure.Repositories;

public class ArchivoCargaRepository : IArchivoCargaRepository
{
    private readonly Database.OperacionesDbContext _context;

    public ArchivoCargaRepository(Database.OperacionesDbContext context)
    {
        _context = context;
    }

    public Task<ArchivoCarga?> ObtenerPorIdAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        return _context.ArchivoCarga.FirstOrDefaultAsync(a => a.IdCarga == idCarga, cancellationToken);
    }

    public async Task<ArchivoCarga?> ObtenerDuplicadoAsync(
        string empresaRuc, string periodo, TipoOperacion tipoOperacion, string hashDocumento, CancellationToken cancellationToken)
    {
        return await _context.ArchivoCarga
            .FirstOrDefaultAsync(a =>
                a.EmpresaRuc == empresaRuc
                && a.Periodo == periodo
                && a.IdTipoOperacion == tipoOperacion
                && a.HashDocumento == hashDocumento
                && a.Activo, cancellationToken);
    }

    public async Task<bool> ExisteCargaAsync(
        string empresaRuc, string periodo, TipoOperacion tipoOperacion, string creadoPor, CancellationToken cancellationToken)
    {
        return await _context.ArchivoCarga
            .AnyAsync(a =>
                a.EmpresaRuc == empresaRuc
                && a.Periodo == periodo
                && a.IdTipoOperacion == tipoOperacion
                && a.CreadoPor == creadoPor
                && a.Activo, cancellationToken);
    }

    public async Task<List<ArchivoCarga>> ListarAsync(
        string empresaRuc, TipoOperacion tipoOperacion, string? periodo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.ArchivoCarga
            .AsNoTracking()
            .Where(a => a.EmpresaRuc == empresaRuc && a.IdTipoOperacion == tipoOperacion && a.Activo);

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            query = query.Where(a => a.Periodo == periodo.Trim());
        }

        return await query
            .OrderByDescending(a => a.FechaCreacion)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarAsync(
        string empresaRuc, TipoOperacion tipoOperacion, CancellationToken cancellationToken)
    {
        return await _context.ArchivoCarga
            .Where(a => a.EmpresaRuc == empresaRuc && a.IdTipoOperacion == tipoOperacion && a.Activo)
            .CountAsync(cancellationToken);
    }

    public async Task AgregarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken)
    {
        await _context.ArchivoCarga.AddAsync(archivoCarga, cancellationToken);
    }

    public Task ActualizarAsync(ArchivoCarga archivoCarga, CancellationToken cancellationToken)
    {
        var entry = _context.Entry(archivoCarga);
        if (entry.State == EntityState.Detached)
        {
            _context.ArchivoCarga.Update(archivoCarga);
        }
        return Task.CompletedTask;
    }

    public async Task EliminarFisicoAsync(Guid idCarga, CancellationToken cancellationToken)
    {
        await _context.ArchivoCarga
            .Where(a => a.IdCarga == idCarga)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
