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

            // 3.1 Agregar nuevos comprobantes validados
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var ventasNuevas = new List<Service.Operaciones.Domain.Entities.Venta>();
                foreach (var n in request.Nuevos)
                {
                    // Validar campos obligatorios: fecha, tipo_cp, serie, numero, tipo_doc_identidad, nro_doc_identidad, razon_social, bi, igv, total
                    if (!n.FechaEmision.HasValue)
                        throw new ValidationException("La fecha de emisión es obligatoria para los nuevos comprobantes.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante de Pago es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.Serie))
                        throw new ValidationException("La Serie del comprobante es obligatoria.");
                    if (string.IsNullOrWhiteSpace(n.Numero))
                        throw new ValidationException("El Número del comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoDocIdentidad))
                        throw new ValidationException("El Tipo de Documento de Identidad del cliente es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.NroDocIdentidad))
                        throw new ValidationException("El Número de Documento de Identidad del cliente es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.RazonSocial))
                        throw new ValidationException("La Razón Social o Nombre del cliente es obligatoria.");

                    var moneda = string.IsNullOrWhiteSpace(n.CodigoMoneda) ? "PEN" : n.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (n.TipoCambio.HasValue && n.TipoCambio.Value > 0) ? n.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(n.CodigoEstadoComprobante) ? "1" : n.CodigoEstadoComprobante.Trim();

                    // Generar CAR SUNAT si no viene provisto (RUC + Tipo + Serie + Numero)
                    var carSunat = !string.IsNullOrWhiteSpace(n.CarSunat)
                        ? n.CarSunat.Trim()
                        : $"{carga.EmpresaRuc}{n.CodigoTipoCp.Trim()}{n.Serie.Trim().PadLeft(4, '0')}{n.Numero.Trim().PadLeft(8, '0')}";

                    var nuevaVenta = Service.Operaciones.Domain.Entities.Venta.Crear(
                        empresaRuc: carga.EmpresaRuc,
                        periodo: carga.Periodo,
                        idCarga: carga.IdCarga,
                        carSunat: carSunat,
                        codigoTipoCp: n.CodigoTipoCp.Trim(),
                        serie: n.Serie.Trim().ToUpper(),
                        numero: n.Numero.Trim(),
                        fechaEmision: n.FechaEmision.Value,
                        codigoTipoDocIdentidad: n.CodigoTipoDocIdentidad.Trim(),
                        nroDocIdentidad: n.NroDocIdentidad.Trim(),
                        razonSocial: n.RazonSocial.Trim().ToUpper(),
                        totalCp: n.TotalCp,
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp,
                        usuarioCreacion: request.Usuario ?? "sistema",
                        biGravada: n.BiGravada,
                        igvIpm: n.IgvIpm
                    );

                    ventasNuevas.Add(nuevaVenta);
                }

                await _ventaRepo.AgregarRangoAsync(ventasNuevas, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3.2 Actualizar comprobantes existentes modificados
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var idsModificados = request.Modificados.Select(m => m.IdVenta).ToList();
                var ventasExistentes = await _ventaRepo.ObtenerPorIdsAsync(idsModificados, cancellationToken);
                var dictExistentes = ventasExistentes.ToDictionary(v => v.IdVenta);

                foreach (var m in request.Modificados)
                {
                    if (!dictExistentes.TryGetValue(m.IdVenta, out var ventaExistente))
                    {
                        continue;
                    }

                    // Validar campos obligatorios
                    if (!m.FechaEmision.HasValue)
                        throw new ValidationException($"La fecha de emisión es obligatoria para el comprobante {m.Serie}-{m.Numero}.");
                    if (string.IsNullOrWhiteSpace(m.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante de Pago es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.Serie))
                        throw new ValidationException("La Serie del comprobante es obligatoria.");
                    if (string.IsNullOrWhiteSpace(m.Numero))
                        throw new ValidationException("El Número del comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.CodigoTipoDocIdentidad))
                        throw new ValidationException("El Tipo de Documento de Identidad del cliente es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.NroDocIdentidad))
                        throw new ValidationException("El Número de Documento de Identidad del cliente es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.RazonSocial))
                        throw new ValidationException("La Razón Social o Nombre del cliente es obligatoria.");

                    var moneda = string.IsNullOrWhiteSpace(m.CodigoMoneda) ? "PEN" : m.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (m.TipoCambio.HasValue && m.TipoCambio.Value > 0) ? m.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(m.CodigoEstadoComprobante) ? "1" : m.CodigoEstadoComprobante.Trim();

                    // Recalcular CAR SUNAT si no viene provisto o si cambiaron sus componentes
                    var carSunat = !string.IsNullOrWhiteSpace(m.CarSunat)
                        ? m.CarSunat.Trim()
                        : $"{carga.EmpresaRuc}{m.CodigoTipoCp.Trim()}{m.Serie.Trim().PadLeft(4, '0')}{m.Numero.Trim().PadLeft(8, '0')}";

                    ventaExistente.ActualizarDatos(
                        codigoTipoCp: m.CodigoTipoCp.Trim(),
                        serie: m.Serie.Trim().ToUpper(),
                        numero: m.Numero.Trim(),
                        fechaEmision: m.FechaEmision.Value,
                        codigoTipoDocIdentidad: m.CodigoTipoDocIdentidad.Trim(),
                        nroDocIdentidad: m.NroDocIdentidad.Trim(),
                        razonSocial: m.RazonSocial.Trim().ToUpper(),
                        biGravada: m.BiGravada,
                        igvIpm: m.IgvIpm,
                        totalCp: m.TotalCp,
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp,
                        carSunat: carSunat,
                        usuarioModificacion: request.Usuario ?? "sistema"
                    );
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 4. Obtener los comprobantes restantes de la carga (incluye los recién agregados, modificados y excluye los eliminados)
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

            // 7. Actualizar conteos y montos acumulados de la carga en operaciones.archivo_carga
            var totalRestantes = ventasRestantes.Count;
            var totalObservaciones = nuevosErrores.Count;
            var totalValidos = Math.Max(0, totalRestantes - totalObservaciones);

            var totalBiRestantes = ventasRestantes.Sum(v => v.BiGravada);
            var totalIgvRestantes = ventasRestantes.Sum(v => v.IgvIpm);
            var totalGenRestantes = ventasRestantes.Sum(v => v.TotalCp);

            carga.ActualizarConteoYMontos(totalRestantes, totalValidos, totalObservaciones, totalBiRestantes, totalIgvRestantes, totalGenRestantes);
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
