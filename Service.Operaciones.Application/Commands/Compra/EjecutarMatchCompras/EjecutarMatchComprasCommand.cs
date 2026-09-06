using MediatR;

namespace Service.Operaciones.Application.Commands.Compra.EjecutarMatchCompras;

public record EjecutarMatchComprasCommand(
    string EmpresaRuc,
    string Periodo,
    string Usuario
) : IRequest<EjecutarMatchComprasResponseDTO>;
