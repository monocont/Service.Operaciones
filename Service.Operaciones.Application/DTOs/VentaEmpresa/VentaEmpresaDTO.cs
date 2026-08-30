namespace Service.Operaciones.Application.DTOs.VentaEmpresa;

public class VentaEmpresaDTO
{
    public Guid IdVentaEmpresa { get; set; }
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public int NumeroLinea { get; set; }
    public DateTime FechaEmision { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public decimal TotalCp { get; set; }
    public string CodigoMoneda { get; set; } = string.Empty;
    public decimal TipoCambio { get; set; }
}
