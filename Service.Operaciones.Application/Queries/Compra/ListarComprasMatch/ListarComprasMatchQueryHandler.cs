using MediatR;
using Service.Operaciones.Application.DTOs.CompraMatch;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Compra.ListarComprasMatch;

public class ListarComprasMatchQueryHandler : IRequestHandler<ListarComprasMatchQuery, List<CompraMatchDTO>>
{
    private readonly ICompraMatchRepository _compraMatchRepo;

    public ListarComprasMatchQueryHandler(ICompraMatchRepository compraMatchRepo)
    {
        _compraMatchRepo = compraMatchRepo;
    }

    public async Task<List<CompraMatchDTO>> Handle(ListarComprasMatchQuery request, CancellationToken cancellationToken)
    {
        var lista = await _compraMatchRepo.ListarPorCargaAsync(request.IdCarga, cancellationToken);

        return lista.Select(c => new CompraMatchDTO
        {
            IdCompraMatch = c.IdCompraMatch,
            IdCarga = c.IdCarga,
            EmpresaRuc = c.EmpresaRuc,
            Periodo = c.Periodo,
            NumeroLinea = c.NumeroLinea,
            OrigenDato = c.OrigenDato,
            EsCoincidenciaExacta = c.EsCoincidenciaExacta,
            EsDiferencia = c.EsDiferencia,
            EsSoloUnOrigen = c.EsSoloUnOrigen,
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
