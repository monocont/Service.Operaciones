using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using CompraEmpresaEntity = Service.Operaciones.Domain.Entities.CompraEmpresa;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarComprasEmpresa;

public class ActualizarComprasEmpresaCommandHandler : IRequestHandler<ActualizarComprasEmpresaCommand, ActualizarComprasEmpresaResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;
    private readonly ICompraEmpresaValidationService _validationService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarComprasEmpresaCommandHandler> _logger;

    public ActualizarComprasEmpresaCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraEmpresaRepository compraEmpresaRepo,
        ICompraEmpresaValidationService validationService,
        IAccesoEmpresaValidator accesoValidator,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarComprasEmpresaCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraEmpresaRepo = compraEmpresaRepo;
        _validationService = validationService;
        _accesoValidator = accesoValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarComprasEmpresaResponseDTO> Handle(ActualizarComprasEmpresaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización y revalidación de Compras Empresa para IdCarga: {IdCarga}", request.IdCarga);

        var archivoCarga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (archivoCarga is null)
        {
            throw new NotFoundException($"No se encontró la carga con identificador {request.IdCarga}");
        }

        await _accesoValidator.ValidarAccesoAsync(archivoCarga.EmpresaRuc, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Eliminar comprobantes seleccionados físicamente
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _compraEmpresaRepo.EliminarRangoFisicoAsync(request.EliminadosIds, cancellationToken);
            }

            // 2. Modificar comprobantes existentes
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var idsModif = request.Modificados.Select(m => m.IdCompraEmpresa).ToList();
                var existentes = await _compraEmpresaRepo.ObtenerPorIdsAsync(idsModif, cancellationToken);
                var dictExistentes = existentes.ToDictionary(e => e.IdCompraEmpresa);

                foreach (var mod in request.Modificados)
                {
                    if (dictExistentes.TryGetValue(mod.IdCompraEmpresa, out var ent))
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
                            anioDocumento: mod.AnioDocumento,
                            numeroFinal: mod.NumeroFinal,
                            biGravadoDg: mod.BiGravadoDg,
                            igvIpmDg: mod.IgvIpmDg,
                            biGravadoDgng: mod.BiGravadoDgng,
                            igvIpmDgng: mod.IgvIpmDgng,
                            biGravadoDng: mod.BiGravadoDng,
                            igvIpmDng: mod.IgvIpmDng,
                            valorAdqNg: mod.ValorAdqNg,
                            montoIsc: mod.MontoIsc,
                            montoIcbper: mod.MontoIcbper,
                            montoOtrosTributos: mod.MontoOtrosTributos,
                            fechaEmisionDocModificado: mod.FechaEmisionDocModificado,
                            codigoTipoCpModificado: mod.CodigoTipoCpModificado,
                            serieCpModificado: mod.SerieCpModificado,
                            codDamDsi: mod.CodDamDsi,
                            numeroCpModificado: mod.NumeroCpModificado,
                            clasifBssSss: mod.ClasifBssSss,
                            idProyectoOp: mod.IdProyectoOp,
                            porcPart: mod.PorcPart,
                            imb: mod.Imb,
                            carOrigIndEI: mod.CarOrigIndEI,
                            detraccion: mod.Detraccion,
                            codigoTipoNota: mod.CodigoTipoNota,
                            codigoEstadoComprobante: mod.CodigoEstadoComprobante,
                            incal: mod.Incal,
                            camposLibres: mod.CamposLibres);
                    }
                }
            }

            // 3. Crear nuevos comprobantes si los hubiere
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var nuevosEntidades = request.Nuevos.Select((n, idx) => CompraEmpresaEntity.Crear(
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
                    anioDocumento: n.AnioDocumento,
                    numeroFinal: n.NumeroFinal,
                    biGravadoDg: n.BiGravadoDg,
                    igvIpmDg: n.IgvIpmDg,
                    biGravadoDgng: n.BiGravadoDgng,
                    igvIpmDgng: n.IgvIpmDgng,
                    biGravadoDng: n.BiGravadoDng,
                    igvIpmDng: n.IgvIpmDng,
                    valorAdqNg: n.ValorAdqNg,
                    montoIsc: n.MontoIsc,
                    montoIcbper: n.MontoIcbper,
                    montoOtrosTributos: n.MontoOtrosTributos,
                    fechaEmisionDocModificado: n.FechaEmisionDocModificado,
                    codigoTipoCpModificado: n.CodigoTipoCpModificado,
                    serieCpModificado: n.SerieCpModificado,
                    codDamDsi: n.CodDamDsi,
                    numeroCpModificado: n.NumeroCpModificado,
                    clasifBssSss: n.ClasifBssSss,
                    idProyectoOp: n.IdProyectoOp,
                    porcPart: n.PorcPart,
                    imb: n.Imb,
                    carOrigIndEI: n.CarOrigIndEI,
                    detraccion: n.Detraccion,
                    codigoTipoNota: n.CodigoTipoNota,
                    codigoEstadoComprobante: n.CodigoEstadoComprobante,
                    incal: n.Incal,
                    camposLibres: n.CamposLibres
                )).ToList();

                await _compraEmpresaRepo.AgregarRangoAsync(nuevosEntidades, cancellationToken);
            }

            // 4. Persistir modificaciones/altas/bajas en la transacción antes de revalidar
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Limpiar errores previos para regenerar diagnóstico completo
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(archivoCarga.IdCarga, cancellationToken);

            // 6. Obtener lista completa consolidada y revalidar reglas de negocio
            var comprasConsolidadas = await _compraEmpresaRepo.ListarPorCargaAsync(archivoCarga.IdCarga, cancellationToken);

            var nuevosErrores = await _validationService.ValidarComprasEmpresaAsync(
                archivoCarga.IdCarga,
                archivoCarga.EmpresaRuc,
                archivoCarga.Periodo,
                comprasConsolidadas,
                cancellationToken);

            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 7. Actualizar métricas acumuladas en cabecera
            var totalRestantes = comprasConsolidadas.Count;
            var totalObservaciones = nuevosErrores.Count;
            var totalValidos = Math.Max(0, totalRestantes - totalObservaciones);

            var totalBiRestantes = comprasConsolidadas.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            var totalIgvRestantes = comprasConsolidadas.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);
            var totalGeneral = comprasConsolidadas.Sum(c => c.TotalCp);

            archivoCarga.ActualizarConteoYMontos(
                totalRestantes,
                totalValidos,
                totalObservaciones,
                totalBiRestantes,
                totalIgvRestantes,
                totalGeneral);

            if (!string.IsNullOrWhiteSpace(request.Usuario))
            {
                archivoCarga.ModificadoPor = request.Usuario;
                archivoCarga.FechaModificacion = DateTime.UtcNow;
            }

            await _archivoCargaRepo.ActualizarAsync(archivoCarga, cancellationToken);

            // 8. Persistir nuevos errores/métricas y confirmar transacción atómicamente
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Actualización de compras empresa para carga {IdCarga} completada. Restantes: {Count}, Observaciones: {Obs}",
                request.IdCarga, comprasConsolidadas.Count, nuevosErrores.Count);

            return new ActualizarComprasEmpresaResponseDTO
            {
                IdCarga = archivoCarga.IdCarga,
                NumRegistros = comprasConsolidadas.Count,
                NumRegistrosValidos = totalValidos,
                NumRegistrosError = totalObservaciones,
                TotalGeneral = totalGeneral,
                Mensaje = "Registros de Compras de Empresa actualizados y revalidados correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar y revalidar compras empresa para IdCarga {IdCarga}", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
