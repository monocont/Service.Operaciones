using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

public class ArchivoSunatParserFactory : IArchivoSunatParserFactory
{
    public IArchivoSunatParser ObtenerParser(TipoOperacion tipoOperacion, FormatoArchivo formato)
    {
        return (tipoOperacion, formato) switch
        {
            (TipoOperacion.CompraSire, FormatoArchivo.Txt) => new CompraTxtParser(),
            (TipoOperacion.CompraSire, FormatoArchivo.Csv) => new CompraCsvParser(),
            (TipoOperacion.VentaSire, FormatoArchivo.Txt) => new VentaTxtParser(),
            (TipoOperacion.VentaSire, FormatoArchivo.Csv) => new VentaCsvParser(),
            _ => throw new ArgumentException($"No existe parser para combinación {tipoOperacion}/{formato}")
        };
    }
}

public abstract class ArchivoSunatParserBase
{
    protected char Delimitador { get; init; }

    protected string[] DividirLinea(string linea)
    {
        return linea.Split(Delimitador);
    }

    protected static string? Limpiar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }
        return valor.Trim();
    }
}
