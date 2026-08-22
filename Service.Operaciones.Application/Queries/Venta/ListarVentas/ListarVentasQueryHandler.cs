using MediatR;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Venta;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentas;

public class ListarVentasQueryHandler : IRequestHandler<ListarVentasQuery, List<VentaDTO>>
{
    private readonly IVentaRepository _repositorio;
    private readonly IArchivoCargaRepository _cargaRepository;
    private readonly IAccesoEmpresaValidator _accesoValidator;

    public ListarVentasQueryHandler(IVentaRepository repositorio, IArchivoCargaRepository cargaRepository, IAccesoEmpresaValidator accesoValidator)
    {
        _repositorio = repositorio;
        _cargaRepository = cargaRepository;
        _accesoValidator = accesoValidator;
    }

    public async Task<List<VentaDTO>> Handle(ListarVentasQuery request, CancellationToken cancellationToken)
    {
        var carga = await _cargaRepository.ObtenerPorIdAsync(request.IdCarga, cancellationToken)
            ?? throw new NotFoundException("Carga", request.IdCarga);

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        var ventas = await _repositorio.ListarPorCargaAsync(
            request.IdCarga, cancellationToken);

        return ventas.Select(v => new VentaDTO
        {
            IdVenta = v.IdVenta,
            CarSunat = v.CarSunat,
            CodigoTipoCp = v.CodigoTipoCp,
            Serie = v.Serie,
            Numero = v.Numero,
            FechaEmision = v.FechaEmision,
            CodigoTipoDocIdentidad = v.CodigoTipoDocIdentidad,
            NroDocIdentidad = v.NroDocIdentidad,
            RazonSocial = v.RazonSocial,
            BiGravada = v.BiGravada,
            IgvIpm = v.IgvIpm,
            TotalCp = v.TotalCp,
            CodigoMoneda = v.CodigoMoneda,
            TipoCambio = v.TipoCambio,
            CodigoEstadoComprobante = v.CodigoEstadoComprobante,
            CodigoTipoNota = v.CodigoTipoNota,
            TipoOperacion = v.TipoOperacion,
            CamposLibres = v.CamposLibres
        }).ToList();
    }
}
