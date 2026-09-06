using MediatR;
using Service.Operaciones.Application.DTOs.LimitesTributarios;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;
using Service.Operaciones.Domain.Services;

namespace Service.Operaciones.Application.Queries.LimitesTributarios;

public class ObtenerLimitesTributariosEmpresasQueryHandler : IRequestHandler<ObtenerLimitesTributariosEmpresasQuery, LimitesTributariosEmpresasResponseDTO>
{
    private readonly IEmpresaService _empresaService;
    private readonly IArchivoCargaRepository _archivoCargaRepo;

    public ObtenerLimitesTributariosEmpresasQueryHandler(
        IEmpresaService empresaService,
        IArchivoCargaRepository archivoCargaRepo)
    {
        _empresaService = empresaService;
        _archivoCargaRepo = archivoCargaRepo;
    }

    public async Task<LimitesTributariosEmpresasResponseDTO> Handle(ObtenerLimitesTributariosEmpresasQuery request, CancellationToken cancellationToken)
    {
        var anio = request.Anio > 0 ? request.Anio : DateTime.UtcNow.Year;

        // 1. Obtener empresas del usuario y sus límites de régimen configurados para el año
        var empresas = await _empresaService.ObtenerEmpresasConLimitesAsync(anio, cancellationToken);
        if (empresas == null || empresas.Count == 0)
        {
            return new LimitesTributariosEmpresasResponseDTO
            {
                Anio = anio,
                ValorUitReferencia = 0,
                TotalEmpresas = 0,
                TotalEmpresasEnRiesgoCritico = 0,
                TotalEmpresasEnAlerta = 0,
                TotalEmpresasNormales = 0,
                Empresas = new List<EmpresaLimiteItemDTO>()
            };
        }

        // 2. Obtener cargas de todas las empresas para el año en lote
        var rucs = empresas.Select(e => e.Ruc).Distinct().ToList();
        var cargas = await _archivoCargaRepo.ObtenerCargasPorRucsYAnioAsync(rucs, anio, cancellationToken);
        var cargasPorRuc = cargas.GroupBy(c => c.EmpresaRuc).ToDictionary(g => g.Key, g => g.ToList());

        var itemsEmpresas = new List<EmpresaLimiteItemDTO>();

        foreach (var emp in empresas)
        {
            cargasPorRuc.TryGetValue(emp.Ruc, out var cargasEmpresa);
            cargasEmpresa ??= new List<ArchivoCarga>();

            var cargasPorPeriodo = cargasEmpresa.GroupBy(c => c.Periodo);

            decimal totalVentasAnual = 0;
            decimal totalVentasBiAnual = 0;
            decimal maxVentasMensual = 0;

            decimal totalComprasAnual = 0;
            decimal totalComprasBiAnual = 0;
            decimal maxComprasMensual = 0;

            foreach (var grupoPeriodo in cargasPorPeriodo)
            {
                var cargasPeriodo = grupoPeriodo.ToList();

                // Resolver Ventas del periodo: Prioridad VentaMatch (1003) > VentaSire (1001) > VentaEmpresa (1002)
                var cargaVenta = cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.VentaMatch)
                              ?? cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.VentaSire)
                              ?? cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.VentaEmpresa);

                if (cargaVenta != null)
                {
                    totalVentasAnual += cargaVenta.TotalGeneral;
                    totalVentasBiAnual += cargaVenta.TotalBaseImponible;
                    if (cargaVenta.TotalGeneral > maxVentasMensual)
                    {
                        maxVentasMensual = cargaVenta.TotalGeneral;
                    }
                }

                // Resolver Compras del periodo: Prioridad CompraMatch (2003) > CompraSire (2001) > CompraEmpresa (2002)
                var cargaCompra = cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.CompraMatch)
                               ?? cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.CompraSire)
                               ?? cargasPeriodo.FirstOrDefault(c => c.IdTipoOperacion == TipoOperacion.CompraEmpresa);

                if (cargaCompra != null)
                {
                    totalComprasAnual += cargaCompra.TotalGeneral;
                    totalComprasBiAnual += cargaCompra.TotalBaseImponible;
                    if (cargaCompra.TotalGeneral > maxComprasMensual)
                    {
                        maxComprasMensual = cargaCompra.TotalGeneral;
                    }
                }
            }

            // 3. Evaluar límites de Ventas
            var resVentas = EvaluadorLimitesTributarios.Evaluar(
                emp.CodigoRegimenTributario,
                emp.RegimenDescripcion,
                totalVentasAnual,
                totalVentasBiAnual,
                maxVentasMensual,
                emp.LimiteAnualVentas,
                emp.LimiteMensualVentas,
                emp.VentasSinLimite,
                esVenta: true
            );

            // 4. Evaluar límites de Compras
            var resCompras = EvaluadorLimitesTributarios.Evaluar(
                emp.CodigoRegimenTributario,
                emp.RegimenDescripcion,
                totalComprasAnual,
                totalComprasBiAnual,
                maxComprasMensual,
                emp.LimiteAnualCompras,
                emp.LimiteMensualCompras,
                emp.ComprasSinLimite,
                esVenta: false
            );

            // 5. Determinar estado de semáforo general
            string semaforoGeneral;
            if (resVentas.Semaforo == "ROJO" || resCompras.Semaforo == "ROJO")
            {
                semaforoGeneral = "ROJO";
            }
            else if (resVentas.Semaforo == "AMBAR" || resCompras.Semaforo == "AMBAR")
            {
                semaforoGeneral = "AMBAR";
            }
            else
            {
                semaforoGeneral = "VERDE";
            }

            var item = new EmpresaLimiteItemDTO
            {
                IdEmpresa = emp.IdEmpresa,
                EmpresaRuc = emp.Ruc,
                RazonSocial = emp.RazonSocial,
                NombreComercial = emp.NombreComercial,
                CodigoRegimenTributario = emp.CodigoRegimenTributario,
                RegimenDescripcion = emp.RegimenDescripcion,
                Anio = anio,
                ValorUit = emp.ValorUit,
                EstadoGeneralSemaforo = semaforoGeneral,
                RequiereAtencion = semaforoGeneral != "VERDE",
                Ventas = new ConsumoLimiteDTO
                {
                    AcumuladoAnual = resVentas.AcumuladoAnual,
                    AcumuladoBaseImponible = resVentas.AcumuladoBaseImponible,
                    LimiteAnual = resVentas.LimiteAnual,
                    PorcentajeConsumo = resVentas.PorcentajeConsumo,
                    SaldoDisponible = resVentas.SaldoDisponible,
                    Semaforo = resVentas.Semaforo,
                    AlertaMensaje = resVentas.AlertaMensaje,
                    SinLimite = resVentas.SinLimite,
                    LimiteMensual = resVentas.LimiteMensual,
                    MaximoMensualRegistrado = resVentas.MaximoMensualRegistrado
                },
                Compras = new ConsumoLimiteDTO
                {
                    AcumuladoAnual = resCompras.AcumuladoAnual,
                    AcumuladoBaseImponible = resCompras.AcumuladoBaseImponible,
                    LimiteAnual = resCompras.LimiteAnual,
                    PorcentajeConsumo = resCompras.PorcentajeConsumo,
                    SaldoDisponible = resCompras.SaldoDisponible,
                    Semaforo = resCompras.Semaforo,
                    AlertaMensaje = resCompras.AlertaMensaje,
                    SinLimite = resCompras.SinLimite,
                    LimiteMensual = resCompras.LimiteMensual,
                    MaximoMensualRegistrado = resCompras.MaximoMensualRegistrado
                }
            };

            itemsEmpresas.Add(item);
        }

        // Ordenar: primero ROJO, luego AMBAR, luego VERDE, y por RazonSocial
        var empresasOrdenadas = itemsEmpresas
            .OrderByDescending(e => e.EstadoGeneralSemaforo == "ROJO")
            .ThenByDescending(e => e.EstadoGeneralSemaforo == "AMBAR")
            .ThenBy(e => e.RazonSocial)
            .ToList();

        var uitRef = empresas.FirstOrDefault(e => e.ValorUit > 0)?.ValorUit ?? 0;

        return new LimitesTributariosEmpresasResponseDTO
        {
            Anio = anio,
            ValorUitReferencia = uitRef,
            TotalEmpresas = empresasOrdenadas.Count,
            TotalEmpresasEnRiesgoCritico = empresasOrdenadas.Count(e => e.EstadoGeneralSemaforo == "ROJO"),
            TotalEmpresasEnAlerta = empresasOrdenadas.Count(e => e.EstadoGeneralSemaforo == "AMBAR"),
            TotalEmpresasNormales = empresasOrdenadas.Count(e => e.EstadoGeneralSemaforo == "VERDE"),
            Empresas = empresasOrdenadas
        };
    }
}
