using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Domain.Enums;

namespace Service.Operaciones.Infrastructure.Services.Parsers;

public class ArchivoSunatParserFactory : IArchivoSunatParserFactory
{
    public IArchivoSunatParser ObtenerParser(TipoArchivo tipoArchivo, FormatoArchivo formato)
    {
        return (tipoArchivo, formato) switch
        {
            (TipoArchivo.Compras, FormatoArchivo.Txt) => new CompraTxtParser(),
            (TipoArchivo.Compras, FormatoArchivo.Csv) => new CompraCsvParser(),
            (TipoArchivo.Ventas, FormatoArchivo.Txt) => new VentaTxtParser(),
            (TipoArchivo.Ventas, FormatoArchivo.Csv) => new VentaCsvParser(),
            _ => throw new ArgumentException($"No existe parser para combinación {tipoArchivo}/{formato}")
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
