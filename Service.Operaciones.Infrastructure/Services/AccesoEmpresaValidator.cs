using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Common.Exceptions;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

/// <summary>
/// Valida el acceso del usuario a una empresa delegando en Service.Empresa
/// (dueño de la relación usuario-empresa). Fail-closed: ante cualquier fallo
/// de comunicación se deniega el acceso.
/// </summary>
public class AccesoEmpresaValidator : IAccesoEmpresaValidator
{
    private readonly IEmpresaService _empresaService;
    private readonly IUsuarioActualService _usuarioActualService;
    private readonly ILogger<AccesoEmpresaValidator> _logger;

    public AccesoEmpresaValidator(
        IEmpresaService empresaService,
        IUsuarioActualService usuarioActualService,
        ILogger<AccesoEmpresaValidator> logger)
    {
        _empresaService = empresaService;
        _usuarioActualService = usuarioActualService;
        _logger = logger;
    }

    public async Task ValidarAccesoAsync(string ruc, CancellationToken cancellationToken)
    {
        if (_usuarioActualService.EsAdmin())
            return;

        var tieneAcceso = await _empresaService.UsuarioTieneAccesoAsync(ruc, cancellationToken);
        if (!tieneAcceso)
        {
            _logger.LogWarning("Acceso denegado al RUC {Ruc} para el usuario del token.", ruc);
            throw new UnauthorizedException("No tiene acceso a la empresa indicada.");
        }
    }
}
