namespace Service.Operaciones.Application.Interfaces;

/// <summary>
/// Identidad del usuario actual extraída del JWT (Autorización multi-tenant).
/// </summary>
public interface IUsuarioActualService
{
    Guid? ObtenerIdUsuario();
    bool EsAdmin();
}
