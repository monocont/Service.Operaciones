using MediatR;
using Service.Operaciones.Application.DTOs.CompraMatch;

namespace Service.Operaciones.Application.Queries.Compra.ListarComprasMatch;

public record ListarComprasMatchQuery(Guid IdCarga) : IRequest<List<CompraMatchDTO>>;
