using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Commands.Carga.EliminarArchivoCarga;

public class EliminarArchivoCargaCommandHandler : IRequestHandler<EliminarArchivoCargaCommand, bool>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaRepository _ventaRepo;
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;
    private readonly ICompraSireRepository _compraSireRepo;
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;
    private readonly IVentaMatchRepository _ventaMatchRepo;
    private readonly ICompraMatchRepository _compraMatchRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EliminarArchivoCargaCommandHandler> _logger;

    public EliminarArchivoCargaCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IAccesoEmpresaValidator accesoValidator,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaRepository ventaRepo,
        IVentaEmpresaRepository ventaEmpresaRepo,
        ICompraSireRepository compraSireRepo,
        ICompraEmpresaRepository compraEmpresaRepo,
        IVentaMatchRepository ventaMatchRepo,
        ICompraMatchRepository compraMatchRepo,
        IUnitOfWork unitOfWork,
        ILogger<EliminarArchivoCargaCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _accesoValidator = accesoValidator;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaRepo = ventaRepo;
        _ventaEmpresaRepo = ventaEmpresaRepo;
        _compraSireRepo = compraSireRepo;
        _compraEmpresaRepo = compraEmpresaRepo;
        _ventaMatchRepo = ventaMatchRepo;
        _compraMatchRepo = compraMatchRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(EliminarArchivoCargaCommand request, CancellationToken cancellationToken)
    {
        var carga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (carga == null)
        {
            throw new NotFoundException($"No se encontró la carga con Id '{request.IdCarga}'.");
        }

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        // 1. Iniciar transacción en UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. Eliminar primero los errores y observaciones asociados (Hijos)
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(request.IdCarga, cancellationToken);

            // 3. Eliminar comprobantes asociados (Ventas SIRE, Ventas Empresa, Compras SIRE, Compras Empresa, Ventas Match, Compras Match)
            await _ventaRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);
            await _ventaEmpresaRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);
            await _compraSireRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);
            await _compraEmpresaRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);
            await _ventaMatchRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);
            await _compraMatchRepo.EliminarPorCargaFisicoAsync(request.IdCarga, cancellationToken);

            // 4. Eliminar el registro raíz de archivo_carga (Padre)
            await _archivoCargaRepo.EliminarFisicoAsync(request.IdCarga, cancellationToken);

            // 5. Confirmar transacción atómicamente
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Carga con Id {IdCarga} y todos sus registros asociados han sido eliminados físicamente con éxito.", request.IdCarga);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar la carga con Id {IdCarga}. Ejecutando Rollback.", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
