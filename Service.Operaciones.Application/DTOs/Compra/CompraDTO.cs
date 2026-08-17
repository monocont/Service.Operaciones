namespace Service.Operaciones.Application.DTOs.Compra;

public class CompraDTO
{
    public Guid IdCompra { get; set; }
    public string CarSunat { get; set; } = string.Empty;
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal TotalCp { get; set; }
    public string CodigoMoneda { get; set; } = string.Empty;
    public decimal TipoCambio { get; set; }
    public string CodigoEstadoComprobante { get; set; } = string.Empty;
    public string? Detraccion { get; set; }
    public string? CodigoTipoNota { get; set; }
}
