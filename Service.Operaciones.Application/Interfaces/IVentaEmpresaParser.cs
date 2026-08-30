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

    public DateTime? FechaEmision { get; set; }
    public string? CodigoTipoCp { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? CodigoTipoDocIdentidad { get; set; }
    public string? NroDocIdentidad { get; set; }
    public decimal? TotalCp { get; set; }
    public string? CodigoMoneda { get; set; }
    public decimal? TipoCambio { get; set; }
}
