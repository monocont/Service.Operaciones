namespace Service.Operaciones.Domain.Entities;

public abstract class EntidadAuditoria
{
    public string? CreadoPor { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public bool Activo { get; set; }

    public void MarcarComoEliminado()
    {
        Activo = false;
        FechaModificacion = DateTime.UtcNow;
    }
}
