using MediatR;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerCargaPorId;

public class ObtenerCargaPorIdQueryHandler : IRequestHandler<ObtenerCargaPorIdQuery, ObtenerCargaPorIdDTO>
{
    private readonly IArchivoCargaRepository _repositorio;

    public ObtenerCargaPorIdQueryHandler(IArchivoCargaRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ObtenerCargaPorIdDTO> Handle(ObtenerCargaPorIdQuery request, CancellationToken cancellationToken)
    {
        var carga = await _repositorio.ObtenerPorIdAsync(request.IdCarga, cancellationToken)
            ?? throw new NotFoundException("Carga", request.IdCarga);

        return new ObtenerCargaPorIdDTO
        {
            IdCarga = carga.IdCarga,
            EmpresaRuc = carga.EmpresaRuc,
            Periodo = carga.Periodo,
            TipoArchivo = carga.TipoArchivo,
            Formato = carga.Formato,
            NombreOriginal = carga.NombreOriginal,
            HashDocumento = carga.HashDocumento,
            Estado = carga.Estado,
            NumRegistros = carga.NumRegistros,
            NumRegistrosValidos = carga.NumRegistrosValidos,
            NumRegistrosError = carga.NumRegistrosError,
            Observaciones = carga.Observaciones,
            FechaCreacion = carga.FechaCreacion,
            CreadoPor = carga.CreadoPor,
            FechaModificacion = carga.FechaModificacion,
            ModificadoPor = carga.ModificadoPor
        };
    }
}
