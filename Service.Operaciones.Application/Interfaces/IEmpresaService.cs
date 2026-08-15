namespace Service.Operaciones.Application.Interfaces;

public interface IEmpresaService
{
    Task<bool> ExisteEmpresaAsync(string ruc, CancellationToken cancellationToken);
}
