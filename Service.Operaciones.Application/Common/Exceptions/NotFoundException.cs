namespace Service.Operaciones.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"Recurso '{name}' con clave '{key}' no encontrado.") { }
}
