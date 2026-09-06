namespace Service.Operaciones.Application.DTOs.LimitesTributarios;

public class ConsumoLimiteDTO
{
    public decimal AcumuladoAnual { get; set; }
    public decimal AcumuladoBaseImponible { get; set; }
    public decimal? LimiteAnual { get; set; }
    public decimal? PorcentajeConsumo { get; set; }
    public decimal? SaldoDisponible { get; set; }
    public string Semaforo { get; set; } = "VERDE"; // VERDE | AMBAR | ROJO
    public string? AlertaMensaje { get; set; }
    public bool SinLimite { get; set; }
    public decimal? LimiteMensual { get; set; }
    public decimal? MaximoMensualRegistrado { get; set; }
}
