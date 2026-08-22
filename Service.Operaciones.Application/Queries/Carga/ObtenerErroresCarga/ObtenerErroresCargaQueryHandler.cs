using MediatR;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerErroresCarga;

public class ObtenerErroresCargaQueryHandler : IRequestHandler<ObtenerErroresCargaQuery, List<ObtenerErroresCargaDTO>>
{
    private readonly IArchivoCargaErrorRepository _repositorio;
    private readonly IArchivoCargaRepository _cargaRepository;
    private readonly IAccesoEmpresaValidator _accesoValidator;

    public ObtenerErroresCargaQueryHandler(IArchivoCargaErrorRepository repositorio, IArchivoCargaRepository cargaRepository, IAccesoEmpresaValidator accesoValidator)
    {
        _repositorio = repositorio;
        _cargaRepository = cargaRepository;
        _accesoValidator = accesoValidator;
    }

    public async Task<List<ObtenerErroresCargaDTO>> Handle(ObtenerErroresCargaQuery request, CancellationToken cancellationToken)
    {
        var carga = await _cargaRepository.ObtenerPorIdAsync(request.IdCarga, cancellationToken)
            ?? throw new NotFoundException("Carga", request.IdCarga);

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        var errores = await _repositorio.ObtenerPorCargaAsync(request.IdCarga, cancellationToken);

        return errores
            .OrderBy(e => e.NumeroLinea)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ObtenerErroresCargaDTO
            {
                IdError = e.IdError,
                IdCarga = e.IdCarga,
                NumeroLinea = e.NumeroLinea,
                TipoError = e.TipoError,
                CampoError = e.CampoError,
                ValorLectura = e.ValorLectura,
                Mensaje = e.Mensaje,
                Severidad = e.Severidad,
                FechaRegistro = e.FechaRegistro
            }).ToList();
    }
}
