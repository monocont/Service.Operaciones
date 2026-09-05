using MediatR;
using Service.Operaciones.Application.DTOs.VentaEmpresa;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentasEmpresa;

public class ListarVentasEmpresaQueryHandler : IRequestHandler<ListarVentasEmpresaQuery, List<VentaEmpresaDTO>>
{
    private readonly IVentaEmpresaRepository _ventaEmpresaRepo;

    public ListarVentasEmpresaQueryHandler(IVentaEmpresaRepository ventaEmpresaRepo)
    {
        _ventaEmpresaRepo = ventaEmpresaRepo;
    }

    public async Task<List<VentaEmpresaDTO>> Handle(ListarVentasEmpresaQuery request, CancellationToken cancellationToken)
    {
        var ventas = await _ventaEmpresaRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

        return ventas.Select(v => new VentaEmpresaDTO
        {
            IdVentaEmpresa = v.IdVentaEmpresa,
            IdCarga = v.IdCarga,
            EmpresaRuc = v.EmpresaRuc,
            Periodo = v.Periodo,
            NumeroLinea = v.NumeroLinea,
            CarSunat = v.CarSunat,
            FechaEmision = v.FechaEmision,
            FechaVencimiento = v.FechaVencimiento,
            CodigoTipoCp = v.CodigoTipoCp,
            Serie = v.Serie,
            Numero = v.Numero,
            NumeroFinal = v.NumeroFinal,
            CodigoTipoDocIdentidad = v.CodigoTipoDocIdentidad,
            NroDocIdentidad = v.NroDocIdentidad,
            RazonSocial = v.RazonSocial,
            ValorFacturadoExportacion = v.ValorFacturadoExportacion,
            BiGravada = v.BiGravada,
            DescuentoBi = v.DescuentoBi,
            IgvIpm = v.IgvIpm,
            DescuentoIgv = v.DescuentoIgv,
            MontoExonerado = v.MontoExonerado,
            MontoInafecto = v.MontoInafecto,
            MontoIsc = v.MontoIsc,
            BiGravadaIvap = v.BiGravadaIvap,
            MontoIvap = v.MontoIvap,
            MontoIcbper = v.MontoIcbper,
            MontoOtrosTributos = v.MontoOtrosTributos,
            TotalCp = v.TotalCp,
            CodigoMoneda = v.CodigoMoneda,
            TipoCambio = v.TipoCambio,
            FechaEmisionDocModificado = v.FechaEmisionDocModificado,
            CodigoTipoCpModificado = v.CodigoTipoCpModificado,
            SerieCpModificado = v.SerieCpModificado,
            NumeroCpModificado = v.NumeroCpModificado,
            CodigoEstadoComprobante = v.CodigoEstadoComprobante,
            CodigoTipoNota = v.CodigoTipoNota,
            TipoOperacion = v.TipoOperacion,
            CamposLibres = v.CamposLibres
        }).ToList();
    }
}
