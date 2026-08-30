using MediatR;

namespace Service.Operaciones.Application.Commands.Venta.ActualizarVentasEmpresa;

public class ActualizarVentasEmpresaCommand : IRequest<ActualizarVentasEmpresaResultadoDTO>
{
    public Guid IdCarga { get; set; }
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<CrearVentaEmpresaRegistroDTO> Nuevos { get; set; } = new();
    public List<ModificarVentaEmpresaRegistroDTO> Modificados { get; set; } = new();
    public string Usuario { get; set; } = "sistema";
}

public class CrearVentaEmpresaRegistroDTO
{
    public DateTime FechaEmision { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public decimal TotalCp { get; set; }
    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;
}

public class ModificarVentaEmpresaRegistroDTO
{
    public Guid IdVentaEmpresa { get; set; }
    public DateTime FechaEmision { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public decimal TotalCp { get; set; }
    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;
}

public class ActualizarVentasEmpresaResultadoDTO
{
    public Guid IdCarga { get; set; }
    public int NumRegistros { get; set; }
    public int NumRegistrosValidos { get; set; }
    public int NumRegistrosError { get; set; }
    public decimal TotalGeneral { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
