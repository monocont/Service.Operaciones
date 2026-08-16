using System.Text.Json.Serialization;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.DTOs.Carga;

public class ListarCargasDTO
{
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Formato { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public int NumRegistrosValidos { get; set; }
    public int NumRegistrosError { get; set; }
}
