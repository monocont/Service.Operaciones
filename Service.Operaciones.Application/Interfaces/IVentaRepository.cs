using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaRepository
{
    Task<List<Venta>> ListarPorCargaAsync(Guid idCarga, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<bool> ExistePorCarSunatAsync(string empresaRuc, string periodo, string carSunat, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<Venta> ventas, CancellationToken cancellationToken);
}
