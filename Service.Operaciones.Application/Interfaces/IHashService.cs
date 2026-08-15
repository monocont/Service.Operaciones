namespace Service.Operaciones.Application.Interfaces;

public interface IHashService
{
    Task<string> CalcularSha256Async(Stream stream, CancellationToken cancellationToken);
}
