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
            FechaEmision = v.FechaEmision,
            CodigoTipoCp = v.CodigoTipoCp,
            Serie = v.Serie,
            Numero = v.Numero,
            CodigoTipoDocIdentidad = v.CodigoTipoDocIdentidad,
            NroDocIdentidad = v.NroDocIdentidad,
            TotalCp = v.TotalCp,
            CodigoMoneda = v.CodigoMoneda,
            TipoCambio = v.TipoCambio
        }).ToList();
    }
}
