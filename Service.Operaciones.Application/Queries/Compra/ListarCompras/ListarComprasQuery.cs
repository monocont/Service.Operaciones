using MediatR;
using Service.Operaciones.Application.DTOs.Compra;

namespace Service.Operaciones.Application.Queries.Compra.ListarCompras;

public class ListarComprasQuery : IRequest<List<CompraDTO>>
{
    public required Guid IdCarga { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
