using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarCompras;

/// <summary>
/// Aplica cambios manuales (altas/bajas/modificaciones) sobre los comprobantes
/// de una carga de compras SIRE existente: transaccional, con autorización multi-tenant,
/// revalidación de observaciones y recálculo de conteos/totales.
/// </summary>
public class ActualizarComprasCommandHandler : IRequestHandler<ActualizarComprasCommand, ActualizarComprasResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraSireRepository _compraSireRepo;
    private readonly ICompraSireValidationService _compraValidationService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarComprasCommandHandler> _logger;

    public ActualizarComprasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraSireRepository compraSireRepo,
        ICompraSireValidationService compraValidationService,
        IAccesoEmpresaValidator accesoValidator,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarComprasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraSireRepo = compraSireRepo;
        _compraValidationService = compraValidationService;
        _accesoValidator = accesoValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarComprasResponseDTO> Handle(ActualizarComprasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización de compras SIRE para carga {IdCarga}. Eliminados: {Count}",
            request.IdCarga, request.EliminadosIds?.Count ?? 0);

        // 1. Obtener la carga correspondiente
        var carga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (carga is null)
        {
            throw new NotFoundException("Carga", request.IdCarga);
        }

        // Autorización multi-tenant: solo se modifican cargas de empresas accesibles
        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        // 2. Iniciar transacción en UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 3. Eliminar comprobantes físicamente de la base de datos
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _compraSireRepo.EliminarRangoFisicoAsync(request.EliminadosIds, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3.1 Agregar nuevos comprobantes validados
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var comprasNuevas = new List<CompraSire>();
                var lineaActual = 1;

                foreach (var n in request.Nuevos)
                {
                    if (!n.FechaEmision.HasValue)
                        throw new ValidationException("La fecha de emisión es obligatoria para los nuevos comprobantes.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante de Pago es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.Serie))
                        throw new ValidationException("La Serie del comprobante es obligatoria.");
                    if (string.IsNullOrWhiteSpace(n.Numero))
                        throw new ValidationException("El Número del comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoDocIdentidad))
                        throw new ValidationException("El Tipo de Documento de Identidad del proveedor es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.NroDocIdentidad))
                        throw new ValidationException("El Número de Documento de Identidad del proveedor es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.RazonSocial))
                        throw new ValidationException("La Razón Social o Nombre del proveedor es obligatoria.");

                    var moneda = string.IsNullOrWhiteSpace(n.CodigoMoneda) ? "PEN" : n.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (n.TipoCambio.HasValue && n.TipoCambio.Value > 0) ? n.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(n.CodigoEstadoComprobante) ? "1" : n.CodigoEstadoComprobante.Trim();

                    // Guardar CAR SUNAT si viene provisto, de lo contrario null
                    var carSunat = !string.IsNullOrWhiteSpace(n.CarSunat) ? n.CarSunat.Trim() : null;

                    var nuevaCompra = CompraSire.Crear(
                        empresaRuc: !string.IsNullOrWhiteSpace(n.EmpresaRuc) ? n.EmpresaRuc.Trim() : carga.EmpresaRuc,
                        periodo: !string.IsNullOrWhiteSpace(n.Periodo) ? n.Periodo.Trim() : carga.Periodo,
                        idCarga: carga.IdCarga,
                        numeroLinea: lineaActual++,
                        fechaEmision: n.FechaEmision.Value,
                        codigoTipoCp: n.CodigoTipoCp.Trim(),
                        serie: n.Serie.Trim().ToUpper(),
                        numero: n.Numero.Trim(),
                        codigoTipoDocIdentidad: n.CodigoTipoDocIdentidad.Trim(),
                        nroDocIdentidad: n.NroDocIdentidad.Trim(),
                        razonSocial: n.RazonSocial.Trim().ToUpper(),
                        totalCp: n.TotalCp,
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp,
                        usuarioCreacion: request.Usuario ?? "sistema",
                        carSunat: carSunat,
                        biGravadoDg: n.BiGravadoDg,
                        igvIpmDg: n.IgvIpmDg,
                        detraccion: string.IsNullOrWhiteSpace(n.Detraccion) ? null : n.Detraccion.Trim());

                    comprasNuevas.Add(nuevaCompra);
                }

                await _compraSireRepo.AgregarRangoAsync(comprasNuevas, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3.2 Actualizar comprobantes existentes modificados
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var idsModificados = request.Modificados.Select(m => m.IdCompra).ToList();
                var comprasExistentes = await _compraSireRepo.ObtenerPorIdsAsync(idsModificados, cancellationToken);
                var dictExistentes = comprasExistentes.ToDictionary(c => c.IdCompra);

                foreach (var m in request.Modificados)
                {
                    if (!dictExistentes.TryGetValue(m.IdCompra, out var compraExistente))
                    {
                        continue;
                    }

                    if (!m.FechaEmision.HasValue)
                        throw new ValidationException($"La fecha de emisión es obligatoria para el comprobante {m.Serie}-{m.Numero}.");
                    if (string.IsNullOrWhiteSpace(m.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante de Pago es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.Serie))
                        throw new ValidationException("La Serie del comprobante es obligatoria.");
                    if (string.IsNullOrWhiteSpace(m.Numero))
                        throw new ValidationException("El Número del comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.CodigoTipoDocIdentidad))
                        throw new ValidationException("El Tipo de Documento de Identidad del proveedor es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.NroDocIdentidad))
                        throw new ValidationException("El Número de Documento de Identidad del proveedor es obligatorio.");
                    if (string.IsNullOrWhiteSpace(m.RazonSocial))
                        throw new ValidationException("La Razón Social o Nombre del proveedor es obligatoria.");

                    var moneda = string.IsNullOrWhiteSpace(m.CodigoMoneda) ? "PEN" : m.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (m.TipoCambio.HasValue && m.TipoCambio.Value > 0) ? m.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(m.CodigoEstadoComprobante) ? "1" : m.CodigoEstadoComprobante.Trim();

                    // Guardar CAR SUNAT si viene provisto, de lo contrario null
                    var carSunat = !string.IsNullOrWhiteSpace(m.CarSunat) ? m.CarSunat.Trim() : null;

                    compraExistente.ActualizarDatos(
                        codigoTipoCp: m.CodigoTipoCp.Trim(),
                        serie: m.Serie.Trim().ToUpper(),
                        numero: m.Numero.Trim(),
                        fechaEmision: m.FechaEmision.Value,
                        codigoTipoDocIdentidad: m.CodigoTipoDocIdentidad.Trim(),
                        nroDocIdentidad: m.NroDocIdentidad.Trim(),
                        razonSocial: m.RazonSocial.Trim().ToUpper(),
                        biGravadoDg: m.BiGravadoDg,
                        igvIpmDg: m.IgvIpmDg,
                        totalCp: m.TotalCp,
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp,
                        detraccion: string.IsNullOrWhiteSpace(m.Detraccion) ? null : m.Detraccion.Trim(),
                        carSunat: carSunat,
                        usuarioModificacion: request.Usuario ?? "sistema",
                        empresaRuc: !string.IsNullOrWhiteSpace(m.EmpresaRuc) ? m.EmpresaRuc.Trim() : null);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 4. Obtener los comprobantes restantes de la carga
            var comprasRestantes = await _compraSireRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

            // 5. Re-validar los comprobantes restantes
            var nuevosErrores = await _compraValidationService.ValidarComprasSireAsync(
                request.IdCarga,
                carga.EmpresaRuc,
                carga.Periodo,
                comprasRestantes,
                cancellationToken);

            // 6. Eliminar observaciones anteriores de la carga y registrar las nuevas
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(request.IdCarga, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 7. Actualizar conteos y montos acumulados de la carga
            var totalRestantes = comprasRestantes.Count;
            var totalObservaciones = nuevosErrores.Count;
            var totalValidos = Math.Max(0, totalRestantes - totalObservaciones);

            var totalBiRestantes = comprasRestantes.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            var totalIgvRestantes = comprasRestantes.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);
            var totalGenRestantes = comprasRestantes.Sum(c => c.TotalCp);

            carga.ActualizarConteoYMontos(totalRestantes, totalValidos, totalObservaciones, totalBiRestantes, totalIgvRestantes, totalGenRestantes);
            if (!string.IsNullOrWhiteSpace(request.Usuario))
            {
                carga.ModificadoPor = request.Usuario;
                carga.FechaModificacion = DateTime.UtcNow;
            }
            await _archivoCargaRepo.ActualizarAsync(carga, cancellationToken);

            // 8. Commit transacción en UnitOfWork
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Actualización de compras SIRE para carga {IdCarga} completada. Restantes: {Count}, Observaciones: {Obs}",
                request.IdCarga, comprasRestantes.Count, nuevosErrores.Count);

            return new ActualizarComprasResponseDTO
            {
                Exito = true,
                Mensaje = "Registros actualizados y comprobantes revalidados correctamente.",
                NumRegistros = comprasRestantes.Count,
                NumObservaciones = nuevosErrores.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar compras SIRE de carga {IdCarga}. Ejecutando Rollback.", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
