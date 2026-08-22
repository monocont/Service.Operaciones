using Service.Operaciones.Application.Common.Exceptions;

namespace Service.Operaciones.Application.Interfaces;

/// <summary>
/// Valida que el usuario del JWT tenga acceso a la empresa (RUC).
/// Delega la decisión a Service.Empresa (dueño del dominio de empresas).
/// </summary>
public interface IAccesoEmpresaValidator
{
    /// <summary>
    /// Lanza UnauthorizedException si el usuario no tiene acceso a la empresa.
    /// Los administradores (rol ADMIN) tienen bypass.
    /// </summary>
    Task ValidarAccesoAsync(string ruc, CancellationToken cancellationToken);
}
