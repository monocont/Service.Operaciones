namespace Service.Operaciones.Application.Commands.Compra.EjecutarMatchCompras;

public class EjecutarMatchComprasResponseDTO
{
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public int TotalRegistrosSire { get; set; }
    public int TotalRegistrosEmpresa { get; set; }
    public int TotalConsolidado { get; set; }
    public int CoincidenciasExactas { get; set; }
    public int Diferencias { get; set; }
    public int SoloUnOrigen { get; set; }
    public int TotalObservaciones { get; set; }
    public decimal TotalBaseImponible { get; set; }
    public decimal TotalIgv { get; set; }
    public decimal TotalGeneral { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
