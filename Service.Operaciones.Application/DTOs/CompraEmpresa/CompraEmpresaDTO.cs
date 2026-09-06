namespace Service.Operaciones.Application.DTOs.CompraEmpresa;

public class CompraEmpresaDTO
{
    public Guid IdCompraEmpresa { get; set; }
    public Guid IdCarga { get; set; }
    public string EmpresaRuc { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public int NumeroLinea { get; set; }

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
