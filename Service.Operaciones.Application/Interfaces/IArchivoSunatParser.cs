using Service.Operaciones.Domain.Entities;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Application.Interfaces;

public interface IArchivoSunatParserFactory
{
    IArchivoSunatParser ObtenerParser(TipoOperacion tipoOperacion, FormatoArchivo formato);
}

public interface IArchivoSunatParser
{
    Task<List<ResultadoParseoLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken);
}

public class ResultadoParseoLinea
{
    public int NumeroLinea { get; set; }
    public bool EsValido { get; set; }
    public string? MensajeError { get; set; }
    public Dictionary<string, string> Campos { get; set; } = new();
}
