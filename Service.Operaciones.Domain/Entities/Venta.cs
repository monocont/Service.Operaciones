namespace Service.Operaciones.Domain.Entities;

public class Venta : EntidadAuditoria
{
    public Guid IdVenta { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    public string CarSunat { get; private set; } = string.Empty;
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string? NumeroFinal { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaVctoPago { get; private set; }

    public string CodigoTipoDocIdentidad { get; private set; } = string.Empty;
    public string NroDocIdentidad { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;

    public decimal ValorFactExp { get; private set; }
    public decimal BiGravada { get; private set; }
    public decimal DsctoBi { get; private set; }
    public decimal IgvIpm { get; private set; }
    public decimal DsctoIgvIpm { get; private set; }
    public decimal MontoExonerado { get; private set; }
    public decimal MontoInafecto { get; private set; }
    public decimal Isc { get; private set; }
    public decimal BiGravIvap { get; private set; }
    public decimal Ivap { get; private set; }
    public decimal Icbper { get; private set; }
    public decimal OtrosTributos { get; private set; }
    public decimal TotalCp { get; private set; }

    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal TipoCambio { get; private set; }

    public DateTime? FechaEmisionDocModif { get; private set; }
    public string? TipoCpModificado { get; private set; }
    public string? SerieCpModificado { get; private set; }
    public string? NroCpModificado { get; private set; }

    public string? IdProyectoOpAttr { get; private set; }
    public decimal ValorFobEmbar { get; private set; }
    public decimal ValorOpGratuitas { get; private set; }
    public string? TipoOperacion { get; private set; }
    public string? DamCp { get; private set; }

    public string? CodigoTipoNota { get; private set; }
    public string CodigoEstadoComprobante { get; private set; } = string.Empty;
    public string? CamposLibres { get; private set; }

    private Venta() { }

    public static Venta Crear(
        string empresaRuc,
        string periodo,
        Guid idCarga,
        string carSunat,
        string codigoTipoCp,
        string serie,
        string numero,
        DateTime fechaEmision,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string codigoEstadoComprobante,
        string usuarioCreacion,
        int numeroLinea = 1,
        string? numeroFinal = null,
        DateTime? fechaVctoPago = null,
        decimal valorFactExp = 0,
        decimal biGravada = 0,
        decimal dsctoBi = 0,
        decimal igvIpm = 0,
        decimal dsctoIgvIpm = 0,
        decimal montoExonerado = 0,
        decimal montoInafecto = 0,
        decimal isc = 0,
        decimal biGravIvap = 0,
        decimal ivap = 0,
        decimal icbper = 0,
        decimal otrosTributos = 0,
        DateTime? fechaEmisionDocModif = null,
        string? tipoCpModificado = null,
        string? serieCpModificado = null,
        string? nroCpModificado = null,
        string? idProyectoOpAttr = null,
        decimal valorFobEmbar = 0,
        decimal valorOpGratuitas = 0,
        string? tipoOperacion = null,
        string? damCp = null,
        string? codigoTipoNota = null,
        string? camposLibres = null)
    {
        return new Venta
        {
            IdVenta = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
            CarSunat = carSunat,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            Numero = numero,
            NumeroFinal = numeroFinal,
            FechaEmision = fechaEmision,
            FechaVctoPago = fechaVctoPago,
            CodigoTipoDocIdentidad = codigoTipoDocIdentidad,
            NroDocIdentidad = nroDocIdentidad,
            RazonSocial = razonSocial,
            ValorFactExp = valorFactExp,
            BiGravada = biGravada,
            DsctoBi = dsctoBi,
            IgvIpm = igvIpm,
            DsctoIgvIpm = dsctoIgvIpm,
            MontoExonerado = montoExonerado,
            MontoInafecto = montoInafecto,
            Isc = isc,
            BiGravIvap = biGravIvap,
            Ivap = ivap,
            Icbper = icbper,
            OtrosTributos = otrosTributos,
            TotalCp = totalCp,
            CodigoMoneda = codigoMoneda,
            TipoCambio = tipoCambio,
            FechaEmisionDocModif = fechaEmisionDocModif,
            TipoCpModificado = tipoCpModificado,
            SerieCpModificado = serieCpModificado,
            NroCpModificado = nroCpModificado,
            IdProyectoOpAttr = idProyectoOpAttr,
            ValorFobEmbar = valorFobEmbar,
            ValorOpGratuitas = valorOpGratuitas,
            TipoOperacion = tipoOperacion,
            DamCp = damCp,
            CodigoTipoNota = codigoTipoNota,
            CodigoEstadoComprobante = codigoEstadoComprobante,
            CamposLibres = camposLibres,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = usuarioCreacion
        };
    }

    public void ActualizarDatos(
        string codigoTipoCp,
        string serie,
        string numero,
        DateTime fechaEmision,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal biGravada,
        decimal igvIpm,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string codigoEstadoComprobante,
        string carSunat,
        string usuarioModificacion)
    {
        CodigoTipoCp = codigoTipoCp;
        Serie = serie;
        Numero = numero;
        FechaEmision = fechaEmision;
        CodigoTipoDocIdentidad = codigoTipoDocIdentidad;
        NroDocIdentidad = nroDocIdentidad;
        RazonSocial = razonSocial;
        BiGravada = biGravada;
        IgvIpm = igvIpm;
        TotalCp = totalCp;
        CodigoMoneda = codigoMoneda;
        TipoCambio = tipoCambio;
        CodigoEstadoComprobante = codigoEstadoComprobante;
        CarSunat = carSunat;

        ModificadoPor = usuarioModificacion;
        FechaModificacion = DateTime.UtcNow;
    }
}
