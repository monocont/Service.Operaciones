namespace Service.Operaciones.Domain.Entities;

public class TipoOperacionCatalogo
{
    public int IdTipoOperacion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public bool Activo { get; set; } = true;
}
