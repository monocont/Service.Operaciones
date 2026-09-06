namespace Service.Operaciones.Application.Interfaces;

public interface ICompraEmpresaParser
{
    Task<List<ResultadoParseoCompraEmpresaLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken);
}

public class ResultadoParseoCompraEmpresaLinea
{
    public int NumeroLinea { get; set; }
    public bool EsValido { get; set; }
    public string? ErrorMensaje { get; set; }
    public string? CampoError { get; set; }

    // Claves y Trazabilidad
    public string? CarSunat { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? AnioDocumento { get; set; }
    public string? Numero { get; set; }
    public string? NumeroFinal { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }

    // Bases Imponibles y Tributos
    public decimal? BiGravadoDg { get; set; }
    public decimal? IgvIpmDg { get; set; }
    public decimal? BiGravadoDgng { get; set; }
    public decimal? IgvIpmDgng { get; set; }
    public decimal? BiGravadoDng { get; set; }
    public decimal? IgvIpmDng { get; set; }
    public decimal? ValorAdqNg { get; set; }
    public decimal? MontoIsc { get; set; }
    public decimal? MontoIcbper { get; set; }
    public decimal? MontoOtrosTributos { get; set; }
    public decimal? TotalCp { get; set; }

    // Moneda y Tipo de Cambio
    public string? CodigoMoneda { get; set; }
    public decimal? TipoCambio { get; set; }

    // Documento de Referencia / Modificado
    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? CodDamDsi { get; set; }
    public string? NumeroCpModificado { get; set; }

    // Atributos Específicos
    public string? ClasifBssSss { get; set; }
    public string? IdProyectoOp { get; set; }
    public decimal? PorcPart { get; set; }
    public decimal? Imb { get; set; }
    public string? CarOrigIndEI { get; set; }
    public string? Detraccion { get; set; }
    public string? CodigoTipoNota { get; set; }
    public string? CodigoEstadoComprobante { get; set; }
    public string? Incal { get; set; }
    public string? CamposLibres { get; set; }
}
