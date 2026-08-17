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

    public Task<ArchivoCarga?> ObtenerDuplicadoAsync(
        string empresaRuc, string periodo, TipoArchivo tipoArchivo, string hashDocumento, CancellationToken cancellationToken)
    {
        return _context.ArchivoCarga.FirstOrDefaultAsync(
            a => a.EmpresaRuc == empresaRuc
              && a.Periodo == periodo
              && a.TipoArchivo == tipoArchivo
              && a.HashDocumento == hashDocumento,
            cancellationToken);
    }

    public Task<bool> ExisteCargaAsync(
        string empresaRuc, string periodo, TipoArchivo tipoArchivo, string creadoPor, CancellationToken cancellationToken)
    {
        return _context.ArchivoCarga.AnyAsync(
            a => a.EmpresaRuc == empresaRuc
              && a.Periodo == periodo
              && a.TipoArchivo == tipoArchivo
              && a.CreadoPor == creadoPor
              && a.Activo,
            cancellationToken);
    }

    public async Task<List<ArchivoCarga>> ListarAsync(
        string empresaRuc, TipoArchivo tipoArchivo, string? periodo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.ArchivoCarga
            .AsNoTracking()
            .Where(a => a.EmpresaRuc == empresaRuc && a.TipoArchivo == tipoArchivo && a.Activo);

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            query = query.Where(a => a.Periodo == periodo.Trim());
        }

        return await query
            .OrderByDescending(a => a.Periodo)
            .ThenByDescending(a => a.FechaCreacion)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarAsync(
        string empresaRuc, TipoArchivo tipoArchivo, CancellationToken cancellationToken)
    {
        return await _context.ArchivoCarga
            .AsNoTracking()
            .Where(a => a.EmpresaRuc == empresaRuc && a.TipoArchivo == tipoArchivo && a.Activo)
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
        // Si ya está en estado Added o Modified en el contexto, sus propiedades modificadas ya se rastrean sin forzar Update
        return Task.CompletedTask;
    }
}
