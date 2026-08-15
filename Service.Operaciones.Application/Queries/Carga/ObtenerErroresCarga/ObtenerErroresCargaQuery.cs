using MediatR;
using Service.Operaciones.Application.DTOs.Carga;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerErroresCarga;

public class ObtenerErroresCargaQuery : IRequest<List<ObtenerErroresCargaDTO>>
{
    public required Guid IdCarga { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
