using MediatR;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ListarCargas;

public class ListarCargasQueryHandler : IRequestHandler<ListarCargasQuery, List<ListarCargasDTO>>
{
    private readonly IArchivoCargaRepository _repositorio;
    private readonly IArchivoCargaErrorRepository _errorRepositorio;

    public ListarCargasQueryHandler(
        IArchivoCargaRepository repositorio,
        IArchivoCargaErrorRepository errorRepositorio)
    {
        _repositorio = repositorio;
        _errorRepositorio = errorRepositorio;
    }

    public async Task<List<ListarCargasDTO>> Handle(ListarCargasQuery request, CancellationToken cancellationToken)
    {
        var cargas = await _repositorio.ListarAsync(
            request.EmpresaRuc,
            request.TipoArchivo,
            request.Periodo,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var idsCarga = cargas.Select(c => c.IdCarga).ToList();
        var conteoErrores = await _errorRepositorio.ContarPorCargasAsync(idsCarga, cancellationToken);

        return cargas.Select(c => new ListarCargasDTO
        {
            IdCarga = c.IdCarga,
            EmpresaRuc = c.EmpresaRuc,
            Periodo = c.Periodo,
            Formato = c.Formato.ToString(),
            NombreOriginal = c.NombreOriginal,
            NumRegistros = c.NumRegistros,
            NumRegistrosValidos = c.NumRegistrosValidos,
            NumRegistrosError = c.NumRegistrosError,
            NumObservaciones = conteoErrores.TryGetValue(c.IdCarga, out var totalObs) ? totalObs : c.NumRegistrosError
        }).ToList();
    }
}
