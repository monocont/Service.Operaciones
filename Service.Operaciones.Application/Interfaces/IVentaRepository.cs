using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaRepository
{
    Task<List<Venta>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<bool> ExistePorCarSunatAsync(string empresaRuc, string periodo, string carSunat, CancellationToken cancellationToken);
    Task<List<string>> ObtenerNumerosPorSerieYPeriodoAsync(string empresaRuc, string periodo, string codigoTipoCp, string serie, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<Venta> ventas, CancellationToken cancellationToken);
    Task EliminarRangoFisicoAsync(List<Guid> idsVenta, CancellationToken cancellationToken);
    Task<List<Venta>> ObtenerPorIdsAsync(List<Guid> idsVenta, CancellationToken cancellationToken);
}
