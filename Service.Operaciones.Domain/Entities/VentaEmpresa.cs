namespace Service.Operaciones.Domain.Entities;

public class VentaEmpresa : EntidadAuditoria
{
    public Guid IdVentaEmpresa { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    public string? CarSunat { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string? NumeroFinal { get; private set; }

    public string CodigoTipoDocIdentidad { get; private set; } = string.Empty;
    public string NroDocIdentidad { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;

    public decimal ValorFacturadoExportacion { get; private set; }
    public decimal BiGravada { get; private set; }
    public decimal DescuentoBi { get; private set; }
    public decimal IgvIpm { get; private set; }
    public decimal DescuentoIgv { get; private set; }
    public decimal MontoExonerado { get; private set; }
    public decimal MontoInafecto { get; private set; }
    public decimal MontoIsc { get; private set; }
    public decimal BiGravadaIvap { get; private set; }
    public decimal MontoIvap { get; private set; }
    public decimal MontoIcbper { get; private set; }
    public decimal MontoOtrosTributos { get; private set; }
    public decimal TotalCp { get; private set; }

    public string CodigoMoneda { get; private set; } = "PEN";
    public decimal TipoCambio { get; private set; } = 1.0000m;

    public DateTime? FechaEmisionDocModificado { get; private set; }
    public string? CodigoTipoCpModificado { get; private set; }
    public string? SerieCpModificado { get; private set; }
    public string? NumeroCpModificado { get; private set; }

    public string CodigoEstadoComprobante { get; private set; } = "1";
    public string? CodigoTipoNota { get; private set; }
    public string? TipoOperacion { get; private set; }
    public string? CamposLibres { get; private set; }

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
        string usuarioCreacion,
        string razonSocial = "",
        string? carSunat = null,
        DateTime? fechaVencimiento = null,
        string? numeroFinal = null,
        decimal valorFacturadoExportacion = 0,
        decimal biGravada = 0,
        decimal descuentoBi = 0,
        decimal igvIpm = 0,
        decimal descuentoIgv = 0,
        decimal montoExonerado = 0,
        decimal montoInafecto = 0,
        decimal montoIsc = 0,
        decimal biGravadaIvap = 0,
        decimal montoIvap = 0,
        decimal montoIcbper = 0,
        decimal montoOtrosTributos = 0,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? numeroCpModificado = null,
        string codigoEstadoComprobante = "1",
        string? codigoTipoNota = null,
        string? tipoOperacion = null,
        string? camposLibres = null)
    {
        return new VentaEmpresa
        {
            IdVentaEmpresa = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
            CarSunat = carSunat,
            FechaEmision = fechaEmision,
            FechaVencimiento = fechaVencimiento,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            Numero = numero,
            NumeroFinal = numeroFinal,
            CodigoTipoDocIdentidad = codigoTipoDocIdentidad,
            NroDocIdentidad = nroDocIdentidad,
            RazonSocial = razonSocial,
            ValorFacturadoExportacion = valorFacturadoExportacion,
            BiGravada = biGravada,
            DescuentoBi = descuentoBi,
            IgvIpm = igvIpm,
            DescuentoIgv = descuentoIgv,
            MontoExonerado = montoExonerado,
            MontoInafecto = montoInafecto,
            MontoIsc = montoIsc,
            BiGravadaIvap = biGravadaIvap,
            MontoIvap = montoIvap,
            MontoIcbper = montoIcbper,
            MontoOtrosTributos = montoOtrosTributos,
            TotalCp = totalCp,
            CodigoMoneda = codigoMoneda,
            TipoCambio = tipoCambio,
            FechaEmisionDocModificado = fechaEmisionDocModificado,
            CodigoTipoCpModificado = codigoTipoCpModificado,
            SerieCpModificado = serieCpModificado,
            NumeroCpModificado = numeroCpModificado,
            CodigoEstadoComprobante = codigoEstadoComprobante,
            CodigoTipoNota = codigoTipoNota,
            TipoOperacion = tipoOperacion,
            CamposLibres = camposLibres,
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
        string usuarioModificacion,
        string razonSocial = "",
        string? carSunat = null,
        DateTime? fechaVencimiento = null,
        string? numeroFinal = null,
        decimal valorFacturadoExportacion = 0,
        decimal biGravada = 0,
        decimal descuentoBi = 0,
        decimal igvIpm = 0,
        decimal descuentoIgv = 0,
        decimal montoExonerado = 0,
        decimal montoInafecto = 0,
        decimal montoIsc = 0,
        decimal biGravadaIvap = 0,
        decimal montoIvap = 0,
        decimal montoIcbper = 0,
        decimal montoOtrosTributos = 0,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? numeroCpModificado = null,
        string codigoEstadoComprobante = "1",
        string? codigoTipoNota = null,
        string? tipoOperacion = null,
        string? camposLibres = null)
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
        RazonSocial = razonSocial;
        CarSunat = carSunat;
        FechaVencimiento = fechaVencimiento;
        NumeroFinal = numeroFinal;
        ValorFacturadoExportacion = valorFacturadoExportacion;
        BiGravada = biGravada;
        DescuentoBi = descuentoBi;
        IgvIpm = igvIpm;
        DescuentoIgv = descuentoIgv;
        MontoExonerado = montoExonerado;
        MontoInafecto = montoInafecto;
        MontoIsc = montoIsc;
        BiGravadaIvap = biGravadaIvap;
        MontoIvap = montoIvap;
        MontoIcbper = montoIcbper;
        MontoOtrosTributos = montoOtrosTributos;
        FechaEmisionDocModificado = fechaEmisionDocModificado;
        CodigoTipoCpModificado = codigoTipoCpModificado;
        SerieCpModificado = serieCpModificado;
        NumeroCpModificado = numeroCpModificado;
        CodigoEstadoComprobante = codigoEstadoComprobante;
        CodigoTipoNota = codigoTipoNota;
        TipoOperacion = tipoOperacion;
        CamposLibres = camposLibres;

        ModificadoPor = usuarioModificacion;
        FechaModificacion = DateTime.UtcNow;
    }
}
