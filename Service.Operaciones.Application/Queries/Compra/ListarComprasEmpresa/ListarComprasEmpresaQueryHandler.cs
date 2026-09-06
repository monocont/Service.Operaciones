using MediatR;
using Service.Operaciones.Application.DTOs.CompraEmpresa;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Compra.ListarComprasEmpresa;

public class ListarComprasEmpresaQueryHandler : IRequestHandler<ListarComprasEmpresaQuery, List<CompraEmpresaDTO>>
{
    private readonly ICompraEmpresaRepository _compraEmpresaRepo;

    public ListarComprasEmpresaQueryHandler(ICompraEmpresaRepository compraEmpresaRepo)
    {
        _compraEmpresaRepo = compraEmpresaRepo;
    }

    public async Task<List<CompraEmpresaDTO>> Handle(ListarComprasEmpresaQuery request, CancellationToken cancellationToken)
    {
        var compras = await _compraEmpresaRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

        return compras.Select(c => new CompraEmpresaDTO
        {
            IdCompraEmpresa = c.IdCompraEmpresa,
            IdCarga = c.IdCarga,
            EmpresaRuc = c.EmpresaRuc,
            Periodo = c.Periodo,
            NumeroLinea = c.NumeroLinea,
            CarSunat = c.CarSunat,
            FechaEmision = c.FechaEmision,
            FechaVencimiento = c.FechaVencimiento,
            CodigoTipoCp = c.CodigoTipoCp,
            Serie = c.Serie,
            AnioDocumento = c.AnioDocumento,
            Numero = c.Numero,
            NumeroFinal = c.NumeroFinal,
            CodigoTipoDocIdentidad = c.CodigoTipoDocIdentidad,
            NroDocIdentidad = c.NroDocIdentidad,
            RazonSocial = c.RazonSocial,
            BiGravadoDg = c.BiGravadoDg,
            IgvIpmDg = c.IgvIpmDg,
            BiGravadoDgng = c.BiGravadoDgng,
            IgvIpmDgng = c.IgvIpmDgng,
            BiGravadoDng = c.BiGravadoDng,
            IgvIpmDng = c.IgvIpmDng,
            ValorAdqNg = c.ValorAdqNg,
            MontoIsc = c.MontoIsc,
            MontoIcbper = c.MontoIcbper,
            MontoOtrosTributos = c.MontoOtrosTributos,
            TotalCp = c.TotalCp,
            CodigoMoneda = c.CodigoMoneda,
            TipoCambio = c.TipoCambio,
            FechaEmisionDocModificado = c.FechaEmisionDocModificado,
            CodigoTipoCpModificado = c.CodigoTipoCpModificado,
            SerieCpModificado = c.SerieCpModificado,
            CodDamDsi = c.CodDamDsi,
            NumeroCpModificado = c.NumeroCpModificado,
            ClasifBssSss = c.ClasifBssSss,
            IdProyectoOp = c.IdProyectoOp,
            PorcPart = c.PorcPart,
            Imb = c.Imb,
            CarOrigIndEI = c.CarOrigIndEI,
            Detraccion = c.Detraccion,
            CodigoTipoNota = c.CodigoTipoNota,
            CodigoEstadoComprobante = c.CodigoEstadoComprobante,
            Incal = c.Incal,
            CamposLibres = c.CamposLibres
        }).ToList();
    }
}
