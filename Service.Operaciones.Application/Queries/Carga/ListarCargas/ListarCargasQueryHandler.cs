using MediatR;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ListarCargas;

public class ListarCargasQueryHandler : IRequestHandler<ListarCargasQuery, List<ListarCargasDTO>>
{
    private readonly IArchivoCargaRepository _repositorio;
    private readonly IAccesoEmpresaValidator _accesoValidator;

    public ListarCargasQueryHandler(IArchivoCargaRepository repositorio, IAccesoEmpresaValidator accesoValidator)
    {
        _repositorio = repositorio;
        _accesoValidator = accesoValidator;
    }

    public async Task<List<ListarCargasDTO>> Handle(ListarCargasQuery request, CancellationToken cancellationToken)
    {
        await _accesoValidator.ValidarAccesoAsync(request.EmpresaRuc, cancellationToken);

        var cargas = await _repositorio.ListarAsync(
            request.EmpresaRuc,
            request.TipoArchivo,
            request.Periodo,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return cargas.Select(c => new ListarCargasDTO
        {
            IdCarga = c.IdCarga,
            EmpresaRuc = c.EmpresaRuc,
            Periodo = c.Periodo,
            Formato = c.Formato.ToString(),
            NombreOriginal = c.NombreOriginal,
            NumRegistros = c.NumRegistros,
            NumRegistrosValidos = c.NumRegistrosValidos,
            NumRegistrosError = c.NumRegistrosError
        }).ToList();
    }
}
