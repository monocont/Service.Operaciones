using MediatR;

namespace Service.Operaciones.Application.Commands.Venta.EjecutarMatchVentas;

public record EjecutarMatchVentasCommand(
    string EmpresaRuc,
    string Periodo,
    string Usuario
) : IRequest<EjecutarMatchVentasResponseDTO>;
