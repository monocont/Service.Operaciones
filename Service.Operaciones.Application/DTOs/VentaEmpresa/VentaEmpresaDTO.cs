namespace Service.Operaciones.Application.DTOs.VentaEmpresa;

public class VentaEmpresaDTO
{
    public Guid IdVentaEmpresa { get; set; }
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public int NumeroLinea { get; set; }

    public string? CarSunat { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? NumeroFinal { get; set; }

    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;

    public decimal ValorFacturadoExportacion { get; set; }
    public decimal BiGravada { get; set; }
    public decimal DescuentoBi { get; set; }
    public decimal IgvIpm { get; set; }
    public decimal DescuentoIgv { get; set; }
    public decimal MontoExonerado { get; set; }
    public decimal MontoInafecto { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal BiGravadaIvap { get; set; }
    public decimal MontoIvap { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }

    public string CodigoMoneda { get; set; } = string.Empty;
    public decimal TipoCambio { get; set; }

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? NumeroCpModificado { get; set; }
    public string CodigoEstadoComprobante { get; set; } = "1";
    public string? CodigoTipoNota { get; set; }
    public string? TipoOperacion { get; set; }
    public string? CamposLibres { get; set; }
}
