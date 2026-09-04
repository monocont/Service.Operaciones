using MediatR;
using Service.Operaciones.Application.DTOs.VentaMatch;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentasMatch;

public class ListarVentasMatchQueryHandler : IRequestHandler<ListarVentasMatchQuery, List<VentaMatchDTO>>
{
    private readonly IVentaMatchRepository _ventaMatchRepo;

    public ListarVentasMatchQueryHandler(IVentaMatchRepository ventaMatchRepo)
    {
        _ventaMatchRepo = ventaMatchRepo;
    }

    public async Task<List<VentaMatchDTO>> Handle(ListarVentasMatchQuery request, CancellationToken cancellationToken)
    {
        var lista = await _ventaMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

        return lista.Select(v => new VentaMatchDTO
        {
            IdVentaMatch = v.IdVentaMatch,
            IdCarga = v.IdCarga,
            EmpresaRuc = v.EmpresaRuc,
            Periodo = v.Periodo,
            NumeroLinea = v.NumeroLinea,
            OrigenDato = v.OrigenDato,
            EsCoincidenciaExacta = v.EsCoincidenciaExacta,
            EsDiferencia = v.EsDiferencia,
            EsSoloUnOrigen = v.EsSoloUnOrigen,
            CarSunat = v.CarSunat,
            CodigoTipoCp = v.CodigoTipoCp,
            Serie = v.Serie,
            Numero = v.Numero,
            NumeroFinal = v.NumeroFinal,
            FechaEmision = v.FechaEmision,
            FechaVencimiento = v.FechaVencimiento,
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
