using MediatR;

namespace Service.Operaciones.Application.Commands.Compra.ActualizarComprasEmpresa;

public class ActualizarComprasEmpresaCommand : IRequest<ActualizarComprasEmpresaResponseDTO>
{
    public Guid IdCarga { get; set; }
    public List<Guid> EliminadosIds { get; set; } = new();
    public List<CrearCompraEmpresaRegistroDTO> Nuevos { get; set; } = new();
    public List<ModificarCompraEmpresaRegistroDTO> Modificados { get; set; } = new();
    public string Usuario { get; set; } = "sistema";
}

public class CrearCompraEmpresaRegistroDTO
{
    public string? CarSunat { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string? AnioDocumento { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? NumeroFinal { get; set; }
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;

    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal BiGravadoDgng { get; set; }
    public decimal IgvIpmDgng { get; set; }
    public decimal BiGravadoDng { get; set; }
    public decimal IgvIpmDng { get; set; }
    public decimal ValorAdqNg { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }

    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? CodDamDsi { get; set; }
    public string? NumeroCpModificado { get; set; }

    public string? ClasifBssSss { get; set; }
    public string? IdProyectoOp { get; set; }
    public decimal? PorcPart { get; set; }
    public decimal Imb { get; set; }
    public string? CarOrigIndEI { get; set; }
    public string? Detraccion { get; set; }
    public string? CodigoTipoNota { get; set; }
    public string CodigoEstadoComprobante { get; set; } = "1";
    public string? Incal { get; set; }
    public string? CamposLibres { get; set; }
}

public class ModificarCompraEmpresaRegistroDTO
{
    public Guid IdCompraEmpresa { get; set; }
    public string? CarSunat { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string CodigoTipoCp { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string? AnioDocumento { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? NumeroFinal { get; set; }
    public string CodigoTipoDocIdentidad { get; set; } = string.Empty;
    public string NroDocIdentidad { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;

    public decimal BiGravadoDg { get; set; }
    public decimal IgvIpmDg { get; set; }
    public decimal BiGravadoDgng { get; set; }
    public decimal IgvIpmDgng { get; set; }
    public decimal BiGravadoDng { get; set; }
    public decimal IgvIpmDng { get; set; }
    public decimal ValorAdqNg { get; set; }
    public decimal MontoIsc { get; set; }
    public decimal MontoIcbper { get; set; }
    public decimal MontoOtrosTributos { get; set; }
    public decimal TotalCp { get; set; }

    public string CodigoMoneda { get; set; } = "PEN";
    public decimal TipoCambio { get; set; } = 1.0000m;

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? CodDamDsi { get; set; }
    public string? NumeroCpModificado { get; set; }

    public string? ClasifBssSss { get; set; }
    public string? IdProyectoOp { get; set; }
    public decimal? PorcPart { get; set; }
    public decimal Imb { get; set; }
    public string? CarOrigIndEI { get; set; }
    public string? Detraccion { get; set; }
    public string? CodigoTipoNota { get; set; }
    public string CodigoEstadoComprobante { get; set; } = "1";
    public string? Incal { get; set; }
    public string? CamposLibres { get; set; }
}

public class ActualizarComprasEmpresaResponseDTO
{
    public Guid IdCarga { get; set; }
    public int NumRegistros { get; set; }
    public int NumRegistrosValidos { get; set; }
    public int NumRegistrosError { get; set; }
    public decimal TotalGeneral { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
