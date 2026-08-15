using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.DTOs.Carga;

public class ObtenerErroresCargaDTO
{
    public Guid IdError { get; set; }
    public Guid IdCarga { get; set; }
    public int NumeroLinea { get; set; }
    public TipoErrorCarga TipoError { get; set; }
    public string? CampoError { get; set; }
    public string? ValorLectura { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public SeveridadError Severidad { get; set; }
    public DateTime FechaRegistro { get; set; }
}
