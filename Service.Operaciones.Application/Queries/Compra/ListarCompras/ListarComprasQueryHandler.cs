using MediatR;
using Service.Operaciones.Application.DTOs.Compra;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Compra.ListarCompras;

public class ListarComprasQueryHandler : IRequestHandler<ListarComprasQuery, List<CompraDTO>>
{
    private readonly ICompraRepository _repositorio;

    public ListarComprasQueryHandler(ICompraRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<CompraDTO>> Handle(ListarComprasQuery request, CancellationToken cancellationToken)
    {
        var compras = await _repositorio.ListarPorCargaAsync(
            request.IdCarga, request.PageNumber, request.PageSize, cancellationToken);

        return compras.Select(c => new CompraDTO
        {
            IdCompra = c.IdCompra,
            CarSunat = c.CarSunat,
            CodigoTipoCp = c.CodigoTipoCp,
            Serie = c.Serie,
            Numero = c.Numero,
            FechaEmision = c.FechaEmision,
            NroDocIdentidad = c.NroDocIdentidad,
            RazonSocial = c.RazonSocial,
            BiGravadoDg = c.BiGravadoDg,
            IgvIpmDg = c.IgvIpmDg,
            TotalCp = c.TotalCp,
            CodigoMoneda = c.CodigoMoneda,
            TipoCambio = c.TipoCambio,
            CodigoEstadoComprobante = c.CodigoEstadoComprobante,
            Detraccion = c.Detraccion,
            CodigoTipoNota = c.CodigoTipoNota
        }).ToList();
    }
}
