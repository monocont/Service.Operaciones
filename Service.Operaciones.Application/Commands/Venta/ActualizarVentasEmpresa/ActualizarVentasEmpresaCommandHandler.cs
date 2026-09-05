using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using VentaEmpresaEntity = Service.Operaciones.Domain.Entities.VentaEmpresa;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa;

public class ActualizarVentasEmpresaCommandHandler : IRequestHandler<ActualizarVentasEmpresaCommand, ActualizarVentasEmpresaResultadoDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;
    private readonly IVentaEmpresaValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarVentasEmpresaCommandHandler> _logger;

    public ActualizarVentasEmpresaCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaEmpresaRepository ventaEmpresaRepo,
        IVentaEmpresaValidationService validationService,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarVentasEmpresaCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaEmpresaRepo = ventaEmpresaRepo;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarVentasEmpresaResultadoDTO> Handle(ActualizarVentasEmpresaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización y revalidación de Ventas Empresa para IdCarga: {IdCarga}", request.IdCarga);

        var archivoCarga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (archivoCarga is null)
        {
            throw new NotFoundException($"No se encontró la carga con identificador {request.IdCarga}");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Eliminar comprobantes seleccionados
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _ventaEmpresaRepo.EliminarRangoFisicoAsync(request.EliminadosIds, cancellationToken);
            }

            // 2. Modificar comprobantes existentes
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var idsModif = request.Modificados.Select(m => m.IdVentaEmpresa).ToList();
                var existentes = await _ventaEmpresaRepo.ObtenerPorIdsAsync(idsModif, cancellationToken);

                foreach (var mod in request.Modificados)
                {
                    var ent = existentes.FirstOrDefault(e => e.IdVentaEmpresa == mod.IdVentaEmpresa);
                    if (ent != null)
                    {
                        ent.ActualizarDatos(
                            fechaEmision: mod.FechaEmision,
                            codigoTipoCp: mod.CodigoTipoCp,
                            serie: mod.Serie,
                            numero: mod.Numero,
                            codigoTipoDocIdentidad: mod.CodigoTipoDocIdentidad,
                            nroDocIdentidad: mod.NroDocIdentidad,
                            totalCp: mod.TotalCp,
                            codigoMoneda: mod.CodigoMoneda,
                            tipoCambio: mod.TipoCambio,
                            usuarioModificacion: request.Usuario,
                            razonSocial: mod.RazonSocial,
                            carSunat: mod.CarSunat,
                            fechaVencimiento: mod.FechaVencimiento,
                            numeroFinal: mod.NumeroFinal,
                            valorFacturadoExportacion: mod.ValorFacturadoExportacion,
                            biGravada: mod.BiGravada,
                            descuentoBi: mod.DescuentoBi,
                            igvIpm: mod.IgvIpm,
                            descuentoIgv: mod.DescuentoIgv,
                            montoExonerado: mod.MontoExonerado,
                            montoInafecto: mod.MontoInafecto,
                            montoIsc: mod.MontoIsc,
                            biGravadaIvap: mod.BiGravadaIvap,
                            montoIvap: mod.MontoIvap,
                            montoIcbper: mod.MontoIcbper,
                            montoOtrosTributos: mod.MontoOtrosTributos,
                            fechaEmisionDocModificado: mod.FechaEmisionDocModificado,
                            codigoTipoCpModificado: mod.CodigoTipoCpModificado,
                            serieCpModificado: mod.SerieCpModificado,
                            numeroCpModificado: mod.NumeroCpModificado,
                            codigoEstadoComprobante: mod.CodigoEstadoComprobante,
                            codigoTipoNota: mod.CodigoTipoNota,
                            tipoOperacion: mod.TipoOperacion,
                            camposLibres: mod.CamposLibres);
                    }
                }
            }

            // 3. Crear nuevos comprobantes si los hubiere
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var nuevosEntidades = request.Nuevos.Select((n, idx) => VentaEmpresaEntity.Crear(
                    idCarga: archivoCarga.IdCarga,
                    empresaRuc: archivoCarga.EmpresaRuc,
                    periodo: archivoCarga.Periodo,
                    numeroLinea: idx + 1,
                    fechaEmision: n.FechaEmision,
                    codigoTipoCp: n.CodigoTipoCp,
                    serie: n.Serie,
                    numero: n.Numero,
                    codigoTipoDocIdentidad: n.CodigoTipoDocIdentidad,
                    nroDocIdentidad: n.NroDocIdentidad,
                    totalCp: n.TotalCp,
                    codigoMoneda: n.CodigoMoneda,
                    tipoCambio: n.TipoCambio,
                    usuarioCreacion: request.Usuario,
                    razonSocial: n.RazonSocial,
                    carSunat: n.CarSunat,
                    fechaVencimiento: n.FechaVencimiento,
                    numeroFinal: n.NumeroFinal,
                    valorFacturadoExportacion: n.ValorFacturadoExportacion,
                    biGravada: n.BiGravada,
                    descuentoBi: n.DescuentoBi,
                    igvIpm: n.IgvIpm,
                    descuentoIgv: n.DescuentoIgv,
                    montoExonerado: n.MontoExonerado,
                    montoInafecto: n.MontoInafecto,
                    montoIsc: n.MontoIsc,
                    biGravadaIvap: n.BiGravadaIvap,
                    montoIvap: n.MontoIvap,
                    montoIcbper: n.MontoIcbper,
                    montoOtrosTributos: n.MontoOtrosTributos,
                    fechaEmisionDocModificado: n.FechaEmisionDocModificado,
                    codigoTipoCpModificado: n.CodigoTipoCpModificado,
                    serieCpModificado: n.SerieCpModificado,
                    numeroCpModificado: n.NumeroCpModificado,
                    codigoEstadoComprobante: n.CodigoEstadoComprobante,
                    codigoTipoNota: n.CodigoTipoNota,
                    tipoOperacion: n.TipoOperacion,
                    camposLibres: n.CamposLibres
                )).ToList();

                await _ventaEmpresaRepo.AgregarRangoAsync(nuevosEntidades, cancellationToken);
            }

            // 3.1 Persistir modificaciones/altas/bajas en la transacción antes de revalidar
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Limpiar errores previos para regenerar diagnóstico completo
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(archivoCarga.IdCarga, cancellationToken);

            // 5. Obtener lista completa consolidada y revalidar reglas de negocio
            var ventasConsolidadas = await _ventaEmpresaRepo.ListarPorCargaAsync(archivoCarga.IdCarga, cancellationToken);

            var nuevosErrores = await _validationService.ValidarVentasEmpresaAsync(
                archivoCarga.IdCarga,
                archivoCarga.EmpresaRuc,
                archivoCarga.Periodo,
                ventasConsolidadas,
                cancellationToken);

            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 6. Actualizar métricas acumuladas en cabecera
            decimal totalGeneral = ventasConsolidadas.Sum(v => v.TotalCp);
            archivoCarga.ActualizarConteoYMontos(
                ventasConsolidadas.Count,
                ventasConsolidadas.Count,
                nuevosErrores.Count,
                0,
                0,
                totalGeneral);

            // 7. Persistir nuevos errores/métricas y confirmar transacción atómicamente
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ActualizarVentasEmpresaResultadoDTO
            {
                IdCarga = archivoCarga.IdCarga,
                NumRegistros = ventasConsolidadas.Count,
                NumRegistrosValidos = ventasConsolidadas.Count,
                NumRegistrosError = nuevosErrores.Count,
                TotalGeneral = totalGeneral,
                Mensaje = "Registros de Ventas de Empresa actualizados y revalidados correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar y revalidar ventas empresa para IdCarga {IdCarga}", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
