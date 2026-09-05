using MediatR;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Compra;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Compra.ListarCompras;

public class ListarComprasQueryHandler : IRequestHandler<ListarComprasQuery, List<CompraDTO>>
{
    private readonly ICompraSireRepository _repositorio;
    private readonly IArchivoCargaRepository _cargaRepository;
    private readonly IAccesoEmpresaValidator _accesoValidator;

    public ListarComprasQueryHandler(ICompraSireRepository repositorio, IArchivoCargaRepository cargaRepository, IAccesoEmpresaValidator accesoValidator)
    {
        _repositorio = repositorio;
        _cargaRepository = cargaRepository;
        _accesoValidator = accesoValidator;
    }

    public async Task<List<CompraDTO>> Handle(ListarComprasQuery request, CancellationToken cancellationToken)
    {
        var carga = await _cargaRepository.ObtenerPorIdAsync(request.IdCarga, cancellationToken)
            ?? throw new NotFoundException("Carga", request.IdCarga);

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        var compras = await _repositorio.ListarPorCargaAsync(
            request.IdCarga, cancellationToken);

        return compras.Select(c => new CompraDTO
        {
            IdCompra = c.IdCompra,
            EmpresaRuc = c.EmpresaRuc,
            Periodo = c.Periodo,
            CarSunat = c.CarSunat,
            CodigoTipoCp = c.CodigoTipoCp,
            Serie = c.Serie,
            Numero = c.Numero,
            FechaEmision = c.FechaEmision,
            CodigoTipoDocIdentidad = c.CodigoTipoDocIdentidad,
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
