namespace Service.Operaciones.Application.Interfaces;

public interface IEmpresaService
{
    Task<bool> ExisteEmpresaAsync(string ruc, CancellationToken cancellationToken);

    /// <summary>
    /// Determina si el usuario del JWT reenviado tiene acceso a la empresa del RUC
    /// (delegación de autorización en Service.Empresa).
    /// </summary>
    Task<bool> UsuarioTieneAccesoAsync(string ruc, CancellationToken cancellationToken);
}
