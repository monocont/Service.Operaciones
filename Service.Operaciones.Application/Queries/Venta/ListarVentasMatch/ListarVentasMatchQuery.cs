using MediatR;
using Service.Operaciones.Application.DTOs.VentaMatch;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentasMatch;

public record ListarVentasMatchQuery(Guid IdCarga) : IRequest<List<VentaMatchDTO>>;
