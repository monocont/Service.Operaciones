using MediatR;
using Service.Operaciones.Application.DTOs.LimitesTributarios;

namespace Service.Operaciones.Application.Queries.LimitesTributarios;

public record ObtenerLimitesTributariosEmpresasQuery(int Anio) : IRequest<LimitesTributariosEmpresasResponseDTO>;
