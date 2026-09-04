using MediatR;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentasMatch;

public record ActualizarVentasMatchCommand : IRequest<ActualizarVentasMatchResponseDTO>
{
    public Guid IdCarga { get; init; }
    public List<Guid> EliminadosIds { get; init; } = new();
    public List<CrearVentaMatchRegistroDTO> Nuevos { get; init; } = new();
    public List<ModificarVentaMatchRegistroDTO> Modificados { get; init; } = new();
    public string? Usuario { get; init; }
}

public class CrearVentaMatchRegistroDTO
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
    public string? OrigenDato { get; set; } = "SIRE";
}

public class ModificarVentaMatchRegistroDTO
{
    public Guid IdVentaMatch { get; set; }
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
    public string? OrigenDato { get; set; }
}

public class ActualizarVentasMatchResponseDTO
{
    public Guid IdCarga { get; set; }
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public int NumObservaciones { get; set; }
    public decimal TotalBaseImponible { get; set; }
    public decimal TotalIgv { get; set; }
    public decimal TotalGeneral { get; set; }
}
