using MediatR;
using Service.Operaciones.Application.DTOs.CompraEmpresa;

namespace Service.Operaciones.Application.Queries.Compra.ListarComprasEmpresa;

public record ListarComprasEmpresaQuery(Guid IdCarga) : IRequest<List<CompraEmpresaDTO>>;
