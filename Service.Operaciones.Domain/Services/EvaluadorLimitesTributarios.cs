namespace Service.Operaciones.Domain.Services;

public class ResultadoEvaluacionLimite
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

public static class EvaluadorLimitesTributarios
{
    public static ResultadoEvaluacionLimite Evaluar(
        string codigoRegimen,
        string regimenDescripcion,
        decimal acumuladoTotal,
        decimal acumuladoBaseImponible,
        decimal maximoMensual,
        decimal? limiteAnual,
        decimal? limiteMensual,
        bool sinLimite,
        bool esVenta)
    {
        var tipoOperacionStr = esVenta ? "ventas" : "compras";

        if (sinLimite || (!limiteAnual.HasValue && !limiteMensual.HasValue))
        {
            return new ResultadoEvaluacionLimite
            {
                AcumuladoAnual = acumuladoTotal,
                AcumuladoBaseImponible = acumuladoBaseImponible,
                LimiteAnual = null,
                PorcentajeConsumo = null,
                SaldoDisponible = null,
                Semaforo = "VERDE",
                AlertaMensaje = null,
                SinLimite = true,
                LimiteMensual = null,
                MaximoMensualRegistrado = maximoMensual
            };
        }

        var limAnual = limiteAnual ?? 0m;
        var porcentaje = limAnual > 0 ? Math.Round((acumuladoTotal / limAnual) * 100m, 2) : 0m;
        var saldo = limAnual > 0 ? Math.Max(0m, limAnual - acumuladoTotal) : 0m;

        string semaforo;
        string? alerta = null;

        if (porcentaje >= 100m)
        {
            semaforo = "ROJO";
            alerta = $"¡Límite superado! La empresa ha alcanzado el {porcentaje:N1}% de su tope legal de {tipoOperacionStr} (S/ {limAnual:N2}) en el {regimenDescripcion}. Requiere cambio de régimen tributario.";
        }
        else if (porcentaje >= 95m)
        {
            semaforo = "ROJO";
            alerta = $"¡Alerta Crítica! La empresa ha consumido el {porcentaje:N1}% de su límite anual de {tipoOperacionStr} (S/ {limAnual:N2}) en el {regimenDescripcion}. Saldo restante: S/ {saldo:N2}.";
        }
        else if (porcentaje >= 80m)
        {
            semaforo = "AMBAR";
            alerta = $"Atención Preventiva: La empresa ha consumido el {porcentaje:N1}% de su límite anual de {tipoOperacionStr} (S/ {limAnual:N2}) en el {regimenDescripcion}. Saldo disponible: S/ {saldo:N2}.";
        }
        else
        {
            semaforo = "VERDE";
        }

        // Validación adicional de tope mensual (por ejemplo en Nuevo RUS)
        if (limiteMensual.HasValue && limiteMensual.Value > 0 && maximoMensual > limiteMensual.Value)
        {
            if (semaforo == "VERDE")
            {
                semaforo = "AMBAR";
                alerta = $"Atención: Se registró un mes con S/ {maximoMensual:N2}, superando el tope mensual permitido de S/ {limiteMensual.Value:N2} para el {regimenDescripcion}.";
            }
        }

        return new ResultadoEvaluacionLimite
        {
            AcumuladoAnual = acumuladoTotal,
            AcumuladoBaseImponible = acumuladoBaseImponible,
            LimiteAnual = limAnual > 0 ? limAnual : null,
            PorcentajeConsumo = limAnual > 0 ? porcentaje : null,
            SaldoDisponible = limAnual > 0 ? saldo : null,
            Semaforo = semaforo,
            AlertaMensaje = alerta,
            SinLimite = false,
            LimiteMensual = limiteMensual,
            MaximoMensualRegistrado = maximoMensual
        };
    }
}
