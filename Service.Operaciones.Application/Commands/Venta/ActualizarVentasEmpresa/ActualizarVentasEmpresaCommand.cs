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
    public string? CarSunat { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? NumeroFinal { get; set; }
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;

    public decimal ValorFacturadoExportacion { get; set; }
    public decimal BiGravada { get; set; }
    public decimal DescuentoBi { get; set; }
    public decimal IgvIpm { get; set; }
    public decimal DescuentoIgv { get; set; }
    public decimal MontoExonerado { get; set; }
    public decimal MontoInafecto { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal BiGravadaIvap { get; set; }
    public decimal MontoIvap { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }

    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? NumeroCpModificado { get; set; }
    public string CodigoEstadoComprobante { get; set; } = "1";
    public string? CodigoTipoNota { get; set; }
    public string? TipoOperacion { get; set; }
    public string? CamposLibres { get; set; }
}

public class ModificarVentaEmpresaRegistroDTO
{
    public Guid IdVentaEmpresa { get; set; }
    public string? CarSunat { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? NumeroFinal { get; set; }
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;

    public decimal ValorFacturadoExportacion { get; set; }
    public decimal BiGravada { get; set; }
    public decimal DescuentoBi { get; set; }
    public decimal IgvIpm { get; set; }
    public decimal DescuentoIgv { get; set; }
    public decimal MontoExonerado { get; set; }
    public decimal MontoInafecto { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal BiGravadaIvap { get; set; }
    public decimal MontoIvap { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }

    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? NumeroCpModificado { get; set; }
    public string CodigoEstadoComprobante { get; set; } = "1";
    public string? CodigoTipoNota { get; set; }
    public string? TipoOperacion { get; set; }
    public string? CamposLibres { get; set; }
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
