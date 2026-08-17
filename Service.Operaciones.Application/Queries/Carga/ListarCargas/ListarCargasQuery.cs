using MediatR;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Queries.Carga.ListarCargas;

public class ListarCargasQuery : IRequest<List<ListarCargasDTO>>
{
    public required string EmpresaRuc { get; set; }
    public required TipoArchivo TipoArchivo { get; set; }
    public string? Periodo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
