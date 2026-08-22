using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

public class UsuarioActualService : IUsuarioActualService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? ObtenerIdUsuario()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    public bool EsAdmin()
        => _httpContextAccessor.HttpContext?.User.IsInRole("ADMIN") ?? false;
}
