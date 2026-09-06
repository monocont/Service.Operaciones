using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarComprasMatch;

public class ActualizarComprasMatchCommandHandler : IRequestHandler<ActualizarComprasMatchCommand, ActualizarComprasMatchResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraMatchRepository _compraMatchRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActualizarComprasMatchCommandHandler> _logger;

    public ActualizarComprasMatchCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IAccesoEmpresaValidator accesoValidator,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraMatchRepository compraMatchRepo,
        IUnitOfWork unitOfWork,
        ILogger<ActualizarComprasMatchCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _accesoValidator = accesoValidator;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraMatchRepo = compraMatchRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ActualizarComprasMatchResponseDTO> Handle(ActualizarComprasMatchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando actualización de compras_match para carga {IdCarga}. Eliminados: {Eliminados}, Nuevos: {Nuevos}, Modificados: {Modificados}",
            request.IdCarga, request.EliminadosIds?.Count ?? 0, request.Nuevos?.Count ?? 0, request.Modificados?.Count ?? 0);

        var carga = await _archivoCargaRepo.ObtenerPorIdAsync(request.IdCarga, cancellationToken);
        if (carga is null)
        {
            throw new NotFoundException("Carga", request.IdCarga);
        }

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Eliminar comprobantes seleccionados
            if (request.EliminadosIds != null && request.EliminadosIds.Count > 0)
            {
                await _compraMatchRepo.EliminarRangoPorIdsAsync(request.EliminadosIds, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Obtener lista actual de comprobantes vigentes en base de datos
            var comprobantesActuales = await _compraMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);
            var dictActuales = comprobantesActuales.ToDictionary(c => c.IdCompraMatch);

            // 3. Procesar Modificaciones
            if (request.Modificados != null && request.Modificados.Count > 0)
            {
                var paraActualizar = new List<CompraMatch>();
                foreach (var m in request.Modificados)
                {
                    if (dictActuales.TryGetValue(m.IdCompraMatch, out var entidad))
                    {
                        var numeroNormalizado = NormalizarNumeroComprobante(m.Numero ?? entidad.Numero);
                        var serieNormalizada = (m.Serie ?? entidad.Serie)?.Trim().ToUpper() ?? string.Empty;

                        entidad.ActualizarDatos(
                            origenDato: m.OrigenDato ?? entidad.OrigenDato,
                            esCoincidenciaExacta: entidad.EsCoincidenciaExacta,
                            esDiferencia: entidad.EsDiferencia,
                            esSoloUnOrigen: entidad.EsSoloUnOrigen,
                            codigoTipoCp: m.CodigoTipoCp?.Trim() ?? entidad.CodigoTipoCp,
                            serie: serieNormalizada,
                            numero: numeroNormalizado,
                            fechaEmision: m.FechaEmision ?? entidad.FechaEmision,
                            codigoTipoDocIdentidad: m.CodigoTipoDocIdentidad?.Trim() ?? entidad.CodigoTipoDocIdentidad,
                            nroDocIdentidad: m.NroDocIdentidad?.Trim() ?? entidad.NroDocIdentidad,
                            razonSocial: (m.RazonSocial?.Trim() ?? entidad.RazonSocial).ToUpperInvariant(),
                            totalCp: m.TotalCp,
                            usuarioModificacion: request.Usuario ?? "sistema",
                            carSunat: m.CarSunat ?? entidad.CarSunat,
                            fechaVencimiento: m.FechaVencimiento ?? entidad.FechaVencimiento,
                            anioDocumento: entidad.AnioDocumento,
                            numeroFinal: entidad.NumeroFinal,
                            biGravadoDg: m.BiGravadoDg,
                            igvIpmDg: m.IgvIpmDg,
                            biGravadoDgng: m.BiGravadoDgng,
                            igvIpmDgng: m.IgvIpmDgng,
                            biGravadoDng: m.BiGravadoDng,
                            igvIpmDng: m.IgvIpmDng,
                            valorAdqNg: m.ValorAdqNg,
                            montoIsc: m.MontoIsc,
                            montoIcbper: m.MontoIcbper,
                            montoOtrosTributos: m.MontoOtrosTributos,
                            codigoMoneda: string.IsNullOrWhiteSpace(m.CodigoMoneda) ? entidad.CodigoMoneda : m.CodigoMoneda.Trim().ToUpper(),
                            tipoCambio: (m.TipoCambio.HasValue && m.TipoCambio.Value > 0) ? m.TipoCambio.Value : entidad.TipoCambio,
                            fechaEmisionDocModificado: entidad.FechaEmisionDocModificado,
                            codigoTipoCpModificado: entidad.CodigoTipoCpModificado,
                            serieCpModificado: entidad.SerieCpModificado,
                            codDamDsi: entidad.CodDamDsi,
                            numeroCpModificado: entidad.NumeroCpModificado,
                            clasifBssSss: entidad.ClasifBssSss,
                            idProyectoOp: entidad.IdProyectoOp,
                            porcPart: entidad.PorcPart,
                            imb: entidad.Imb,
                            carOrigIndEI: entidad.CarOrigIndEI,
                            detraccion: entidad.Detraccion,
                            codigoTipoNota: entidad.CodigoTipoNota,
                            codigoEstadoComprobante: string.IsNullOrWhiteSpace(m.CodigoEstadoComprobante) ? entidad.CodigoEstadoComprobante : m.CodigoEstadoComprobante.Trim(),
                            incal: entidad.Incal,
                            camposLibres: entidad.CamposLibres
                        );

                        paraActualizar.Add(entidad);
                    }
                }

                if (paraActualizar.Count > 0)
                {
                    await _compraMatchRepo.ActualizarRangoAsync(paraActualizar, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // 4. Procesar Inserciones Nuevas
            if (request.Nuevos != null && request.Nuevos.Count > 0)
            {
                var nuevosEntidades = new List<CompraMatch>();
                var maxLinea = comprobantesActuales.Count > 0 ? comprobantesActuales.Max(c => c.NumeroLinea) : 0;

                foreach (var n in request.Nuevos)
                {
                    if (!n.FechaEmision.HasValue)
                        throw new ValidationException("La fecha de emisión es obligatoria para nuevos registros.");
                    if (string.IsNullOrWhiteSpace(n.CodigoTipoCp))
                        throw new ValidationException("El Tipo de Comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.Serie))
                        throw new ValidationException("La Serie es obligatoria.");
                    if (string.IsNullOrWhiteSpace(n.Numero))
                        throw new ValidationException("El Número de comprobante es obligatorio.");
                    if (string.IsNullOrWhiteSpace(n.NroDocIdentidad))
                        throw new ValidationException("El RUC o Documento del proveedor es obligatorio.");

                    var numeroNormalizado = NormalizarNumeroComprobante(n.Numero);
                    var serieNormalizada = n.Serie.Trim().ToUpper();
                    var moneda = string.IsNullOrWhiteSpace(n.CodigoMoneda) ? "PEN" : n.CodigoMoneda.Trim().ToUpper();
                    var tipoCambio = (n.TipoCambio.HasValue && n.TipoCambio.Value > 0) ? n.TipoCambio.Value : 1.0000m;
                    var estadoCp = string.IsNullOrWhiteSpace(n.CodigoEstadoComprobante) ? "1" : n.CodigoEstadoComprobante.Trim();

                    maxLinea++;
                    var nuevo = CompraMatch.Crear(
                        idCarga: carga.IdCarga,
                        empresaRuc: carga.EmpresaRuc,
                        periodo: carga.Periodo,
                        numeroLinea: maxLinea,
                        origenDato: string.IsNullOrWhiteSpace(n.OrigenDato) ? "SIRE" : n.OrigenDato.Trim().ToUpper(),
                        esCoincidenciaExacta: true,
                        esDiferencia: false,
                        esSoloUnOrigen: false,
                        codigoTipoCp: n.CodigoTipoCp.Trim(),
                        serie: serieNormalizada,
                        numero: numeroNormalizado,
                        fechaEmision: n.FechaEmision.Value,
                        codigoTipoDocIdentidad: n.CodigoTipoDocIdentidad?.Trim() ?? "6",
                        nroDocIdentidad: n.NroDocIdentidad?.Trim() ?? string.Empty,
                        razonSocial: (n.RazonSocial?.Trim() ?? string.Empty).ToUpperInvariant(),
                        totalCp: n.TotalCp,
                        usuarioCreacion: request.Usuario ?? "sistema",
                        carSunat: n.CarSunat,
                        fechaVencimiento: n.FechaVencimiento,
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
                        codigoMoneda: moneda,
                        tipoCambio: tipoCambio,
                        codigoEstadoComprobante: estadoCp
                    );

                    nuevosEntidades.Add(nuevo);
                }

                await _compraMatchRepo.AgregarRangoAsync(nuevosEntidades, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Recuperar todos los comprobantes consolidados vigentes tras mutaciones
            var todosConsolidados = await _compraMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

            // 6. Validar discrepancias de match directamente sobre el estado consolidado actual de compra_match
            var porComprobante = todosConsolidados
                .GroupBy(c => $"{c.NroDocIdentidad?.Trim()}|{c.CodigoTipoCp?.Trim().PadLeft(2,'0')}|{c.Serie?.Trim().ToUpper()}|{NormalizarNumeroComprobante(c.Numero)}")
                .ToList();

            var nuevosErrores = new List<ArchivoCargaError>();
            var paraActualizarBanderas = new List<CompraMatch>();

            foreach (var grupo in porComprobante)
            {
                var items = grupo.ToList();
                if (items.Count > 1)
                {
                    var sire = items.FirstOrDefault(x => x.OrigenDato == "SIRE") ?? items[0];
                    var emp = items.FirstOrDefault(x => x.OrigenDato == "EMPRESA") ?? items[1];

                    var diffs = new List<string>();
                    if (sire.FechaEmision.Date != emp.FechaEmision.Date)
                        diffs.Add("No coincide la Fecha de Emisión");
                    if ((sire.CodigoTipoDocIdentidad ?? string.Empty).Trim() != (emp.CodigoTipoDocIdentidad ?? string.Empty).Trim())
                        diffs.Add("No coincide el Tipo de Documento de Identidad");
                    if ((sire.NroDocIdentidad ?? string.Empty).Trim() != (emp.NroDocIdentidad ?? string.Empty).Trim())
                        diffs.Add("No coincide el RUC / Documento del Proveedor");
                    if (sire.BiGravadoDg != emp.BiGravadoDg)
                        diffs.Add("No coincide la Base Imponible Gravada (BI Gravado DG)");
                    if (sire.IgvIpmDg != emp.IgvIpmDg)
                        diffs.Add("No coincide el IGV / IPM Gravado (IGV DG)");
                    if (sire.TotalCp != emp.TotalCp)
                        diffs.Add("No coincide el Total CP");
                    if ((sire.CodigoMoneda ?? "PEN").Trim().ToUpper() != (emp.CodigoMoneda ?? "PEN").Trim().ToUpper())
                        diffs.Add("No coincide la Moneda");

                    if (diffs.Count > 0)
                    {
                        foreach (var d in diffs)
                        {
                            nuevosErrores.Add(ArchivoCargaError.Crear(
                                idCarga: carga.IdCarga,
                                numeroLinea: sire.NumeroLinea,
                                tipoError: TipoErrorCarga.Validacion,
                                mensaje: $"{d} en comprobante {sire.Serie}-{sire.Numero} (Proveedor {sire.NroDocIdentidad})",
                                campoError: "match_discrepancia",
                                valorLectura: $"{sire.Serie}-{sire.Numero}",
                                severidad: SeveridadError.Error
                            ));
                        }

                        foreach (var item in items)
                        {
                            if (!item.EsDiferencia || item.EsCoincidenciaExacta)
                            {
                                item.ActualizarBanderasMatch(esCoincidenciaExacta: false, esDiferencia: true, esSoloUnOrigen: false);
                                paraActualizarBanderas.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            if (item.EsDiferencia || !item.EsCoincidenciaExacta)
                            {
                                item.ActualizarBanderasMatch(esCoincidenciaExacta: true, esDiferencia: false, esSoloUnOrigen: false);
                                paraActualizarBanderas.Add(item);
                            }
                        }
                    }
                }
                else if (items.Count == 1)
                {
                    var item = items[0];
                    if (item.EsDiferencia)
                    {
                        item.ActualizarBanderasMatch(esCoincidenciaExacta: true, esDiferencia: false, esSoloUnOrigen: false);
                        paraActualizarBanderas.Add(item);
                    }
                }
            }

            if (paraActualizarBanderas.Count > 0)
            {
                await _compraMatchRepo.ActualizarRangoAsync(paraActualizarBanderas, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 6.2 Validaciones de negocio posteriores
            var erroresNegocio = EjecutarValidacionesPosterioresCompras(carga.IdCarga, carga.EmpresaRuc, carga.Periodo, todosConsolidados);
            nuevosErrores.AddRange(erroresNegocio);

            // 7. Refrescar archivo_carga_error
            await _archivoCargaErrorRepo.EliminarPorCargaAsync(carga.IdCarga, cancellationToken);
            if (nuevosErrores.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(nuevosErrores, cancellationToken);
            }

            // 8. Actualizar cabecera archivo_carga
            decimal totalGeneral = todosConsolidados.Sum(c => c.TotalCp);
            decimal totalBi = todosConsolidados.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            decimal totalIgv = todosConsolidados.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);

            carga.ActualizarConteoYMontos(
                todosConsolidados.Count,
                todosConsolidados.Count,
                nuevosErrores.Count,
                totalBi,
                totalIgv,
                totalGeneral);

            await _archivoCargaRepo.ActualizarAsync(carga, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Actualización de compras_match finalizada con éxito. Carga: {IdCarga}, TotalRegs: {Regs}, Obs: {Obs}",
                carga.IdCarga, todosConsolidados.Count, nuevosErrores.Count);

            return new ActualizarComprasMatchResponseDTO
            {
                IdCarga = carga.IdCarga,
                Exito = true,
                Mensaje = "Registros de match de compras actualizados y comprobantes revalidados correctamente.",
                NumRegistros = todosConsolidados.Count,
                NumObservaciones = nuevosErrores.Count,
                TotalBaseImponible = totalBi,
                TotalIgv = totalIgv,
                TotalGeneral = totalGeneral
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar compras_match para carga {IdCarga}", request.IdCarga);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static List<ArchivoCargaError> EjecutarValidacionesPosterioresCompras(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<CompraMatch> comprasMatch)
    {
        var errores = new List<ArchivoCargaError>();
        if (comprasMatch.Count == 0) return errores;

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        for (var i = 0; i < comprasMatch.Count; i++)
        {
            var c = comprasMatch[i];
            var numLinea = c.NumeroLinea;
            var serie = (c.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (c.Numero ?? string.Empty).Trim();
            var fechaEmision = c.FechaEmision;

            // 1. Fecha de Emisión en compras:
            if (fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} posterior al periodo seleccionado '{periodo}'",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: SeveridadError.Error));
            }

            // 2. Validación de Documento del Proveedor (DNI y RUC)
            var tipoDoc = (c.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            var nroDoc = (c.NroDocIdentidad ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: 8 dígitos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene documento '{nroDoc}' que debe contener 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: 11 dígitos con prefijos 10, 20, 15, 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento RUC inválido: El proveedor del comprobante Serie '{serie}', Número '{numero}' tiene RUC '{nroDoc}' que debe contener 11 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                    else
                    {
                        var prefijo = nroDoc.Substring(0, 2);
                        if (prefijo != "10" && prefijo != "20" && prefijo != "15" && prefijo != "17")
                        {
                            errores.Add(ArchivoCargaError.Crear(
                                idCarga,
                                numLinea,
                                TipoErrorCarga.Negocio,
                                $"Documento RUC con prefijo inválido: El RUC '{nroDoc}' del comprobante Serie '{serie}', Número '{numero}' debe iniciar con 10, 20, 15 o 17.",
                                campoError: "nro_doc_identidad",
                                valorLectura: nroDoc,
                                severidad: SeveridadError.Advertencia));
                        }
                    }
                }
            }

            // 3. Validación de Razón Social obligatoria
            if (string.IsNullOrWhiteSpace(c.RazonSocial) || c.RazonSocial == "-")
            {
                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numLinea,
                    TipoErrorCarga.Negocio,
                    $"La Razón Social del proveedor en el comprobante Serie '{serie}', Número '{numero}' es obligatoria.",
                    campoError: "razon_social",
                    valorLectura: string.Empty,
                    severidad: SeveridadError.Advertencia));
            }
        }

        return errores;
    }

    private static string NormalizarNumeroComprobante(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return string.Empty;

        var limpio = numero.Trim();
        if (limpio.All(char.IsDigit))
        {
            var sinCeros = limpio.TrimStart('0');
            return string.IsNullOrEmpty(sinCeros) ? "0" : sinCeros;
        }

        return limpio.ToUpperInvariant();
    }
}
