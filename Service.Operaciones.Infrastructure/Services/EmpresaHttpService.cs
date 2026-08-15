using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Service.Operaciones.Application.Interfaces;

namespace Service.Operaciones.Infrastructure.Services;

public class EmpresaHttpService : IEmpresaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmpresaHttpService> _logger;

    public EmpresaHttpService(HttpClient httpClient, ILogger<EmpresaHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ExisteEmpresaAsync(string ruc, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/empresa/ruc/{ruc}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
             || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Service.Empresa denegó acceso verificación RUC {Ruc}. Status: {Status}", ruc, response.StatusCode);
                return true; // Asumir válido para no bloquear la carga si auth falla
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar empresa {Ruc} en Service.Empresa", ruc);
            // En caso de fallo de comunicación, asumir válido para no bloquear la carga (mejorable)
            return true;
        }
    }
}
