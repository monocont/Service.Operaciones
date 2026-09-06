using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Operaciones.Application.Queries.LimitesTributarios;

namespace Service.Operaciones.API.Controllers;

[ApiController]
[Route("api/v1/operaciones/limites-tributarios")]
[Authorize]
public class LimitesTributariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public LimitesTributariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene el tablero de control de límites tributarios (SUNAT) de compras y ventas
    /// para todas las empresas asociadas al usuario autenticado en un año específico.
    /// </summary>
    /// <param name="anio">Año fiscal a evaluar (Ej: 2026). Si no se envía, toma el año actual.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Listado de empresas con acumulados, topes de régimen, porcentajes de consumo y semáforos.</returns>
    [HttpGet("empresas")]
    public async Task<IActionResult> ObtenerLimitesEmpresas(
        [FromQuery] int? anio,
        CancellationToken cancellationToken = default)
    {
        var anioConsulta = anio.HasValue && anio.Value > 0 ? anio.Value : DateTime.UtcNow.Year;
        var query = new ObtenerLimitesTributariosEmpresasQuery(anioConsulta);

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }
}
