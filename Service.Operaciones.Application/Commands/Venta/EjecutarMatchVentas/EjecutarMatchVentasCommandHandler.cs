using MediatR;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using System.Globalization;

namespace Service.Operaciones.Application.Commands.Venta.EjecutarMatchVentas;

public class EjecutarMatchVentasCommandHandler : IRequestHandler<EjecutarMatchVentasCommand, EjecutarMatchVentasResponseDTO>
{
    private readonly IArchivoCargaRepository _archivoCargaRepo;
    private readonly IArchivoCargaErrorRepository _archivoCargaErrorRepo;
    private readonly IVentaRepository _ventaRepo;
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;
    private readonly IVentaMatchRepository _ventaMatchRepo;
    private readonly IEmpresaService _empresaService;
    private readonly IAccesoEmpresaValidator _accesoValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EjecutarMatchVentasCommandHandler> _logger;

    public EjecutarMatchVentasCommandHandler(
        IArchivoCargaRepository archivoCargaRepo,
        IArchivoCargaErrorRepository archivoCargaErrorRepo,
        IVentaRepository ventaRepo,
        IVentaEmpresaRepository ventaEmpresaRepo,
        IVentaMatchRepository ventaMatchRepo,
        IEmpresaService empresaService,
        IAccesoEmpresaValidator accesoValidator,
        IUnitOfWork unitOfWork,
        ILogger<EjecutarMatchVentasCommandHandler> logger)
    {
        _archivoCargaRepo = archivoCargaRepo;
        _archivoCargaErrorRepo = archivoCargaErrorRepo;
        _ventaRepo = ventaRepo;
        _ventaEmpresaRepo = ventaEmpresaRepo;
        _ventaMatchRepo = ventaMatchRepo;
        _empresaService = empresaService;
        _accesoValidator = accesoValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EjecutarMatchVentasResponseDTO> Handle(EjecutarMatchVentasCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando ejecución de Match de Ventas para Empresa: {Ruc}, Periodo: {Periodo}",
            request.EmpresaRuc, request.Periodo);

        // 1. Validar empresa y permisos
        if (!await _empresaService.ExisteEmpresaAsync(request.EmpresaRuc, cancellationToken))
        {
            throw new ValidationException($"La empresa con RUC {request.EmpresaRuc} no está registrada en el sistema.");
        }
        await _accesoValidator.ValidarAccesoAsync(request.EmpresaRuc, cancellationToken);

        // 2. Obtener cargas vigentes de SIRE y EMPRESA para el periodo
        var cargasSire = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.VentaSire, request.Periodo, 1, 1, cancellationToken);
        var cargaSire = cargasSire.FirstOrDefault();

        var cargasEmpresa = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.VentaEmpresa, request.Periodo, 1, 1, cancellationToken);
        var cargaEmpresa = cargasEmpresa.FirstOrDefault();

        if (cargaSire == null && cargaEmpresa == null)
        {
            throw new ValidationException($"Para realizar el match se requiere que existan datos de ambos orígenes. No se encontró el archivo de Ventas SIRE ni el archivo de Ventas de la Empresa para el periodo {request.Periodo}.");
        }

        if (cargaSire == null)
        {
            throw new ValidationException($"No se encontró la carga de Ventas SIRE para el periodo {request.Periodo}. Debe cargar el archivo SIRE antes de realizar el match.");
        }

        if (cargaEmpresa == null)
        {
            throw new ValidationException($"No se encontró la carga de Ventas de la Empresa para el periodo {request.Periodo}. Debe cargar el archivo de la Empresa antes de realizar el match.");
        }

        // 3. Obtener los comprobantes de ambos orígenes
        var ventasSire = await _ventaRepo.ListarPorCargaAsync(cargaSire.IdCarga, cancellationToken);
        if (ventasSire.Count == 0)
        {
            throw new ValidationException($"El archivo de Ventas SIRE del periodo {request.Periodo} no contiene registros válidos para realizar el match.");
        }

        var ventasEmpresa = await _ventaEmpresaRepo.ListarPorCargaAsync(cargaEmpresa.IdCarga, cancellationToken);
        if (ventasEmpresa.Count == 0)
        {
            throw new ValidationException($"El archivo de Ventas de la Empresa del periodo {request.Periodo} no contiene registros válidos para realizar el match.");
        }

        // 4. Iniciar transacción en UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4.1 Buscar o crear cabecera ArchivoCarga para TipoOperacion.VentaMatch
            var cargasMatchPrevias = await _archivoCargaRepo.ListarAsync(request.EmpresaRuc, TipoOperacion.VentaMatch, request.Periodo, 1, 1, cancellationToken);
            var cargaMatch = cargasMatchPrevias.FirstOrDefault();

            if (cargaMatch == null)
            {
                cargaMatch = ArchivoCarga.Crear(
                    request.EmpresaRuc,
                    request.Periodo,
                    TipoOperacion.VentaMatch,
                    FormatoArchivo.Sistema,
                    $"MATCH_VENTAS_{request.EmpresaRuc}_{request.Periodo}.sys",
                    $"MATCH_{request.EmpresaRuc}_{request.Periodo}_{Guid.NewGuid():N}",
                    request.Usuario
                );
                await _archivoCargaRepo.AgregarAsync(cargaMatch, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Limpiar registros y errores previos del match de este periodo
                await _ventaMatchRepo.EliminarPorCargaFisicoAsync(cargaMatch.IdCarga, cancellationToken);
                await _archivoCargaErrorRepo.EliminarPorCargaAsync(cargaMatch.IdCarga, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Algoritmo de Cruce y Match (Serie + Numero Normalizado sin ceros a la izquierda)
            var dictSire = new Dictionary<string, List<Service.Operaciones.Domain.Entities.Venta>>(StringComparer.OrdinalIgnoreCase);
            foreach (var vs in ventasSire)
            {
                var numNorm = NormalizarNumeroComprobante(vs.Numero);
                var key = $"{vs.Serie?.Trim().ToUpper()}|{numNorm}";
                if (!dictSire.ContainsKey(key)) dictSire[key] = new List<Service.Operaciones.Domain.Entities.Venta>();
                dictSire[key].Add(vs);
            }

            var dictEmpresa = new Dictionary<string, List<VentaEmpresa>>(StringComparer.OrdinalIgnoreCase);
            foreach (var ve in ventasEmpresa)
            {
                var numNorm = NormalizarNumeroComprobante(ve.Numero);
                var key = $"{ve.Serie?.Trim().ToUpper()}|{numNorm}";
                if (!dictEmpresa.ContainsKey(key)) dictEmpresa[key] = new List<VentaEmpresa>();
                dictEmpresa[key].Add(ve);
            }

            var todasLasClaves = dictSire.Keys.Union(dictEmpresa.Keys, StringComparer.OrdinalIgnoreCase).ToList();

            var listaMatch = new List<VentaMatch>();
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

                    // Ambos existen: Validar los 5 campos
                    var diffs = new List<string>();

                    if (s.FechaEmision.Date != e.FechaEmision.Date)
                    {
                        diffs.Add("No coincide la Fecha de Emisión");
                    }

                    if ((s.CodigoTipoCp ?? string.Empty).Trim().PadLeft(2, '0') != (e.CodigoTipoCp ?? string.Empty).Trim().PadLeft(2, '0'))
                    {
                        diffs.Add("No coincide el Tipo de Comprobante");
                    }

                    if ((s.CodigoTipoDocIdentidad ?? string.Empty).Trim() != (e.CodigoTipoDocIdentidad ?? string.Empty).Trim())
                    {
                        diffs.Add("No coincide el Tipo de Documento de Identidad");
                    }

                    if ((s.NroDocIdentidad ?? string.Empty).Trim() != (e.NroDocIdentidad ?? string.Empty).Trim())
                    {
                        diffs.Add("No coincide el RUC / Documento del Cliente");
                    }

                    if (s.TotalCp != e.TotalCp)
                    {
                        diffs.Add("No coincide el Total CP");
                    }

                    if (diffs.Count == 0)
                    {
                        // CASO 1: Coincidencia Exacta (Caso Ideal) -> Solo se guarda SIRE con número normalizado
                        conteoExactas++;
                        var vm = VentaMatch.Crear(
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
                            fechaVencimiento: s.FechaVctoPago,
                            numeroFinal: s.NumeroFinal,
                            valorFacturadoExportacion: s.ValorFactExp,
                            biGravada: s.BiGravada,
                            descuentoBi: s.DsctoBi,
                            igvIpm: s.IgvIpm,
                            descuentoIgv: s.DsctoIgvIpm,
                            montoExonerado: s.MontoExonerado,
                            montoInafecto: s.MontoInafecto,
                            montoIsc: s.Isc,
                            biGravadaIvap: s.BiGravIvap,
                            montoIvap: s.Ivap,
                            montoIcbper: s.Icbper,
                            montoOtrosTributos: s.OtrosTributos,
                            codigoMoneda: s.CodigoMoneda,
                            tipoCambio: s.TipoCambio,
                            fechaEmisionDocModificado: s.FechaEmisionDocModif,
                            codigoTipoCpModificado: s.TipoCpModificado,
                            serieCpModificado: s.SerieCpModificado,
                            numeroCpModificado: s.NroCpModificado,
                            codigoEstadoComprobante: s.CodigoEstadoComprobante,
                            codigoTipoNota: s.CodigoTipoNota,
                            tipoOperacion: s.TipoOperacion,
                            camposLibres: s.CamposLibres
                        );
                        listaMatch.Add(vm);
                    }
                    else
                    {
                        // CASO 2: Coincidencia con diferencias (Caso Erróneo) -> Se guardan AMBOS consecutivamente con número normalizado
                        conteoDiferencias++;

                        var lineaSire = contadorLinea++;
                        var vmSire = VentaMatch.Crear(
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
                            codigoTipoDocIdentidad: s.CodigoTipoDocIdentidad,
                            nroDocIdentidad: s.NroDocIdentidad,
                            razonSocial: s.RazonSocial,
                            totalCp: s.TotalCp,
                            usuarioCreacion: request.Usuario,
                            carSunat: s.CarSunat,
                            fechaVencimiento: s.FechaVctoPago,
                            numeroFinal: s.NumeroFinal,
                            valorFacturadoExportacion: s.ValorFactExp,
                            biGravada: s.BiGravada,
                            descuentoBi: s.DsctoBi,
                            igvIpm: s.IgvIpm,
                            descuentoIgv: s.DsctoIgvIpm,
                            montoExonerado: s.MontoExonerado,
                            montoInafecto: s.MontoInafecto,
                            montoIsc: s.Isc,
                            biGravadaIvap: s.BiGravIvap,
                            montoIvap: s.Ivap,
                            montoIcbper: s.Icbper,
                            montoOtrosTributos: s.OtrosTributos,
                            codigoMoneda: s.CodigoMoneda,
                            tipoCambio: s.TipoCambio,
                            fechaEmisionDocModificado: s.FechaEmisionDocModif,
                            codigoTipoCpModificado: s.TipoCpModificado,
                            serieCpModificado: s.SerieCpModificado,
                            numeroCpModificado: s.NroCpModificado,
                            codigoEstadoComprobante: s.CodigoEstadoComprobante,
                            codigoTipoNota: s.CodigoTipoNota,
                            tipoOperacion: s.TipoOperacion,
                            camposLibres: s.CamposLibres
                        );
                        listaMatch.Add(vmSire);

                        var lineaEmpresa = contadorLinea++;
                        var vmEmpresa = VentaMatch.Crear(
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
                            codigoTipoDocIdentidad: e.CodigoTipoDocIdentidad,
                            nroDocIdentidad: e.NroDocIdentidad,
                            razonSocial: !string.IsNullOrWhiteSpace(e.RazonSocial) ? e.RazonSocial : s.RazonSocial,
                            totalCp: e.TotalCp,
                            usuarioCreacion: request.Usuario,
                            carSunat: e.CarSunat,
                            fechaVencimiento: e.FechaVencimiento,
                            numeroFinal: e.NumeroFinal,
                            valorFacturadoExportacion: e.ValorFacturadoExportacion,
                            biGravada: e.BiGravada,
                            descuentoBi: e.DescuentoBi,
                            igvIpm: e.IgvIpm,
                            descuentoIgv: e.DescuentoIgv,
                            montoExonerado: e.MontoExonerado,
                            montoInafecto: e.MontoInafecto,
                            montoIsc: e.MontoIsc,
                            biGravadaIvap: e.BiGravadaIvap,
                            montoIvap: e.MontoIvap,
                            montoIcbper: e.MontoIcbper,
                            montoOtrosTributos: e.MontoOtrosTributos,
                            codigoMoneda: e.CodigoMoneda,
                            tipoCambio: e.TipoCambio,
                            fechaEmisionDocModificado: e.FechaEmisionDocModificado,
                            codigoTipoCpModificado: e.CodigoTipoCpModificado,
                            serieCpModificado: e.SerieCpModificado,
                            numeroCpModificado: e.NumeroCpModificado,
                            codigoEstadoComprobante: e.CodigoEstadoComprobante,
                            codigoTipoNota: e.CodigoTipoNota,
                            tipoOperacion: e.TipoOperacion,
                            camposLibres: e.CamposLibres
                        );
                        listaMatch.Add(vmEmpresa);

                        // Registrar cada diferencia puntual en archivo_carga_error
                        foreach (var d in diffs)
                        {
                            erroresMatch.Add(ArchivoCargaError.Crear(
                                idCarga: cargaMatch.IdCarga,
                                numeroLinea: lineaSire,
                                tipoError: TipoErrorCarga.Validacion,
                                mensaje: $"{d} en comprobante {s.Serie?.Trim().ToUpper()}-{numeroNormalizado}",
                                campoError: "match_discrepancia",
                                valorLectura: $"{s.Serie?.Trim().ToUpper()}-{numeroNormalizado}",
                                severidad: SeveridadError.Error
                            ));
                        }
                    }
                }
                else if (s != null)
                {
                    // CASO 3: Solo existe en SIRE (Caso Ideal Parcial) con número normalizado
                    conteoSoloUnOrigen++;
                    var numeroNormalizado = NormalizarNumeroComprobante(s.Numero);
                    var vm = VentaMatch.Crear(
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
                        fechaVencimiento: s.FechaVctoPago,
                        numeroFinal: s.NumeroFinal,
                        valorFacturadoExportacion: s.ValorFactExp,
                        biGravada: s.BiGravada,
                        descuentoBi: s.DsctoBi,
                        igvIpm: s.IgvIpm,
                        descuentoIgv: s.DsctoIgvIpm,
                        montoExonerado: s.MontoExonerado,
                        montoInafecto: s.MontoInafecto,
                        montoIsc: s.Isc,
                        biGravadaIvap: s.BiGravIvap,
                        montoIvap: s.Ivap,
                        montoIcbper: s.Icbper,
                        montoOtrosTributos: s.OtrosTributos,
                        codigoMoneda: s.CodigoMoneda,
                        tipoCambio: s.TipoCambio,
                        fechaEmisionDocModificado: s.FechaEmisionDocModif,
                        codigoTipoCpModificado: s.TipoCpModificado,
                        serieCpModificado: s.SerieCpModificado,
                        numeroCpModificado: s.NroCpModificado,
                        codigoEstadoComprobante: s.CodigoEstadoComprobante,
                        codigoTipoNota: s.CodigoTipoNota,
                        tipoOperacion: s.TipoOperacion,
                        camposLibres: s.CamposLibres
                    );
                    listaMatch.Add(vm);
                }
                else if (e != null)
                {
                    // CASO 3: Solo existe en EMPRESA (Caso Ideal Parcial) con número normalizado
                    conteoSoloUnOrigen++;
                    var numeroNormalizado = NormalizarNumeroComprobante(e.Numero);
                    var vm = VentaMatch.Crear(
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
                        codigoTipoDocIdentidad: e.CodigoTipoDocIdentidad,
                        nroDocIdentidad: e.NroDocIdentidad,
                        razonSocial: !string.IsNullOrWhiteSpace(e.RazonSocial) ? e.RazonSocial : "-",
                        totalCp: e.TotalCp,
                        usuarioCreacion: request.Usuario,
                        carSunat: e.CarSunat,
                        fechaVencimiento: e.FechaVencimiento,
                        numeroFinal: e.NumeroFinal,
                        valorFacturadoExportacion: e.ValorFacturadoExportacion,
                        biGravada: e.BiGravada,
                        descuentoBi: e.DescuentoBi,
                        igvIpm: e.IgvIpm,
                        descuentoIgv: e.DescuentoIgv,
                        montoExonerado: e.MontoExonerado,
                        montoInafecto: e.MontoInafecto,
                        montoIsc: e.MontoIsc,
                        biGravadaIvap: e.BiGravadaIvap,
                        montoIvap: e.MontoIvap,
                        montoIcbper: e.MontoIcbper,
                        montoOtrosTributos: e.MontoOtrosTributos,
                        codigoMoneda: e.CodigoMoneda,
                        tipoCambio: e.TipoCambio,
                        fechaEmisionDocModificado: e.FechaEmisionDocModificado,
                        codigoTipoCpModificado: e.CodigoTipoCpModificado,
                        serieCpModificado: e.SerieCpModificado,
                        numeroCpModificado: e.NumeroCpModificado,
                        codigoEstadoComprobante: e.CodigoEstadoComprobante,
                        codigoTipoNota: e.CodigoTipoNota,
                        tipoOperacion: e.TipoOperacion,
                        camposLibres: e.CamposLibres
                    );
                    listaMatch.Add(vm);
                }
            }

            // 6. Ejecutar las Validaciones de Negocio Posteriores sobre el resultado consolidado (mismas de Empresa)
            var erroresNegocioPosteriores = await EjecutarValidacionesPosterioresAsync(
                cargaMatch.IdCarga, request.EmpresaRuc, request.Periodo, listaMatch, cancellationToken);

            erroresMatch.AddRange(erroresNegocioPosteriores);

            // 7. Persistir en Base de Datos
            if (listaMatch.Count > 0)
            {
                await _ventaMatchRepo.AgregarRangoAsync(listaMatch, cancellationToken);
            }

            if (erroresMatch.Count > 0)
            {
                await _archivoCargaErrorRepo.AgregarRangoAsync(erroresMatch, cancellationToken);
            }

            decimal totalGeneral = listaMatch.Sum(v => v.TotalCp);
            decimal totalBi = listaMatch.Sum(v => v.BiGravada);
            decimal totalIgv = listaMatch.Sum(v => v.IgvIpm);

            cargaMatch.ActualizarConteoYMontos(
                listaMatch.Count,
                listaMatch.Count,
                erroresMatch.Count,
                totalBi,
                totalIgv,
                totalGeneral);

            await _archivoCargaRepo.ActualizarAsync(cargaMatch, cancellationToken);

            // 8. Guardar cambios y confirmar transacción atómicamente
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Match de Ventas finalizado con éxito. IdCarga: {IdCarga}, Exactas: {Exactas}, Diffs: {Diffs}, SoloUnOrigen: {Solo}, TotalObs: {Obs}",
                cargaMatch.IdCarga, conteoExactas, conteoDiferencias, conteoSoloUnOrigen, erroresMatch.Count);

            return new EjecutarMatchVentasResponseDTO
            {
                IdCarga = cargaMatch.IdCarga,
                EmpresaRuc = request.EmpresaRuc,
                Periodo = request.Periodo,
                TotalRegistrosSire = ventasSire.Count,
                TotalRegistrosEmpresa = ventasEmpresa.Count,
                TotalConsolidado = listaMatch.Count,
                CoincidenciasExactas = conteoExactas,
                Diferencias = conteoDiferencias,
                SoloUnOrigen = conteoSoloUnOrigen,
                TotalObservaciones = erroresMatch.Count,
                TotalGeneral = totalGeneral,
                Mensaje = "Proceso de Match de Ventas ejecutado y validado correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico durante la ejecución de Match de Ventas para Empresa {Ruc}, Periodo {Periodo}",
                request.EmpresaRuc, request.Periodo);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<ArchivoCargaError>> EjecutarValidacionesPosterioresAsync(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        List<VentaMatch> ventasMatch,
        CancellationToken cancellationToken)
    {
        var errores = new List<ArchivoCargaError>();
        if (ventasMatch.Count == 0) return errores;

        var anioPeriodo = int.Parse(periodo[..4]);
        var mesPeriodo = int.Parse(periodo[4..]);
        var primerDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, 1);
        var ultimoDiaPeriodo = new DateTime(anioPeriodo, mesPeriodo, DateTime.DaysInMonth(anioPeriodo, mesPeriodo));

        for (var i = 0; i < ventasMatch.Count; i++)
        {
            var v = ventasMatch[i];
            var numLinea = v.NumeroLinea;
            var serie = (v.Serie ?? string.Empty).Trim().ToUpper();
            var numero = (v.Numero ?? string.Empty).Trim();
            var fechaEmision = v.FechaEmision;

            // 1. Fecha de emisión dentro del periodo
            if (fechaEmision.Date < primerDiaPeriodo.Date || fechaEmision.Date > ultimoDiaPeriodo.Date)
            {
                var periodoFecha = $"{fechaEmision.Year}{fechaEmision.Month:D2}";
                var esFechaSuperior = string.Compare(periodoFecha, periodo, StringComparison.Ordinal) > 0;
                var sevFecha = esFechaSuperior ? SeveridadError.Error : SeveridadError.Advertencia;

                errores.Add(ArchivoCargaError.Crear(
                    idCarga,
                    numLinea,
                    TipoErrorCarga.Negocio,
                    $"Fecha fuera de periodo: El comprobante Serie '{serie}', Número '{numero}' tiene fecha de emisión {fechaEmision:dd/MM/yyyy} correspondiente al periodo '{periodoFecha}' (el periodo seleccionado es '{periodo}')",
                    campoError: "fecha_emision",
                    valorLectura: fechaEmision.ToString("dd/MM/yyyy"),
                    severidad: sevFecha));
            }

            // 2. Documento de Identidad (DNI / RUC)
            var tipoDoc = (v.CodigoTipoDocIdentidad ?? string.Empty).Trim();
            var nroDoc = (v.NroDocIdentidad ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(tipoDoc) || !string.IsNullOrWhiteSpace(nroDoc))
            {
                if (tipoDoc == "1") // DNI: 8 dígitos numéricos
                {
                    if (nroDoc.Length != 8 || !nroDoc.All(char.IsDigit))
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento DNI inválido: El comprobante Serie '{serie}', Número '{numero}' tiene el documento '{nroDoc}' que debe contener exactamente 8 dígitos numéricos.",
                            campoError: "nro_doc_identidad",
                            valorLectura: nroDoc,
                            severidad: SeveridadError.Advertencia));
                    }
                }
                else if (tipoDoc == "6") // RUC: 11 dígitos numéricos que inicien con 10, 20, 15 o 17
                {
                    var rucValido = nroDoc.Length == 11 && nroDoc.All(char.IsDigit);
                    if (!rucValido)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            numLinea,
                            TipoErrorCarga.Formato,
                            $"Documento RUC inválido: El comprobante Serie '{serie}', Número '{numero}' tiene el RUC '{nroDoc}' que debe contener exactamente 11 dígitos numéricos.",
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
        }

        // 3. Correlatividad interna de series
        var porSerie = ventasMatch
            .GroupBy(v => new
            {
                TipoCp = (v.CodigoTipoCp ?? string.Empty).Trim().PadLeft(2, '0'),
                Serie = (v.Serie ?? string.Empty).Trim().ToUpper()
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.Serie));

        foreach (var grupo in porSerie)
        {
            var numeros = grupo
                .Select(v => long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (numeros.Count < 2) continue;

            for (var i = 1; i < numeros.Count; i++)
            {
                var anterior = numeros[i - 1];
                var actual = numeros[i];
                var diferencia = actual - anterior;

                if (diferencia == 2)
                {
                    var faltante = anterior + 1;
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga,
                        1,
                        TipoErrorCarga.Secuencia,
                        $"Salto de correlatividad: No se encontró el registro para el número correlativo '{faltante}' de la Serie '{grupo.Key.Serie}'",
                        campoError: "numero",
                        valorLectura: faltante.ToString(),
                        severidad: SeveridadError.Error));
                }
                else if (diferencia > 2)
                {
                    var desde = anterior + 1;
                    var hasta = actual - 1;
                    errores.Add(ArchivoCargaError.Crear(
                        idCarga,
                        1,
                        TipoErrorCarga.Secuencia,
                        $"Salto múltiple de correlatividad: No se encontraron los registros de los números correlativos desde '{desde}' hasta '{hasta}' de la Serie '{grupo.Key.Serie}'",
                        campoError: "numero",
                        valorLectura: $"{desde}-{hasta}",
                        severidad: SeveridadError.Error));
                }
            }
        }

        // 4. Correlatividad respecto al periodo anterior
        var periodoAnterior = ObtenerPeriodoAnterior(periodo);
        foreach (var grupo in porSerie)
        {
            var numerosCargados = grupo
                .Select(v => long.TryParse((v.Numero ?? string.Empty).Trim(), out var n) ? (long?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();

            if (numerosCargados.Count == 0) continue;

            var menorCargado = numerosCargados.First();

            var numerosPeriodoAnterior = await _ventaEmpresaRepo.ObtenerNumerosPorSerieYPeriodoAsync(
                empresaRuc, periodoAnterior, grupo.Key.TipoCp, grupo.Key.Serie, cancellationToken);

            if (numerosPeriodoAnterior != null && numerosPeriodoAnterior.Count > 0)
            {
                var mayoresPeriodoAnterior = numerosPeriodoAnterior
                    .Select(numStr => long.TryParse(numStr?.Trim(), out var n) ? (long?)n : null)
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .OrderByDescending(n => n)
                    .ToList();

                if (mayoresPeriodoAnterior.Count > 0)
                {
                    var mayorAnterior = mayoresPeriodoAnterior.First();
                    var esperado = mayorAnterior + 1;

                    if (menorCargado != esperado)
                    {
                        errores.Add(ArchivoCargaError.Crear(
                            idCarga,
                            1,
                            TipoErrorCarga.Secuencia,
                            $"Discontinuidad con periodo anterior: El número inicial de la Serie '{grupo.Key.Serie}' es '{menorCargado}', pero el último número del periodo anterior ({periodoAnterior}) fue '{mayorAnterior}' (se esperaba '{esperado}')",
                            campoError: "numero",
                            valorLectura: menorCargado.ToString(),
                            severidad: SeveridadError.Advertencia));
                    }
                }
            }
        }

        return errores;
    }

    private static string ObtenerPeriodoAnterior(string periodoActual)
    {
        var anio = int.Parse(periodoActual[..4]);
        var mes = int.Parse(periodoActual[4..]);

        if (mes == 1)
        {
            anio--;
            mes = 12;
        }
        else
        {
            mes--;
        }

        return $"{anio}{mes:D2}";
    }

    private static string NormalizarNumeroComprobante(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return string.Empty;

        var limpio = numero.Trim();

        // Si es puramente numérico, remover todos los ceros a la izquierda
        if (limpio.All(char.IsDigit))
        {
            var sinCeros = limpio.TrimStart('0');
            return string.IsNullOrEmpty(sinCeros) ? "0" : sinCeros;
        }

        return limpio.ToUpperInvariant();
    }
}
