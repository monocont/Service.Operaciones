using MediatR;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentas;

public record ActualizarVentasCommand : IRequest<ActualizarVentasResponseDTO>
{
    public Guid IdCarga { get; init; }
    public List<Guid> EliminadosIds { get; init; } = new();
    public List<CrearVentaRegistroDTO> Nuevos { get; init; } = new();
    public List<ModificarVentaRegistroDTO> Modificados { get; init; } = new();
    public string? Usuario { get; init; }
}

public class CrearVentaRegistroDTO
{
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravada { get; set; }
    public decimal IgvIpm { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? CarSunat { get; set; }
}

public class ModificarVentaRegistroDTO
{
    public Guid IdVenta { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravada { get; set; }
    public decimal IgvIpm { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? CarSunat { get; set; }
}

public class ActualizarVentasResponseDTO
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public int NumObservaciones { get; set; }
}
