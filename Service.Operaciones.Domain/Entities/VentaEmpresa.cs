namespace Service.Operaciones.Domain.Entities;

public class VentaEmpresa : EntidadAuditoria
{
    public Guid IdVentaEmpresa { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    public DateTime FechaEmision { get; private set; }
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string CodigoTipoDocIdentidad { get; private set; } = string.Empty;
    public string NroDocIdentidad { get; private set; } = string.Empty;
    public decimal TotalCp { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal TipoCambio { get; private set; }

    private VentaEmpresa() { }

    public static VentaEmpresa Crear(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        int numeroLinea,
        DateTime fechaEmision,
        string codigoTipoCp,
        string serie,
        string numero,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string usuarioCreacion)
    {
        return new VentaEmpresa
        {
            IdVentaEmpresa = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
            FechaEmision = fechaEmision,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            Numero = numero,
            CodigoTipoDocIdentidad = codigoTipoDocIdentidad,
            NroDocIdentidad = nroDocIdentidad,
            TotalCp = totalCp,
            CodigoMoneda = codigoMoneda,
            TipoCambio = tipoCambio,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = usuarioCreacion
        };
    }

    public void ActualizarDatos(
        DateTime fechaEmision,
        string codigoTipoCp,
        string serie,
        string numero,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string usuarioModificacion)
    {
        FechaEmision = fechaEmision;
        CodigoTipoCp = codigoTipoCp;
        Serie = serie;
        Numero = numero;
        CodigoTipoDocIdentidad = codigoTipoDocIdentidad;
        NroDocIdentidad = nroDocIdentidad;
        TotalCp = totalCp;
        CodigoMoneda = codigoMoneda;
        TipoCambio = tipoCambio;
        ModificadoPor = usuarioModificacion;
        FechaModificacion = DateTime.UtcNow;
    }
}
