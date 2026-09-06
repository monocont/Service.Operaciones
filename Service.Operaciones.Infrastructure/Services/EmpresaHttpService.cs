using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

/// <summary>
/// Cliente HTTP hacia Service.Empresa. Reenvía el JWT del usuario original
/// (propagación de identidad) para que Empresa pueda autorizar por tenant.
/// Fail-closed: ante fallo de comunicación se deniega (nunca se asume acceso).
/// </summary>
public class EmpresaHttpService : IEmpresaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmpresaHttpService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmpresaHttpService(HttpClient httpClient, ILogger<EmpresaHttpService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> ExisteEmpresaAsync(string ruc, CancellationToken cancellationToken)
    {
        var response = await EnviarAsync($"api/v1/empresa/ruc/{ruc}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Service.Empresa respondió {Status} al verificar RUC {Ruc}; se asume que no existe/accessible.", response.StatusCode, ruc);
            return false;
        }

        return true;
    }

    public async Task<bool> UsuarioTieneAccesoAsync(string ruc, CancellationToken cancellationToken)
    {
        try
        {
            var response = await EnviarAsync($"api/v1/empresa/{ruc}/acceso", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Service.Empresa respondió {Status} al validar acceso al RUC {Ruc}.", response.StatusCode, ruc);
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;

            // La respuesta viene envuelta por el middleware de Empresa: { success, data: { tieneAcceso, rol } }
            JsonElement origen = root;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                origen = data;

            if (origen.TryGetProperty("tieneAcceso", out var tieneAcceso))
                return tieneAcceso.ValueKind == JsonValueKind.True;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar acceso al RUC {Ruc} en Service.Empresa. Acceso denegado (fail-closed).", ruc);
            return false;
        }
    }

    public async Task<List<Service.Operaciones.Application.DTOs.LimitesTributarios.EmpresaConLimitesHttpDTO>> ObtenerEmpresasConLimitesAsync(int anio, CancellationToken cancellationToken)
    {
        try
        {
            var response = await EnviarAsync($"api/v1/empresa/usuario-empresas-limites?anio={anio}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Service.Empresa respondió {Status} al obtener empresas y límites para el año {Anio}.", response.StatusCode, anio);
                return new List<Service.Operaciones.Application.DTOs.LimitesTributarios.EmpresaConLimitesHttpDTO>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;

            JsonElement arrayElement = root;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                arrayElement = data;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<Service.Operaciones.Application.DTOs.LimitesTributarios.EmpresaConLimitesHttpDTO>>(arrayElement.GetRawText(), options);
            return items ?? new List<Service.Operaciones.Application.DTOs.LimitesTributarios.EmpresaConLimitesHttpDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar empresas y límites en Service.Empresa para el año {Anio}.", anio);
            return new List<Service.Operaciones.Application.DTOs.LimitesTributarios.EmpresaConLimitesHttpDTO>();
        }
    }

    /// <summary>
    /// Envía la petición propagando el Authorization header del usuario original.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        return await _httpClient.SendAsync(request, cancellationToken);
    }
}
