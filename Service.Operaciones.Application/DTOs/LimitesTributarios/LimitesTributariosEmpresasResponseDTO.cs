namespace Service.Operaciones.Application.DTOs.LimitesTributarios;

public class LimitesTributariosEmpresasResponseDTO
{
    public int Anio { get; set; }
    public decimal ValorUitReferencia { get; set; }
    public int TotalEmpresas { get; set; }
    public int TotalEmpresasEnRiesgoCritico { get; set; }
    public int TotalEmpresasEnAlerta { get; set; }
    public int TotalEmpresasNormales { get; set; }
    public List<EmpresaLimiteItemDTO> Empresas { get; set; } = new();
}
