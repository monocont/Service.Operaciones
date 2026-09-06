namespace Service.Operaciones.Application.DTOs.LimitesTributarios;

public class EmpresaConLimitesHttpDTO
{
    public Guid IdEmpresa { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string CodigoRegimenTributario { get; set; } = string.Empty;
    public string RegimenDescripcion { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal ValorUit { get; set; }
    public decimal? LimiteMensualVentas { get; set; }
    public decimal? LimiteMensualCompras { get; set; }
    public decimal? LimiteAnualVentas { get; set; }
    public decimal? LimiteAnualCompras { get; set; }
    public int? LimiteAnualVentasUit { get; set; }
    public bool VentasSinLimite { get; set; }
    public bool ComprasSinLimite { get; set; }
}
