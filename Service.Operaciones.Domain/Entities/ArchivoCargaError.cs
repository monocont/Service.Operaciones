using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Domain.Entities;

public class ArchivoCargaError
{
    public Guid IdError { get; private set; }
    public Guid IdCarga { get; private set; }
    public int NumeroLinea { get; private set; }
    public TipoErrorCarga TipoError { get; private set; }
    public string? CampoError { get; private set; }
    public string? ValorLectura { get; private set; }
    public string Mensaje { get; private set; } = string.Empty;
    public SeveridadError Severidad { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    private ArchivoCargaError() { }

    public static ArchivoCargaError Crear(
        Guid idCarga,
        int numeroLinea,
        TipoErrorCarga tipoError,
        string mensaje,
        string? campoError = null,
        string? valorLectura = null,
        SeveridadError severidad = SeveridadError.Error)
    {
        return new ArchivoCargaError
        {
            IdError = Guid.NewGuid(),
            IdCarga = idCarga,
            NumeroLinea = numeroLinea,
            TipoError = tipoError,
            CampoError = campoError,
            ValorLectura = valorLectura,
            Mensaje = mensaje,
            Severidad = severidad,
            FechaRegistro = DateTime.UtcNow
        };
    }
}
