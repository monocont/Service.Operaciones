using MediatR;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentas;

public record ActualizarVentasCommand : IRequest<ActualizarVentasResponseDTO>
{
    public Guid IdCarga { get; init; }
    public List<Guid> EliminadosIds { get; init; } = new();
    public string? Usuario { get; init; }
}

public class ActualizarVentasResponseDTO
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public int NumObservaciones { get; set; }
}
