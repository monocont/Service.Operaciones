using MediatR;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ListarCargas;

public class ListarCargasQueryHandler : IRequestHandler<ListarCargasQuery, List<ListarCargasDTO>>
{
    private readonly IArchivoCargaRepository _repositorio;

    public ListarCargasQueryHandler(IArchivoCargaRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<ListarCargasDTO>> Handle(ListarCargasQuery request, CancellationToken cancellationToken)
    {
        var cargas = await _repositorio.ListarAsync(
            request.EmpresaRuc,
            request.TipoArchivo,
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
