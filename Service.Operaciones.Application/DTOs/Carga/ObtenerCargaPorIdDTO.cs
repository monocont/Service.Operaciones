using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.DTOs.Carga;

public class ObtenerCargaPorIdDTO
{
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public TipoOperacion TipoOperacion { get; set; }
    public FormatoArchivo Formato { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string HashDocumento { get; set; } = string.Empty;
    public EstadoCarga Estado { get; set; }
    public int NumRegistros { get; set; }
    public int NumRegistrosValidos { get; set; }
    public int NumRegistrosError { get; set; }
    public decimal TotalBaseImponible { get; set; }
    public decimal TotalIgv { get; set; }
    public decimal TotalGeneral { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
}
