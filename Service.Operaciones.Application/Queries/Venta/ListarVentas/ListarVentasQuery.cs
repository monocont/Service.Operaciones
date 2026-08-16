using MediatR;
using Service.Operaciones.Application.DTOs.Venta;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentas;

public class ListarVentasQuery : IRequest<List<VentaDTO>>
{
    public required Guid IdCarga { get; set; }
}
