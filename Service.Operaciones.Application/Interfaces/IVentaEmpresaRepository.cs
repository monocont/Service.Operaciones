using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaEmpresaRepository
{
    Task<List<VentaEmpresa>> ListarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<int> ContarPorCargaAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<List<string>> ObtenerNumerosPorSerieYPeriodoAsync(string empresaRuc, string periodo, string codigoTipoCp, string serie, CancellationToken cancellationToken);
    Task AgregarRangoAsync(List<VentaEmpresa> ventas, CancellationToken cancellationToken);
    Task EliminarRangoFisicoAsync(List<Guid> idsVentaEmpresa, CancellationToken cancellationToken);
    Task EliminarPorCargaFisicoAsync(Guid idCarga, CancellationToken cancellationToken);
    Task<List<VentaEmpresa>> ObtenerPorIdsAsync(List<Guid> idsVentaEmpresa, CancellationToken cancellationToken);
}
