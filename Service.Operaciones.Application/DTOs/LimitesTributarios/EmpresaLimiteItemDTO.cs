namespace Service.Operaciones.Application.DTOs.LimitesTributarios;

public class EmpresaLimiteItemDTO
{
    public Guid IdEmpresa { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string CodigoRegimenTributario { get; set; } = string.Empty;
    public string RegimenDescripcion { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal ValorUit { get; set; }
    public string EstadoGeneralSemaforo { get; set; } = "VERDE"; // Máxima severidad entre ventas y compras
    public bool RequiereAtencion { get; set; }
    public ConsumoLimiteDTO Ventas { get; set; } = new();
    public ConsumoLimiteDTO Compras { get; set; } = new();
}
