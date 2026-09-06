using MediatR;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarComprasMatch;

public record ActualizarComprasMatchCommand : IRequest<ActualizarComprasMatchResponseDTO>
{
    public Guid IdCarga { get; init; }
    public List<Guid> EliminadosIds { get; init; } = new();
    public List<CrearCompraMatchRegistroDTO> Nuevos { get; init; } = new();
    public List<ModificarCompraMatchRegistroDTO> Modificados { get; init; } = new();
    public string? Usuario { get; init; }
}

public class CrearCompraMatchRegistroDTO
{
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal BiGravadoDgng { get; set; }
    public decimal IgvIpmDgng { get; set; }
    public decimal BiGravadoDng { get; set; }
    public decimal IgvIpmDng { get; set; }
    public decimal ValorAdqNg { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? CarSunat { get; set; }
    public string? OrigenDato { get; set; } = "SIRE";
}

public class ModificarCompraMatchRegistroDTO
{
    public Guid IdCompraMatch { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal BiGravadoDgng { get; set; }
    public decimal IgvIpmDgng { get; set; }
    public decimal BiGravadoDng { get; set; }
    public decimal IgvIpmDng { get; set; }
    public decimal ValorAdqNg { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }
    public string? CodigoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; } = 1.0m;
    public string? CodigoEstadoComprobante { get; set; } = "1";
    public string? CarSunat { get; set; }
    public string? OrigenDato { get; set; }
}

public class ActualizarComprasMatchResponseDTO
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
