using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Interfaces;

public interface IVentaEmpresaValidationService
{
    Task<List<ArchivoCargaError>> ValidarVentasEmpresaAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<VentaEmpresa> ventasEmpresa,
        CancellationToken cancellationToken);
}
