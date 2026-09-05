namespace Service.Operaciones.Domain.Entities;

public class VentaMatch : EntidadAuditoria
{
    public Guid IdVentaMatch { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    // Banderas de Match
    public string OrigenDato { get; private set; } = string.Empty; // 'SIRE' o 'EMPRESA'
    public bool EsCoincidenciaExacta { get; private set; }
    public bool EsDiferencia { get; private set; }
    public bool EsSoloUnOrigen { get; private set; }

    // Datos del Comprobante
    public string? CarSunat { get; private set; }
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string? NumeroFinal { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }

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

    private VentaMatch() { }

    public static VentaMatch Crear(
        Guid idCarga,
        string empresaRuc,
        string periodo,
        int numeroLinea,
        string origenDato,
        bool esCoincidenciaExacta,
        bool esDiferencia,
        bool esSoloUnOrigen,
        string codigoTipoCp,
        string serie,
        string numero,
        DateTime fechaEmision,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal totalCp,
        string usuarioCreacion,
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
        string codigoMoneda = "PEN",
        decimal tipoCambio = 1.0000m,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? numeroCpModificado = null,
        string codigoEstadoComprobante = "1",
        string? codigoTipoNota = null,
        string? tipoOperacion = null,
        string? camposLibres = null)
    {
        return new VentaMatch
        {
            IdVentaMatch = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
            OrigenDato = origenDato,
            EsCoincidenciaExacta = esCoincidenciaExacta,
            EsDiferencia = esDiferencia,
            EsSoloUnOrigen = esSoloUnOrigen,
            CarSunat = carSunat,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            Numero = numero,
            NumeroFinal = numeroFinal,
            FechaEmision = fechaEmision,
            FechaVencimiento = fechaVencimiento,
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
            CreadoPor = usuarioCreacion,
            FechaCreacion = DateTime.UtcNow,
            Activo = true
        };
    }

    public void ActualizarDatos(
        string origenDato,
        bool esCoincidenciaExacta,
        bool esDiferencia,
        bool esSoloUnOrigen,
        string codigoTipoCp,
        string serie,
        string numero,
        DateTime fechaEmision,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal totalCp,
        string usuarioModificacion,
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
        string codigoMoneda = "PEN",
        decimal tipoCambio = 1.0000m,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? numeroCpModificado = null,
        string codigoEstadoComprobante = "1",
        string? codigoTipoNota = null,
        string? tipoOperacion = null,
        string? camposLibres = null)
    {
        OrigenDato = origenDato;
        EsCoincidenciaExacta = esCoincidenciaExacta;
        EsDiferencia = esDiferencia;
        EsSoloUnOrigen = esSoloUnOrigen;
        CodigoTipoCp = codigoTipoCp;
        Serie = serie;
        Numero = numero;
        NumeroFinal = numeroFinal;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaVencimiento;
        CodigoTipoDocIdentidad = codigoTipoDocIdentidad;
        NroDocIdentidad = nroDocIdentidad;
        RazonSocial = razonSocial;
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
        TotalCp = totalCp;
        CodigoMoneda = codigoMoneda;
        TipoCambio = tipoCambio;
        FechaEmisionDocModificado = fechaEmisionDocModificado;
        CodigoTipoCpModificado = codigoTipoCpModificado;
        SerieCpModificado = serieCpModificado;
        NumeroCpModificado = numeroCpModificado;
        CodigoEstadoComprobante = codigoEstadoComprobante;
        CodigoTipoNota = codigoTipoNota;
        TipoOperacion = tipoOperacion;
        CamposLibres = camposLibres;
        CarSunat = carSunat;
        ModificadoPor = usuarioModificacion;
        FechaModificacion = DateTime.UtcNow;
    }

    public void ActualizarBanderasMatch(bool esCoincidenciaExacta, bool esDiferencia, bool esSoloUnOrigen)
    {
        EsCoincidenciaExacta = esCoincidenciaExacta;
        EsDiferencia = esDiferencia;
        EsSoloUnOrigen = esSoloUnOrigen;
        FechaModificacion = DateTime.UtcNow;
    }
}
