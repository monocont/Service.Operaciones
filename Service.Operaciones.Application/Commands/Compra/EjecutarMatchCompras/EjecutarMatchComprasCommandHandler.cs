using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Commands.Compra.EjecutarMatchCompras;

public class EjecutarMatchComprasCommandHandler : IRequestHandler<EjecutarMatchComprasCommand, EjecutarMatchComprasResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly ICompraSireRepository _compraSireRepo;
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;
    private readonly ICompraMatchRepository _compraMatchRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EjecutarMatchComprasCommandHandler> _logger;

    public EjecutarMatchComprasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        ICompraSireRepository compraSireRepo,
        ICompraEmpresaRepository compraEmpresaRepo,
        ICompraMatchRepository compraMatchRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IUnitOfWork unitOfWork,
        ILogger<EjecutarMatchComprasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _compraSireRepo = compraSireRepo;
        _compraEmpresaRepo = compraEmpresaRepo;
        _compraMatchRepo = compraMatchRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EjecutarMatchComprasResponseDTO> Handle(EjecutarMatchComprasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando ejecución de Match de Compras para Empresa: {Ruc}, Periodo: {Periodo}",
            request.EmpresaRuc, request.Periodo);

        // 1. Validar empresa y permisos
        if (!await _empresaService.ExisteEmpresaAsync(request.EmpresaRuc, cancellationToken))
        {
            throw new ValidationException($"La empresa con RUC {request.EmpresaRuc} no está registrada en el sistema.");
        }
        await _accesoValidator.ValidarAccesoAsync(request.EmpresaRuc, cancellationToken);

        // 2. Obtener cargas vigentes de SIRE y EMPRESA para el periodo
        var cargasSire = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.CompraSire, request.Periodo, 1, 1, cancellationToken);
        var cargaSire = cargasSire.FirstOrDefault();

        var cargasEmpresa = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.CompraEmpresa, request.Periodo, 1, 1, cancellationToken);
        var cargaEmpresa = cargasEmpresa.FirstOrDefault();

        if (cargaSire == null && cargaEmpresa == null)
        {
            throw new ValidationException($"Para realizar el match se requiere que existan datos de ambos orígenes. No se encontró el archivo de Compras SIRE ni el archivo de Compras de la Empresa para el periodo {request.Periodo}.");
        }

        if (cargaSire == null)
        {
            throw new ValidationException($"No se encontró la carga de Compras SIRE para el periodo {request.Periodo}. Debe cargar el archivo SIRE antes de realizar el match.");
        }

        if (cargaEmpresa == null)
        {
            throw new ValidationException($"No se encontró la carga de Compras de la Empresa para el periodo {request.Periodo}. Debe cargar el archivo de la Empresa antes de realizar el match.");
        }

        // 3. Obtener los comprobantes de ambos orígenes
        var comprasSire = await _compraSireRepo.ListarPorCargaAsync(cargaSire.IdCarga, cancellationToken);
        if (comprasSire.Count == 0)
        {
            throw new ValidationException($"El archivo de Compras SIRE del periodo {request.Periodo} no contiene registros válidos para realizar el match.");
        }

        var comprasEmpresa = await _compraEmpresaRepo.ListarPorCargaAsync(cargaEmpresa.IdCarga, cancellationToken);
        if (comprasEmpresa.Count == 0)
        {
            throw new ValidationException($"El archivo de Compras de la Empresa del periodo {request.Periodo} no contiene registros válidos para realizar el match.");
        }

        // 4. Iniciar transacción en UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4.1 Buscar o crear cabecera ArchivoCarga para TipoOperacion.CompraMatch
            var cargasMatchPrevias = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.CompraMatch, request.Periodo, 1, 1, cancellationToken);
            var cargaMatch = cargasMatchPrevias.FirstOrDefault();

            if (cargaMatch == null)
            {
                cargaMatch = ArchivoCarga.Crear(
                    request.EmpresaRuc,
                    request.Periodo,
                    TipoOperacion.CompraMatch,
                    FormatoArchivo.Sistema,
                    $"MATCH_COMPRAS_{request.EmpresaRuc}_{request.Periodo}.sys",
                    $"MATCH_COMPRAS_{request.EmpresaRuc}_{request.Periodo}_{Guid.NewGuid():N}",
                    request.Usuario
                );
                await _archivoCargaRepo.AgregarAsync(cargaMatch, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Limpiar registros y errores previos del match de este periodo
                await _compraMatchRepo.EliminarPorCargaFisicoAsync(cargaMatch.IdCarga, cancellationToken);
                await _archivoCargaErrorRepo.EliminarPorCargaAsync(cargaMatch.IdCarga, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Algoritmo de Cruce y Match de Compras (RUC Proveedor + Tipo CP + Serie + Numero Normalizado)
            var dictSire = new Dictionary<string, List<CompraSire>>(StringComparer.OrdinalIgnoreCase);
            foreach (var cs in comprasSire)
            {
                var key = ConstruirClaveCruce(cs.NroDocIdentidad, cs.CodigoTipoCp, cs.Serie, cs.Numero);
                if (!dictSire.ContainsKey(key)) dictSire[key] = new List<CompraSire>();
                dictSire[key].Add(cs);
            }

            var dictEmpresa = new Dictionary<string, List<CompraEmpresa>>(StringComparer.OrdinalIgnoreCase);
            foreach (var ce in comprasEmpresa)
            {
                var key = ConstruirClaveCruce(ce.NroDocIdentidad, ce.CodigoTipoCp, ce.Serie, ce.Numero);
                if (!dictEmpresa.ContainsKey(key)) dictEmpresa[key] = new List<CompraEmpresa>();
                dictEmpresa[key].Add(ce);
            }

            var todasLasClaves = dictSire.Keys.Union(dictEmpresa.Keys, StringComparer.OrdinalIgnoreCase).ToList();

            var listaMatch = new List<CompraMatch>();
            var erroresMatch = new List<ArchivoCargaError>();

            int contadorLinea = 1;
            int conteoExactas = 0;
            int conteoDiferencias = 0;
            int conteoSoloUnOrigen = 0;

            foreach (var key in todasLasClaves)
            {
                dictSire.TryGetValue(key, out var listaS);
                dictEmpresa.TryGetValue(key, out var listaE);

                var s = listaS?.FirstOrDefault();
                var e = listaE?.FirstOrDefault();

                if (s != null && e != null)
                {
                    var numeroNormalizado = NormalizarNumeroComprobante(s.Numero ?? e.Numero);

                    // Ambos existen: Validar campos críticos de compras
                    var diffs = new List<string>();

                    if (s.FechaEmision.Date != e.FechaEmision.Date)
                    {
                        diffs.Add("No coincide la Fecha de Emisión");
                    }

                    if ((s.CodigoTipoDocIdentidad ?? string.Empty).Trim() != (e.CodigoTipoDocIdentidad ?? string.Empty).Trim())
                    {
                        diffs.Add("No coincide el Tipo de Documento de Identidad del Proveedor");
                    }

                    if ((s.NroDocIdentidad ?? string.Empty).Trim() != (e.NroDocIdentidad ?? string.Empty).Trim())
                    {
                        diffs.Add("No coincide el RUC / Documento del Proveedor");
                    }

                    if (s.BiGravadoDg != e.BiGravadoDg)
                    {
                        diffs.Add("No coincide la Base Imponible Gravada (BI Gravado DG)");
                    }

                    if (s.IgvIpmDg != e.IgvIpmDg)
                    {
                        diffs.Add("No coincide el IGV / IPM Gravado (IGV DG)");
                    }

                    if (s.TotalCp != e.TotalCp)
                    {
                        diffs.Add("No coincide el Total CP");
                    }

                    if ((s.CodigoMoneda ?? "PEN").Trim().ToUpper() != (e.CodigoMoneda ?? "PEN").Trim().ToUpper())
                    {
                        diffs.Add("No coincide la Moneda");
                    }

                    if (diffs.Count == 0)
                    {
                        // CASO 1: Coincidencia Exacta -> Se guarda 1 fila con datos SIRE y número normalizado
                        conteoExactas++;
                        var cm = CompraMatch.Crear(
                            idCarga: cargaMatch.IdCarga,
                            empresaRuc: request.EmpresaRuc,
                            periodo: request.Periodo,
                            numeroLinea: contadorLinea++,
                            origenDato: "SIRE",
                            esCoincidenciaExacta: true,
                            esDiferencia: false,
                            esSoloUnOrigen: false,
                            codigoTipoCp: s.CodigoTipoCp,
                            serie: s.Serie?.Trim().ToUpper() ?? string.Empty,
                            numero: numeroNormalizado,
                            fechaEmision: s.FechaEmision,
                            codigoTipoDocIdentidad: s.CodigoTipoDocIdentidad,
                            nroDocIdentidad: s.NroDocIdentidad,
                            razonSocial: s.RazonSocial,
                            totalCp: s.TotalCp,
                            usuarioCreacion: request.Usuario,
                            carSunat: s.CarSunat,
                            fechaVencimiento: s.FechaVencimiento,
                            anioDocumento: s.AnioDocumento,
                            numeroFinal: s.NumeroFinal,
                            biGravadoDg: s.BiGravadoDg,
                            igvIpmDg: s.IgvIpmDg,
                            biGravadoDgng: s.BiGravadoDgng,
                            igvIpmDgng: s.IgvIpmDgng,
                            biGravadoDng: s.BiGravadoDng,
                            igvIpmDng: s.IgvIpmDng,
                            valorAdqNg: s.ValorAdqNg,
                            montoIsc: s.MontoIsc,
                            montoIcbper: s.MontoIcbper,
                            montoOtrosTributos: s.MontoOtrosTributos,
                            codigoMoneda: s.CodigoMoneda ?? "PEN",
                            tipoCambio: s.TipoCambio,
                            fechaEmisionDocModificado: s.FechaEmisionDocModificado,
                            codigoTipoCpModificado: s.CodigoTipoCpModificado,
                            serieCpModificado: s.SerieCpModificado,
                            codDamDsi: s.CodDamDsi,
                            numeroCpModificado: s.NumeroCpModificado,
                            clasifBssSss: s.ClasifBssSss,
                            idProyectoOp: s.IdProyectoOp,
                            porcPart: s.PorcPart,
                            imb: s.Imb,
                            carOrigIndEI: s.CarOrigIndEI,
                            detraccion: s.Detraccion,
                            codigoTipoNota: s.CodigoTipoNota,
                            codigoEstadoComprobante: s.CodigoEstadoComprobante,
                            incal: s.Incal,
                            camposLibres: s.CamposLibres
                        );
                        listaMatch.Add(cm);
                    }
                    else
                    {
                        // CASO 2: Discrepancia -> Se guardan AMBOS consecutivamente
                        conteoDiferencias++;

                        var lineaSire = contadorLinea++;
                        var cmSire = CompraMatch.Crear(
                            idCarga: cargaMatch.IdCarga,
                            empresaRuc: request.EmpresaRuc,
                            periodo: request.Periodo,
                            numeroLinea: lineaSire,
                            origenDato: "SIRE",
                            esCoincidenciaExacta: false,
                            esDiferencia: true,
                            esSoloUnOrigen: false,
                            codigoTipoCp: s.CodigoTipoCp,
                            serie: s.Serie?.Trim().ToUpper() ?? string.Empty,
                            numero: numeroNormalizado,
                            fechaEmision: s.FechaEmision,
                            codigoTipoDocIdentidad: s.CodigoTipoDocIdentidad ?? "6",
                            nroDocIdentidad: s.NroDocIdentidad ?? "-",
                            razonSocial: s.RazonSocial,
                            totalCp: s.TotalCp,
                            usuarioCreacion: request.Usuario,
                            carSunat: s.CarSunat,
                            fechaVencimiento: s.FechaVencimiento,
                            anioDocumento: s.AnioDocumento,
                            numeroFinal: s.NumeroFinal,
                            biGravadoDg: s.BiGravadoDg,
                            igvIpmDg: s.IgvIpmDg,
                            biGravadoDgng: s.BiGravadoDgng,
                            igvIpmDgng: s.IgvIpmDgng,
                            biGravadoDng: s.BiGravadoDng,
                            igvIpmDng: s.IgvIpmDng,
                            valorAdqNg: s.ValorAdqNg,
                            montoIsc: s.MontoIsc,
                            montoIcbper: s.MontoIcbper,
                            montoOtrosTributos: s.MontoOtrosTributos,
                            codigoMoneda: s.CodigoMoneda ?? "PEN",
                            tipoCambio: s.TipoCambio,
                            fechaEmisionDocModificado: s.FechaEmisionDocModificado,
                            codigoTipoCpModificado: s.CodigoTipoCpModificado,
                            serieCpModificado: s.SerieCpModificado,
                            codDamDsi: s.CodDamDsi,
                            numeroCpModificado: s.NumeroCpModificado,
                            clasifBssSss: s.ClasifBssSss,
                            idProyectoOp: s.IdProyectoOp,
                            porcPart: s.PorcPart,
                            imb: s.Imb,
                            carOrigIndEI: s.CarOrigIndEI,
                            detraccion: s.Detraccion,
                            codigoTipoNota: s.CodigoTipoNota,
                            codigoEstadoComprobante: s.CodigoEstadoComprobante,
                            incal: s.Incal,
                            camposLibres: s.CamposLibres
                        );
                        listaMatch.Add(cmSire);

                        var lineaEmpresa = contadorLinea++;
                        var cmEmpresa = CompraMatch.Crear(
                            idCarga: cargaMatch.IdCarga,
                            empresaRuc: request.EmpresaRuc,
                            periodo: request.Periodo,
                            numeroLinea: lineaEmpresa,
                            origenDato: "EMPRESA",
                            esCoincidenciaExacta: false,
                            esDiferencia: true,
                            esSoloUnOrigen: false,
                            codigoTipoCp: e.CodigoTipoCp,
                            serie: e.Serie?.Trim().ToUpper() ?? string.Empty,
                            numero: numeroNormalizado,
                            fechaEmision: e.FechaEmision,
                            codigoTipoDocIdentidad: e.CodigoTipoDocIdentidad ?? "6",
                            nroDocIdentidad: e.NroDocIdentidad ?? "-",
                            razonSocial: !string.IsNullOrWhiteSpace(e.RazonSocial) ? e.RazonSocial : s.RazonSocial,
                            totalCp: e.TotalCp,
                            usuarioCreacion: request.Usuario,
                            carSunat: e.CarSunat,
                            fechaVencimiento: e.FechaVencimiento,
                            anioDocumento: e.AnioDocumento,
                            numeroFinal: e.NumeroFinal,
                            biGravadoDg: e.BiGravadoDg,
                            igvIpmDg: e.IgvIpmDg,
                            biGravadoDgng: e.BiGravadoDgng,
                            igvIpmDgng: e.IgvIpmDgng,
                            biGravadoDng: e.BiGravadoDng,
                            igvIpmDng: e.IgvIpmDng,
                            valorAdqNg: e.ValorAdqNg,
                            montoIsc: e.MontoIsc,
                            montoIcbper: e.MontoIcbper,
                            montoOtrosTributos: e.MontoOtrosTributos,
                            codigoMoneda: e.CodigoMoneda ?? "PEN",
                            tipoCambio: e.TipoCambio,
                            fechaEmisionDocModificado: e.FechaEmisionDocModificado,
                            codigoTipoCpModificado: e.CodigoTipoCpModificado,
                            serieCpModificado: e.SerieCpModificado,
                            codDamDsi: e.CodDamDsi,
                            numeroCpModificado: e.NumeroCpModificado,
                            clasifBssSss: e.ClasifBssSss,
                            idProyectoOp: e.IdProyectoOp,
                            porcPart: e.PorcPart,
                            imb: e.Imb,
                            carOrigIndEI: e.CarOrigIndEI,
                            detraccion: e.Detraccion,
                            codigoTipoNota: e.CodigoTipoNota,
                            codigoEstadoComprobante: e.CodigoEstadoComprobante,
                            incal: e.Incal,
                            camposLibres: e.CamposLibres
                        );
                        listaMatch.Add(cmEmpresa);

                        // Registrar cada discrepancia puntual en archivo_carga_error
                        foreach (var d in diffs)
                        {
                            erroresMatch.Add(ArchivoCargaError.Crear(
                                idCarga: cargaMatch.IdCarga,
                                numeroLinea: lineaSire,
                                tipoError: TipoErrorCarga.Validacion,
                                mensaje: $"{d} en comprobante {s.Serie?.Trim().ToUpper()}-{numeroNormalizado} (Proveedor {s.NroDocIdentidad})",
                                campoError: "match_discrepancia",
                                valorLectura: $"{s.Serie?.Trim().ToUpper()}-{numeroNormalizado}",
                                severidad: SeveridadError.Error
                            ));
                        }
                    }
                }
                else if (s != null)
                {
                    // CASO 3: Solo existe en SIRE
                    conteoSoloUnOrigen++;
                    var numeroNormalizado = NormalizarNumeroComprobante(s.Numero);
                    var cm = CompraMatch.Crear(
                        idCarga: cargaMatch.IdCarga,
                        empresaRuc: request.EmpresaRuc,
                        periodo: request.Periodo,
                        numeroLinea: contadorLinea++,
                        origenDato: "SIRE",
                        esCoincidenciaExacta: false,
                        esDiferencia: false,
                        esSoloUnOrigen: true,
                        codigoTipoCp: s.CodigoTipoCp,
                        serie: s.Serie?.Trim().ToUpper() ?? string.Empty,
                        numero: numeroNormalizado,
                        fechaEmision: s.FechaEmision,
                        codigoTipoDocIdentidad: s.CodigoTipoDocIdentidad,
                        nroDocIdentidad: s.NroDocIdentidad,
                        razonSocial: s.RazonSocial,
                        totalCp: s.TotalCp,
                        usuarioCreacion: request.Usuario,
                        carSunat: s.CarSunat,
                        fechaVencimiento: s.FechaVencimiento,
                        anioDocumento: s.AnioDocumento,
                        numeroFinal: s.NumeroFinal,
                        biGravadoDg: s.BiGravadoDg,
                        igvIpmDg: s.IgvIpmDg,
                        biGravadoDgng: s.BiGravadoDgng,
                        igvIpmDgng: s.IgvIpmDgng,
                        biGravadoDng: s.BiGravadoDng,
                        igvIpmDng: s.IgvIpmDng,
                        valorAdqNg: s.ValorAdqNg,
                        montoIsc: s.MontoIsc,
                        montoIcbper: s.MontoIcbper,
                        montoOtrosTributos: s.MontoOtrosTributos,
                        codigoMoneda: s.CodigoMoneda ?? "PEN",
                        tipoCambio: s.TipoCambio,
                        fechaEmisionDocModificado: s.FechaEmisionDocModificado,
                        codigoTipoCpModificado: s.CodigoTipoCpModificado,
                        serieCpModificado: s.SerieCpModificado,
                        codDamDsi: s.CodDamDsi,
                        numeroCpModificado: s.NumeroCpModificado,
                        clasifBssSss: s.ClasifBssSss,
                        idProyectoOp: s.IdProyectoOp,
                        porcPart: s.PorcPart,
                        imb: s.Imb,
                        carOrigIndEI: s.CarOrigIndEI,
                        detraccion: s.Detraccion,
                        codigoTipoNota: s.CodigoTipoNota,
                        codigoEstadoComprobante: s.CodigoEstadoComprobante,
                        incal: s.Incal,
                        camposLibres: s.CamposLibres
                    );
                    listaMatch.Add(cm);
                }
                else if (e != null)
                {
                    // CASO 4: Solo existe en EMPRESA
                    conteoSoloUnOrigen++;
                    var numeroNormalizado = NormalizarNumeroComprobante(e.Numero);
                    var cm = CompraMatch.Crear(
                        idCarga: cargaMatch.IdCarga,
                        empresaRuc: request.EmpresaRuc,
                        periodo: request.Periodo,
                        numeroLinea: contadorLinea++,
                        origenDato: "EMPRESA",
                        esCoincidenciaExacta: false,
                        esDiferencia: false,
                        esSoloUnOrigen: true,
                        codigoTipoCp: e.CodigoTipoCp,
                        serie: e.Serie?.Trim().ToUpper() ?? string.Empty,
                        numero: numeroNormalizado,
                        fechaEmision: e.FechaEmision,
                        codigoTipoDocIdentidad: e.CodigoTipoDocIdentidad ?? "6",
                        nroDocIdentidad: e.NroDocIdentidad ?? "-",
                        razonSocial: !string.IsNullOrWhiteSpace(e.RazonSocial) ? e.RazonSocial : "-",
                        totalCp: e.TotalCp,
                        usuarioCreacion: request.Usuario,
                        carSunat: e.CarSunat,
                        fechaVencimiento: e.FechaVencimiento,
                        anioDocumento: e.AnioDocumento,
                        numeroFinal: e.NumeroFinal,
                        biGravadoDg: e.BiGravadoDg,
                        igvIpmDg: e.IgvIpmDg,
                        biGravadoDgng: e.BiGravadoDgng,
                        igvIpmDgng: e.IgvIpmDgng,
                        biGravadoDng: e.BiGravadoDng,
                        igvIpmDng: e.IgvIpmDng,
                        valorAdqNg: e.ValorAdqNg,
                        montoIsc: e.MontoIsc,
                        montoIcbper: e.MontoIcbper,
                        montoOtrosTributos: e.MontoOtrosTributos,
                        codigoMoneda: e.CodigoMoneda ?? "PEN",
                        tipoCambio: e.TipoCambio,
                        fechaEmisionDocModificado: e.FechaEmisionDocModificado,
                        codigoTipoCpModificado: e.CodigoTipoCpModificado,
                        serieCpModificado: e.SerieCpModificado,
                        codDamDsi: e.CodDamDsi,
                        numeroCpModificado: e.NumeroCpModificado,
                        clasifBssSss: e.ClasifBssSss,
                        idProyectoOp: e.IdProyectoOp,
                        porcPart: e.PorcPart,
                        imb: e.Imb,
                        carOrigIndEI: e.CarOrigIndEI,
                        detraccion: e.Detraccion,
                        codigoTipoNota: e.CodigoTipoNota,
                        codigoEstadoComprobante: e.CodigoEstadoComprobante,
                        incal: e.Incal,
                        camposLibres: e.CamposLibres
                    );
                    listaMatch.Add(cm);
                }
            }

            // 6. Ejecutar Validaciones de Negocio Posteriores sobre Compras
            var erroresNegocioPosteriores = EjecutarValidacionesPosterioresCompras(
                cargaMatch.IdCarga, request.EmpresaRuc, request.Periodo, listaMatch);

            erroresMatch.AddRange(erroresNegocioPosteriores);

            // 7. Persistir en Base de Datos
            if (listaMatch.Count > 0)
            {
                await _compraMatchRepo.AgregarRangoAsync(listaMatch, cancellationToken);
            }

            if (erroresMatch.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresMatch, cancellationToken);
            }

            decimal totalGeneral = listaMatch.Sum(c => c.TotalCp);
            decimal totalBi = listaMatch.Sum(c => c.BiGravadoDg + c.BiGravadoDgng + c.BiGravadoDng);
            decimal totalIgv = listaMatch.Sum(c => c.IgvIpmDg + c.IgvIpmDgng + c.IgvIpmDng);

            cargaMatch.ActualizarConteoYMontos(
                listaMatch.Count,
                listaMatch.Count,
                erroresMatch.Count,
                totalBi,
                totalIgv,
                totalGeneral);

            await _archivoCargaRepo.ActualizarAsync(cargaMatch, cancellationToken);

            // 8. Confirmar transacción atómicamente
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Match de Compras finalizado con éxito. IdCarga: {IdCarga}, Exactas: {Exactas}, Diffs: {Diffs}, SoloUnOrigen: {Solo}, TotalObs: {Obs}",
                cargaMatch.IdCarga, conteoExactas, conteoDiferencias, conteoSoloUnOrigen, erroresMatch.Count);

            return new EjecutarMatchComprasResponseDTO
            {
                IdCarga = cargaMatch.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TotalRegistrosSire = comprasSire.Count,
                TotalRegistrosEmpresa = comprasEmpresa.Count,
                TotalConsolidado = listaMatch.Count,
                CoincidenciasExactas = conteoExactas,
                Diferencias = conteoDiferencias,
                SoloUnOrigen = conteoSoloUnOrigen,
                TotalObservaciones = erroresMatch.Count,
                TotalBaseImponible = totalBi,
                TotalIgv = totalIgv,
                TotalGeneral = totalGeneral,
                Mensaje = "Proceso de Match de Compras ejecutado y validado correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico durante la ejecución de Match de Compras para Empresa {Ruc}, Periodo {Periodo}",
                request.EmpresaRuc, request.Periodo);
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
            // En compras está permitido registrar comprobantes de periodos anteriores. Solo es error si la fecha es posterior al periodo.
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

    private static string ConstruirClaveCruce(string? nroDocProveedor, string? tipoCp, string? serie, string? numero)
    {
        var doc = (nroDocProveedor ?? string.Empty).Trim();
        var tipo = (tipoCp ?? string.Empty).Trim().PadLeft(2, '0');
        var ser = (serie ?? string.Empty).Trim().ToUpperInvariant();
        var num = NormalizarNumeroComprobante(numero);

        return $"{doc}|{tipo}|{ser}|{num}";
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
