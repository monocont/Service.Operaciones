using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.DTOs.Carga;

public class ListarCargasDTO
{
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public TipoArchivo TipoArchivo { get; set; }
    public FormatoArchivo Formato { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public EstadoCarga Estado { get; set; }
    public int NumRegistros { get; set; }
    public int NumRegistrosValidos { get; set; }
    public int NumRegistrosError { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
}
