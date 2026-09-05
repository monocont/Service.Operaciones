namespace Service.Operaciones.Domain.Entities;

public class CompraSire : EntidadAuditoria
{
    public Guid IdCompra { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;
    public int NumeroLinea { get; private set; }

    // Columnas 04 a 14: Datos Principales del Comprobante
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

    // Columnas 15 a 25: Bases Imponibles, Impuestos y Totales
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

    // Columnas 26 a 27: Moneda y Tipo de Cambio
    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal TipoCambio { get; private set; }

    // Columnas 28 a 32: Documento de Referencia / Modificado
    public DateTime? FechaEmisionDocModificado { get; private set; }
    public string? CodigoTipoCpModificado { get; private set; }
    public string? SerieCpModificado { get; private set; }
    public string? CodDamDsi { get; private set; }
    public string? NumeroCpModificado { get; private set; }

    // Columnas 33 a 41: Atributos Especiales de Compras RCE
    public string? ClasifBssSss { get; private set; }
    public string? IdProyectoOp { get; private set; }
    public decimal? PorcPart { get; private set; }
    public decimal Imb { get; private set; }
    public string? CarOrigIndEI { get; private set; }
    public string? Detraccion { get; private set; }
    public string? CodigoTipoNota { get; private set; }
    public string CodigoEstadoComprobante { get; private set; } = string.Empty;
    public string? Incal { get; private set; }

    // Campos Libres y Auditoría
    public string? CamposLibres { get; private set; }

    private CompraSire() { }

    public static CompraSire Crear(
        string empresaRuc,
        string periodo,
        Guid idCarga,
        int numeroLinea,
        DateTime fechaEmision,
        string codigoTipoCp,
        string serie,
        string numero,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string codigoEstadoComprobante,
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
        string? incal = null,
        string? camposLibres = null)
    {
        return new CompraSire
        {
            IdCompra = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            NumeroLinea = numeroLinea,
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
            CodigoMoneda = codigoMoneda,
            TipoCambio = tipoCambio,
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
            CodigoEstadoComprobante = codigoEstadoComprobante,
            Incal = incal,
            CamposLibres = camposLibres,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = usuarioCreacion
        };
    }

    /// <summary>
    /// Actualiza los campos editables del comprobante de compra SIRE (edición manual en el detalle).
    /// </summary>
    public void ActualizarDatos(
        string codigoTipoCp,
        string serie,
        string numero,
        DateTime fechaEmision,
        string codigoTipoDocIdentidad,
        string nroDocIdentidad,
        string razonSocial,
        decimal biGravadoDg,
        decimal igvIpmDg,
        decimal totalCp,
        string codigoMoneda,
        decimal tipoCambio,
        string codigoEstadoComprobante,
        string? detraccion,
        string? carSunat,
        string usuarioModificacion,
        string? empresaRuc = null)
    {
        if (!string.IsNullOrWhiteSpace(empresaRuc))
        {
            EmpresaRuc = empresaRuc.Trim();
        }

        CodigoTipoCp = codigoTipoCp;
        Serie = serie;
        Numero = numero;
        FechaEmision = fechaEmision;
        CodigoTipoDocIdentidad = codigoTipoDocIdentidad;
        NroDocIdentidad = nroDocIdentidad;
        RazonSocial = razonSocial;
        BiGravadoDg = biGravadoDg;
        IgvIpmDg = igvIpmDg;
        TotalCp = totalCp;
        CodigoMoneda = codigoMoneda;
        TipoCambio = tipoCambio;
        CodigoEstadoComprobante = codigoEstadoComprobante;
        Detraccion = detraccion;
        CarSunat = carSunat;

        ModificadoPor = usuarioModificacion;
        FechaModificacion = DateTime.UtcNow;
    }
}
