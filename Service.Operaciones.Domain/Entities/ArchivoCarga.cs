using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Domain.Entities;

public class ArchivoCarga : EntidadAuditoria
{
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public TipoOperacion IdTipoOperacion { get; private set; }
    public FormatoArchivo Formato { get; private set; }
    public string NombreOriginal { get; private set; } = string.Empty;
    public string HashDocumento { get; private set; } = string.Empty;
    public int NumRegistros { get; private set; }
    public int NumRegistrosValidos { get; private set; }
    public int NumRegistrosError { get; private set; }
    public decimal TotalBaseImponible { get; private set; }
    public decimal TotalIgv { get; private set; }
    public decimal TotalGeneral { get; private set; }
    public EstadoCarga Estado { get; private set; }
    public string? Observaciones { get; private set; }

    private ArchivoCarga() { }

    public static ArchivoCarga Crear(
        string empresaRuc,
        string periodo,
        TipoOperacion tipoOperacion,
        FormatoArchivo formato,
        string nombreOriginal,
        string hashDocumento,
        string usuarioCreacion)
    {
        return new ArchivoCarga
        {
            IdCarga = Guid.NewGuid(),
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            IdTipoOperacion = tipoOperacion,
            Formato = formato,
            NombreOriginal = nombreOriginal,
            HashDocumento = hashDocumento,
            Estado = EstadoCarga.Procesando,
            TotalBaseImponible = 0,
            TotalIgv = 0,
            TotalGeneral = 0,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = usuarioCreacion
        };
    }

    public void ActualizarConteo(int numRegistros, int numValidos, int numErrores)
    {
        NumRegistros = numRegistros;
        NumRegistrosValidos = numValidos;
        NumRegistrosError = numErrores;
        Estado = EstadoCarga.Ok;
        FechaModificacion = DateTime.UtcNow;
    }

    public void ActualizarConteoYMontos(int numRegistros, int numValidos, int numErrores, decimal totalBaseImponible, decimal totalIgv, decimal totalGeneral)
    {
        NumRegistros = numRegistros;
        NumRegistrosValidos = numValidos;
        NumRegistrosError = numErrores;
        TotalBaseImponible = totalBaseImponible;
        TotalIgv = totalIgv;
        TotalGeneral = totalGeneral;
        Estado = EstadoCarga.Ok;
        FechaModificacion = DateTime.UtcNow;
    }

    public void MarcarCompleto()
    {
        Estado = EstadoCarga.Ok;
        FechaModificacion = DateTime.UtcNow;
    }

    public void MarcarError(string observaciones)
    {
        Estado = EstadoCarga.Error;
        Observaciones = observaciones;
        FechaModificacion = DateTime.UtcNow;
    }

    public void MarcarDuplicado(Guid idCargaExistente)
    {
        Estado = EstadoCarga.Duplicado;
        Observaciones = $"Archivo ya cargado anteriormente (IdCarga: {idCargaExistente})";
        FechaModificacion = DateTime.UtcNow;
    }
}
