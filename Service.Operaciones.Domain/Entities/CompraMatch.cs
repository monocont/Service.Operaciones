namespace Service.Operaciones.Domain.Entities;

public class CompraMatch : EntidadAuditoria
{
    // Clave Primaria y Control de Carga
    public Guid IdCompraMatch { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    // Banderas de Estado del Match
    public string OrigenDato { get; private set; } = string.Empty; // 'SIRE' o 'EMPRESA'
    public bool EsCoincidenciaExacta { get; private set; }
    public bool EsDiferencia { get; private set; }
    public bool EsSoloUnOrigen { get; private set; }

    // Datos Principales del Comprobante y Proveedor
    public string? CarSunat { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string? AnioDocumento { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public string? NumeroFinal { get; private set; }

    public string CodigoTipoDocIdentidad { get; private set; } = string.Empty;
    public string NroDocIdentidad { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;

    // Bases Imponibles, Impuestos y Totales de Compras (RCE)
    public decimal BiGravadoDg { get; private set; }
    public decimal IgvIpmDg { get; private set; }
    public decimal BiGravadoDgng { get; private set; }
    public decimal IgvIpmDgng { get; private set; }
    public decimal BiGravadoDng { get; private set; }
    public decimal IgvIpmDng { get; private set; }
    public decimal ValorAdqNg { get; private set; }
    public decimal MontoIsc { get; private set; }
    public decimal MontoIcbper { get; private set; }
    public decimal MontoOtrosTributos { get; private set; }
    public decimal TotalCp { get; private set; }

    // Moneda y Tipo de Cambio
    public string CodigoMoneda { get; private set; } = "PEN";
    public decimal TipoCambio { get; private set; } = 1.0000m;

    // Documentos Modificados / Referencias
    public DateTime? FechaEmisionDocModificado { get; private set; }
    public string? CodigoTipoCpModificado { get; private set; }
    public string? SerieCpModificado { get; private set; }
    public string? CodDamDsi { get; private set; }
    public string? NumeroCpModificado { get; private set; }

    // Atributos Específicos de Compras RCE
    public string? ClasifBssSss { get; private set; }
    public string? IdProyectoOp { get; private set; }
    public decimal? PorcPart { get; private set; }
    public decimal Imb { get; private set; }
    public string? CarOrigIndEI { get; private set; }
    public string? Detraccion { get; private set; }
    public string? CodigoTipoNota { get; private set; }
    public string CodigoEstadoComprobante { get; private set; } = "1";
    public string? Incal { get; private set; }

    // Campos Libres
    public string? CamposLibres { get; private set; }

    private CompraMatch() { }

    public static CompraMatch Crear(
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
        string? anioDocumento = null,
        string? numeroFinal = null,
        decimal biGravadoDg = 0,
        decimal igvIpmDg = 0,
        decimal biGravadoDgng = 0,
        decimal igvIpmDgng = 0,
        decimal biGravadoDng = 0,
        decimal igvIpmDng = 0,
        decimal valorAdqNg = 0,
        decimal montoIsc = 0,
        decimal montoIcbper = 0,
        decimal montoOtrosTributos = 0,
        string codigoMoneda = "PEN",
        decimal tipoCambio = 1.0000m,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? codDamDsi = null,
        string? numeroCpModificado = null,
        string? clasifBssSss = null,
        string? idProyectoOp = null,
        decimal? porcPart = null,
        decimal imb = 0,
        string? carOrigIndEI = null,
        string? detraccion = null,
        string? codigoTipoNota = null,
        string codigoEstadoComprobante = "1",
        string? incal = null,
        string? camposLibres = null)
    {
        return new CompraMatch
        {
            IdCompraMatch = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
            OrigenDato = origenDato,
            EsCoincidenciaExacta = esCoincidenciaExacta,
            EsDiferencia = esDiferencia,
            EsSoloUnOrigen = esSoloUnOrigen,
            CarSunat = carSunat,
            FechaEmision = fechaEmision,
            FechaVencimiento = fechaVencimiento,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            AnioDocumento = anioDocumento,
            Numero = numero,
            NumeroFinal = numeroFinal,
            CodigoTipoDocIdentidad = codigoTipoDocIdentidad,
            NroDocIdentidad = nroDocIdentidad,
            RazonSocial = razonSocial,
            BiGravadoDg = biGravadoDg,
            IgvIpmDg = igvIpmDg,
            BiGravadoDgng = biGravadoDgng,
            IgvIpmDgng = igvIpmDgng,
            BiGravadoDng = biGravadoDng,
            IgvIpmDng = igvIpmDng,
            ValorAdqNg = valorAdqNg,
            MontoIsc = montoIsc,
            MontoIcbper = montoIcbper,
            MontoOtrosTributos = montoOtrosTributos,
            TotalCp = totalCp,
            CodigoMoneda = string.IsNullOrWhiteSpace(codigoMoneda) ? "PEN" : codigoMoneda,
            TipoCambio = tipoCambio > 0 ? tipoCambio : 1.0000m,
            FechaEmisionDocModificado = fechaEmisionDocModificado,
            CodigoTipoCpModificado = codigoTipoCpModificado,
            SerieCpModificado = serieCpModificado,
            CodDamDsi = codDamDsi,
            NumeroCpModificado = numeroCpModificado,
            ClasifBssSss = clasifBssSss,
            IdProyectoOp = idProyectoOp,
            PorcPart = porcPart,
            Imb = imb,
            CarOrigIndEI = carOrigIndEI,
            Detraccion = detraccion,
            CodigoTipoNota = codigoTipoNota,
            CodigoEstadoComprobante = string.IsNullOrWhiteSpace(codigoEstadoComprobante) ? "1" : codigoEstadoComprobante,
            Incal = incal,
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
        string? anioDocumento = null,
        string? numeroFinal = null,
        decimal biGravadoDg = 0,
        decimal igvIpmDg = 0,
        decimal biGravadoDgng = 0,
        decimal igvIpmDgng = 0,
        decimal biGravadoDng = 0,
        decimal igvIpmDng = 0,
        decimal valorAdqNg = 0,
        decimal montoIsc = 0,
        decimal montoIcbper = 0,
        decimal montoOtrosTributos = 0,
        string codigoMoneda = "PEN",
        decimal tipoCambio = 1.0000m,
        DateTime? fechaEmisionDocModificado = null,
        string? codigoTipoCpModificado = null,
        string? serieCpModificado = null,
        string? codDamDsi = null,
        string? numeroCpModificado = null,
        string? clasifBssSss = null,
        string? idProyectoOp = null,
        decimal? porcPart = null,
        decimal imb = 0,
        string? carOrigIndEI = null,
        string? detraccion = null,
        string? codigoTipoNota = null,
        string codigoEstadoComprobante = "1",
        string? incal = null,
        string? camposLibres = null)
    {
        OrigenDato = origenDato;
        EsCoincidenciaExacta = esCoincidenciaExacta;
        EsDiferencia = esDiferencia;
        EsSoloUnOrigen = esSoloUnOrigen;
        CarSunat = carSunat;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaVencimiento;
        CodigoTipoCp = codigoTipoCp;
        Serie = serie;
        AnioDocumento = anioDocumento;
        Numero = numero;
        NumeroFinal = numeroFinal;
        CodigoTipoDocIdentidad = codigoTipoDocIdentidad;
        NroDocIdentidad = nroDocIdentidad;
        RazonSocial = razonSocial;
        BiGravadoDg = biGravadoDg;
        IgvIpmDg = igvIpmDg;
        BiGravadoDgng = biGravadoDgng;
        IgvIpmDgng = igvIpmDgng;
        BiGravadoDng = biGravadoDng;
        IgvIpmDng = igvIpmDng;
        ValorAdqNg = valorAdqNg;
        MontoIsc = montoIsc;
        MontoIcbper = montoIcbper;
        MontoOtrosTributos = montoOtrosTributos;
        TotalCp = totalCp;
        CodigoMoneda = string.IsNullOrWhiteSpace(codigoMoneda) ? "PEN" : codigoMoneda;
        TipoCambio = tipoCambio > 0 ? tipoCambio : 1.0000m;
        FechaEmisionDocModificado = fechaEmisionDocModificado;
        CodigoTipoCpModificado = codigoTipoCpModificado;
        SerieCpModificado = serieCpModificado;
        CodDamDsi = codDamDsi;
        NumeroCpModificado = numeroCpModificado;
        ClasifBssSss = clasifBssSss;
        IdProyectoOp = idProyectoOp;
        PorcPart = porcPart;
        Imb = imb;
        CarOrigIndEI = carOrigIndEI;
        Detraccion = detraccion;
        CodigoTipoNota = codigoTipoNota;
        CodigoEstadoComprobante = string.IsNullOrWhiteSpace(codigoEstadoComprobante) ? "1" : codigoEstadoComprobante;
        Incal = incal;
        CamposLibres = camposLibres;
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
