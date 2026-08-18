using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentas;

public class ActualizarVentasCommandHandler : IRequestHandler<ActualizarVentasCommand, ActualizarVentasResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaRepository _ventaRepo;
    private readonly IVentaValidationService _ventaValidationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarVentasCommandHandler> _logger;

    public ActualizarVentasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaRepository ventaRepo,
        IVentaValidationService ventaValidationService,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarVentasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaRepo = ventaRepo;
        _ventaValidationService = ventaValidationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarVentasResponseDTO> Handle(ActualizarVentasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización de ventas para carga {IdCarga}. Eliminados: {Count}",
            request.IdCarga, request.EliminadosIds?.Count ?? 0);

        // 1. Obtener la carga correspondiente
        var carga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (carga is null)
        {
            throw new NotFoundException("Carga", request.IdCarga);
        }

        // 2. Iniciar transacción en UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 3. Eliminar comprobantes físicamente de la base de datos
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _ventaRepo.EliminarRangoFisicoAsync(request.EliminadosIds, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 4. Obtener los comprobantes restantes de la carga
            var ventasRestantes = await _ventaRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

            // 5. Re-validar los comprobantes restantes con el servicio reutilizable
            var nuevosErrores = await _ventaValidationService.ValidarVentasAsync(
                request.IdCarga,
                carga.EmpresaRuc,
                carga.Periodo,
                ventasRestantes,
                cancellationToken);

            // 6. Eliminar observaciones anteriores de la carga y registrar las nuevas
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(request.IdCarga, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 7. Actualizar conteos de la carga en operaciones.archivo_carga
            var totalRestantes = ventasRestantes.Count;
            var totalObservaciones = nuevosErrores.Count;
            var totalValidos = Math.Max(0, totalRestantes - totalObservaciones);

            carga.ActualizarConteo(totalRestantes, totalValidos, totalObservaciones);
            if (!string.IsNullOrWhiteSpace(request.Usuario))
            {
                carga.ModificadoPor = request.Usuario;
                carga.FechaModificacion = DateTime.UtcNow;
            }
            await _archivoCargaRepo.ActualizarAsync(carga, cancellationToken);

            // 8. Commit transacción en UnitOfWork
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Actualización de ventas para carga {IdCarga} completada exitosamente. Restantes: {Count}, Nuevas Observaciones: {Obs}",
                request.IdCarga, ventasRestantes.Count, nuevosErrores.Count);

            return new ActualizarVentasResponseDTO
            {
                Exito = true,
                Mensaje = "Registros actualizados y comprobantes revalidados correctamente.",
                NumRegistros = ventasRestantes.Count,
                NumObservaciones = nuevosErrores.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar ventas de carga {IdCarga}. Ejecutando Rollback.", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
