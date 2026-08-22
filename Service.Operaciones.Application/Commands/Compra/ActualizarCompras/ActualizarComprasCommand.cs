using MediatR;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarCompras;

public record ActualizarComprasCommand : IRequest<ActualizarComprasResponseDTO>
{
    public Guid IdCarga { get; init; }
    public List<Guid> EliminadosIds { get; init; } = new();
    public List<CrearCompraRegistroDTO> Nuevos { get; init; } = new();
    public List<ModificarCompraRegistroDTO> Modificados { get; init; } = new();
    public string? Usuario { get; init; }
}

public class CrearCompraRegistroDTO
{
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? Detraccion { get; set; }
    public string? CarSunat { get; set; }
}

public class ModificarCompraRegistroDTO
{
    public Guid IdCompra { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? Detraccion { get; set; }
    public string? CarSunat { get; set; }
}

public class ActualizarComprasResponseDTO
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public int NumObservaciones { get; set; }
}
