using MediatR;
using Service.Operaciones.Application.DTOs.Carga;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerCargaPorId;

public class ObtenerCargaPorIdQuery : IRequest<ObtenerCargaPorIdDTO>
{
    public required Guid IdCarga { get; set; }
}
