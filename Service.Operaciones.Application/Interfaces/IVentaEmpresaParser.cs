namespace Service.Operaciones.Application.Interfaces;

public interface IVentaEmpresaParser
{
    Task<List<ResultadoParseoVentaEmpresaLinea>> ParsearAsync(Stream stream, CancellationToken cancellationToken);
}

public class ResultadoParseoVentaEmpresaLinea
{
    public int NumeroLinea { get; set; }
    public bool EsValido { get; set; }
    public string? ErrorMensaje { get; set; }
    public string? CampoError { get; set; }

    public string? CarSunat { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? NumeroFinal { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public string? RazonSocial { get; set; }

    public decimal? ValorFacturadoExportacion { get; set; }
    public decimal? BiGravada { get; set; }
    public decimal? DescuentoBi { get; set; }
    public decimal? IgvIpm { get; set; }
    public decimal? DescuentoIgv { get; set; }
    public decimal? MontoExonerado { get; set; }
    public decimal? MontoInafecto { get; set; }
    public decimal? MontoIsc { get; set; }
    public decimal? BiGravadaIvap { get; set; }
    public decimal? MontoIvap { get; set; }
    public decimal? MontoIcbper { get; set; }
    public decimal? MontoOtrosTributos { get; set; }
    public decimal? TotalCp { get; set; }

    public string? CodigoMoneda { get; set; }
    public decimal? TipoCambio { get; set; }

    public DateTime? FechaEmisionDocModificado { get; set; }
    public string? CodigoTipoCpModificado { get; set; }
    public string? SerieCpModificado { get; set; }
    public string? NumeroCpModificado { get; set; }
    public string? CodigoEstadoComprobante { get; set; }
    public string? CodigoTipoNota { get; set; }
    public string? TipoOperacion { get; set; }
    public string? CamposLibres { get; set; }
}
