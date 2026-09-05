using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Operaciones.Application.Commands.Carga.CargarArchivoCompras;
using Service.Operaciones.Application.Commands.Carga.CargarArchivoVentas;
using Service.Operaciones.Application.DTOs.Carga;
using Service.Operaciones.Application.Queries.Carga.ListarCargas;
using Service.Operaciones.Application.Queries.Carga.ObtenerCargaPorId;
using Service.Operaciones.Application.Queries.Carga.ObtenerErroresCarga;
using Service.Operaciones.Application.Queries.Compra.ListarCompras;
using Service.Operaciones.Application.Queries.Venta.ListarVentas;
using Service.Operaciones.Domain.Enums;
using System.Security.Claims;

namespace Service.Operaciones.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class CargaController : ControllerBase
{
    private readonly IMediator _mediator;

    public CargaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Carga un archivo SUNAT de COMPRAS en formato .txt o .csv
    /// </summary>
    [HttpPost("cargas/compras")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> CargarArchivoCompras(
        [FromForm] CargarArchivoRequest request,
        CancellationToken cancellationToken)
    {
        var command = CrearCommandCompras(request);
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Carga un archivo SUNAT de VENTAS en formato .txt o .csv
    /// </summary>
    [HttpPost("cargas/ventas")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> CargarArchivoVentas(
        [FromForm] CargarArchivoRequest request,
        CancellationToken cancellationToken)
    {
        var command = CrearCommandVentas(request);
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista las cargas de archivos SUNAT realizadas (ordenadas por periodo desc y activo = true)
    /// </summary>
    [HttpGet("cargas")]
    public async Task<IActionResult> ListarCargas(
        [FromQuery] string empresaRuc,
        [FromQuery] string? tipoArchivo,
        [FromQuery] string? tipoOperacion,
        [FromQuery] int? idTipoOperacion,
        [FromQuery] string? periodo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        TipoOperacion tipoOpEnum;

        if (idTipoOperacion.HasValue && Enum.IsDefined(typeof(TipoOperacion), idTipoOperacion.Value))
        {
            tipoOpEnum = (TipoOperacion)idTipoOperacion.Value;
        }
        else
        {
            var opStr = tipoOperacion ?? tipoArchivo;
            if (string.IsNullOrWhiteSpace(opStr))
            {
                return BadRequest(new { success = false, message = "El parámetro tipoOperacion o idTipoOperacion es obligatorio." });
            }

            // Normalización para compatibilidad con nombres y códigos
            if (int.TryParse(opStr, out var opInt) && Enum.IsDefined(typeof(TipoOperacion), opInt))
            {
                tipoOpEnum = (TipoOperacion)opInt;
            }
            else if (string.Equals(opStr, "Ventas", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(opStr, "VentaSire", StringComparison.OrdinalIgnoreCase))
            {
                tipoOpEnum = TipoOperacion.VentaSire;
            }
            else if (string.Equals(opStr, "VentasEmpresa", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(opStr, "VentaEmpresa", StringComparison.OrdinalIgnoreCase))
            {
                tipoOpEnum = TipoOperacion.VentaEmpresa;
            }
            else if (string.Equals(opStr, "Compras", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(opStr, "CompraSire", StringComparison.OrdinalIgnoreCase))
            {
                tipoOpEnum = TipoOperacion.CompraSire;
            }
            else if (string.Equals(opStr, "ComprasEmpresa", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(opStr, "CompraEmpresa", StringComparison.OrdinalIgnoreCase))
            {
                tipoOpEnum = TipoOperacion.CompraEmpresa;
            }
            else if (Enum.TryParse<TipoOperacion>(opStr, ignoreCase: true, out var parsedEnum))
            {
                tipoOpEnum = parsedEnum;
            }
            else
            {
                return BadRequest(new { success = false, message = "El tipo de operación especificado no es válido." });
            }
        }

        var query = new ListarCargasQuery
        {
            EmpresaRuc = empresaRuc,
            TipoOperacion = tipoOpEnum,
            Periodo = periodo,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene el detalle de una carga específica
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}")]
    public async Task<IActionResult> ObtenerCargaPorId([FromRoute] Guid idCarga, CancellationToken cancellationToken)
    {
        var query = new ObtenerCargaPorIdQuery { IdCarga = idCarga };
        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene los errores de una carga específica
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}/errores")]
    public async Task<IActionResult> ObtenerErroresCarga(
        [FromRoute] Guid idCarga,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new ObtenerErroresCargaQuery
        {
            IdCarga = idCarga,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista todas las compras asociadas a una carga sin paginación
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}/compras")]
    public async Task<IActionResult> ListarCompras(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var query = new ListarComprasQuery
        {
            IdCarga = idCarga
        };

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista todas las ventas asociadas a una carga sin paginación
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}/ventas")]
    public async Task<IActionResult> ListarVentas(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var query = new ListarVentasQuery
        {
            IdCarga = idCarga
        };

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Carga un archivo de VENTAS DE EMPRESA en formato .xlsx, .xls o .csv
    /// </summary>
    [HttpPost("cargas/ventas-empresa")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> CargarArchivoVentasEmpresa(
        [FromForm] CargarArchivoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Service.Operaciones.Application.Commands.Carga.CargarArchivoVentasEmpresa.CargarArchivoVentasEmpresaCommand
        {
            ArchivoStream = request.Archivo.OpenReadStream(),
            NombreArchivo = request.Archivo.FileName,
            EmpresaRuc = request.EmpresaRuc,
            Periodo = request.Periodo,
            Usuario = ObtenerUsuario()
        };
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista todas las ventas de empresa asociadas a una carga sin paginación
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}/ventas-empresa")]
    public async Task<IActionResult> ListarVentasEmpresa(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var query = new Service.Operaciones.Application.Queries.Venta.ListarVentasEmpresa.ListarVentasEmpresaQuery
        {
            IdCarga = idCarga
        };

        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Actualiza y sincroniza los comprobantes de ventas de la empresa de una carga y revalida observaciones
    /// </summary>
    [HttpPost("cargas/{idCarga:guid}/actualizar-ventas-empresa")]
    public async Task<IActionResult> ActualizarVentasEmpresa(
        [FromRoute] Guid idCarga,
        [FromBody] ActualizarVentasEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa.ActualizarVentasEmpresaCommand
        {
            IdCarga = idCarga,
            EliminadosIds = request?.EliminadosIds ?? new List<Guid>(),
            Nuevos = request?.Nuevos ?? new List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa.CrearVentaEmpresaRegistroDTO>(),
            Modificados = request?.Modificados ?? new List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa.ModificarVentaEmpresaRegistroDTO>(),
            Usuario = ObtenerUsuario()
        };

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Actualiza y sincroniza los comprobantes de ventas de una carga (eliminaciones, adiciones futuras) y revalida observaciones
    /// </summary>
    [HttpPost("cargas/{idCarga:guid}/actualizar-ventas")]
    public async Task<IActionResult> ActualizarVentas(
        [FromRoute] Guid idCarga,
        [FromBody] ActualizarVentasRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Venta.ActualizarVentas.ActualizarVentasCommand
        {
            IdCarga = idCarga,
            EliminadosIds = request?.EliminadosIds ?? new List<Guid>(),
            Nuevos = request?.Nuevos ?? new List<Service.Operaciones.Application.Commands.Venta.ActualizarVentas.CrearVentaRegistroDTO>(),
            Modificados = request?.Modificados ?? new List<Service.Operaciones.Application.Commands.Venta.ActualizarVentas.ModificarVentaRegistroDTO>(),
            Usuario = ObtenerUsuario()
        };

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("cargas/{idCarga:guid}/actualizar-compras")]
    public async Task<IActionResult> ActualizarCompras(
        [FromRoute] Guid idCarga,
        [FromBody] ActualizarComprasRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Compra.ActualizarCompras.ActualizarComprasCommand
        {
            IdCarga = idCarga,
            EliminadosIds = request?.EliminadosIds ?? new List<Guid>(),
            Nuevos = request?.Nuevos ?? new List<Service.Operaciones.Application.Commands.Compra.ActualizarCompras.CrearCompraRegistroDTO>(),
            Modificados = request?.Modificados ?? new List<Service.Operaciones.Application.Commands.Compra.ActualizarCompras.ModificarCompraRegistroDTO>(),
            Usuario = ObtenerUsuario()
        };

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Ejecuta el proceso de match y cruce de datos entre Ventas SIRE y Ventas de la Empresa para un periodo
    /// </summary>
    [HttpPost("ventas/ejecutar-match")]
    public async Task<IActionResult> EjecutarMatchVentas(
        [FromBody] EjecutarMatchVentasRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Venta.EjecutarMatchVentas.EjecutarMatchVentasCommand(
            request.EmpresaRuc,
            request.Periodo,
            ObtenerUsuario()
        );

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Re-ejecuta el proceso de match: elimina todos los registros de match y observaciones previas del periodo y vuelve a cruzar SIRE vs Empresa
    /// </summary>
    [HttpPost("ventas/re-ejecutar-match")]
    public async Task<IActionResult> ReejecutarMatchVentas(
        [FromBody] EjecutarMatchVentasRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Venta.EjecutarMatchVentas.EjecutarMatchVentasCommand(
            request.EmpresaRuc,
            request.Periodo,
            ObtenerUsuario()
        );

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista los comprobantes resultantes del match de ventas para una carga específica
    /// </summary>
    [HttpGet("cargas/{idCarga:guid}/ventas-match")]
    public async Task<IActionResult> ListarVentasMatch(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var query = new Service.Operaciones.Application.Queries.Venta.ListarVentasMatch.ListarVentasMatchQuery(idCarga);
        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Actualiza y sincroniza los comprobantes de match de ventas de una carga y revalida discrepancias y observaciones
    /// </summary>
    [HttpPost("cargas/{idCarga:guid}/actualizar-ventas-match")]
    public async Task<IActionResult> ActualizarVentasMatch(
        [FromRoute] Guid idCarga,
        [FromBody] ActualizarVentasMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Venta.ActualizarVentasMatch.ActualizarVentasMatchCommand
        {
            IdCarga = idCarga,
            EliminadosIds = request.EliminadosIds,
            Nuevos = request.Nuevos,
            Modificados = request.Modificados,
            Usuario = ObtenerUsuario()
        };

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Elimina físicamente los registros y el archivo de match de ventas de un periodo
    /// </summary>
    [HttpDelete("ventas/match/{idCarga:guid}")]
    public async Task<IActionResult> EliminarMatchVentas(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Carga.EliminarArchivoCarga.EliminarArchivoCargaCommand(idCarga);
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Elimina físicamente una carga de archivos y todos sus registros asociados (errores, ventas, match, etc.)
    /// </summary>
    [HttpDelete("cargas/{idCarga:guid}")]
    public async Task<IActionResult> EliminarCarga(
        [FromRoute] Guid idCarga,
        CancellationToken cancellationToken = default)
    {
        var command = new Service.Operaciones.Application.Commands.Carga.EliminarArchivoCarga.EliminarArchivoCargaCommand(idCarga);
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "sistema";
    }

    private CargarArchivoComprasCommand CrearCommandCompras(CargarArchivoRequest request)
    {
        return new CargarArchivoComprasCommand
        {
            ArchivoStream = request.Archivo.OpenReadStream(),
            NombreArchivo = request.Archivo.FileName,
            EmpresaRuc = request.EmpresaRuc,
            Periodo = request.Periodo,
            Usuario = ObtenerUsuario()
        };
    }

    private CargarArchivoVentasCommand CrearCommandVentas(CargarArchivoRequest request)
    {
        return new CargarArchivoVentasCommand
        {
            ArchivoStream = request.Archivo.OpenReadStream(),
            NombreArchivo = request.Archivo.FileName,
            EmpresaRuc = request.EmpresaRuc,
            Periodo = request.Periodo,
            Usuario = ObtenerUsuario()
        };
    }
}

public class CargarArchivoRequest
{
    public required IFormFile Archivo { get; set; }
    public required string EmpresaRuc { get; set; }
    public required string Periodo { get; set; }
}

public class ActualizarVentasRequest
{
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentas.CrearVentaRegistroDTO> Nuevos { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentas.ModificarVentaRegistroDTO> Modificados { get; set; } = new();
}

public class ActualizarVentasEmpresaRequest
{
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa.CrearVentaEmpresaRegistroDTO> Nuevos { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa.ModificarVentaEmpresaRegistroDTO> Modificados { get; set; } = new();
}

public class ActualizarVentasMatchRequest
{
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasMatch.CrearVentaMatchRegistroDTO> Nuevos { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Venta.ActualizarVentasMatch.ModificarVentaMatchRegistroDTO> Modificados { get; set; } = new();
}

public class ActualizarComprasRequest
{
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Compra.ActualizarCompras.CrearCompraRegistroDTO> Nuevos { get; set; } = new();
    public List<Service.Operaciones.Application.Commands.Compra.ActualizarCompras.ModificarCompraRegistroDTO> Modificados { get; set; } = new();
}

public class EjecutarMatchVentasRequest
{
    public required string EmpresaRuc { get; set; }
    public required string Periodo { get; set; }
}
