using MediatR;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerCargaPorId;

public class ObtenerCargaPorIdQueryHandler : IRequestHandler<ObtenerCargaPorIdQuery, ObtenerCargaPorIdDTO>
{
    private readonly IArchivoCargaRepository _repositorio;
    private readonly IAccesoEmpresaValidator _accesoValidator;

    public ObtenerCargaPorIdQueryHandler(IArchivoCargaRepository repositorio, IAccesoEmpresaValidator accesoValidator)
    {
        _repositorio = repositorio;
        _accesoValidator = accesoValidator;
    }

    public async Task<ObtenerCargaPorIdDTO> Handle(ObtenerCargaPorIdQuery request, CancellationToken cancellationToken)
    {
        var carga = await _repositorio.ObtenerPorIdAsync(request.IdCarga, cancellationToken)
            ?? throw new NotFoundException("Carga", request.IdCarga);

        await _accesoValidator.ValidarAccesoAsync(carga.EmpresaRuc, cancellationToken);

        return new ObtenerCargaPorIdDTO
        {
            IdCarga = carga.IdCarga,
            EmpresaRuc = carga.EmpresaRuc,
            Periodo = carga.Periodo,
            TipoOperacion = carga.IdTipoOperacion,
            Formato = carga.Formato,
            NombreOriginal = carga.NombreOriginal,
            HashDocumento = carga.HashDocumento,
            Estado = carga.Estado,
            NumRegistros = carga.NumRegistros,
            NumRegistrosValidos = carga.NumRegistrosValidos,
            NumRegistrosError = carga.NumRegistrosError,
            TotalBaseImponible = carga.TotalBaseImponible,
            TotalIgv = carga.TotalIgv,
            TotalGeneral = carga.TotalGeneral,
            Observaciones = carga.Observaciones,
            FechaCreacion = carga.FechaCreacion,
            CreadoPor = carga.CreadoPor,
            FechaModificacion = carga.FechaModificacion,
            ModificadoPor = carga.ModificadoPor
        };
    }
}
