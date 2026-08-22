namespace Service.Operaciones.Domain.Entities;

public class Compra : EntidadAuditoria
{
    public Guid IdCompra { get; private set; }
    public Guid IdCarga { get; private set; }
    public string EmpresaRuc { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;

    public string CarSunat { get; private set; } = string.Empty;
    public string CodigoTipoCp { get; private set; } = string.Empty;
    public string Serie { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string? NumeroFinal { get; private set; }
    public string? AnioDocumento { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaVctoPago { get; private set; }

    public string CodigoTipoDocIdentidad { get; private set; } = string.Empty;
    public string NroDocIdentidad { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;

    public decimal BiGravadoDg { get; private set; }
    public decimal IgvIpmDg { get; private set; }
    public decimal BiGravadoDgng { get; private set; }
    public decimal IgvIpmDgng { get; private set; }
    public decimal BiGravadoDng { get; private set; }
    public decimal IgvIpmDng { get; private set; }
    public decimal ValorAdqNg { get; private set; }

    public decimal Isc { get; private set; }
    public decimal Icbper { get; private set; }
    public decimal OtrosTribCargos { get; private set; }
    public decimal TotalCp { get; private set; }

    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal TipoCambio { get; private set; }

    public DateTime? FechaEmisionDocModif { get; private set; }
    public string? TipoCpModificado { get; private set; }
    public string? SerieCpModificado { get; private set; }
    public string? NroCpModificado { get; private set; }
    public string? CodDamDsi { get; private set; }
    public string? ClasifBssSss { get; private set; }

    public string? IdProyectoOp { get; private set; }
    public decimal? PorcPart { get; private set; }
    public decimal? Imb { get; private set; }
    public string? CarOrigIndEI { get; private set; }
    public string? Detraccion { get; private set; }

    public string? CodigoTipoNota { get; private set; }
    public string CodigoEstadoComprobante { get; private set; } = string.Empty;
    public string? Incal { get; private set; }

    public string? CamposLibres { get; private set; }

    private Compra() { }

    public static Compra Crear(
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
        string? numeroFinal = null,
        string? anioDocumento = null,
        DateTime? fechaVctoPago = null,
        decimal biGravadoDg = 0,
        decimal igvIpmDg = 0,
        decimal biGravadoDgng = 0,
        decimal igvIpmDgng = 0,
        decimal biGravadoDng = 0,
        decimal igvIpmDng = 0,
        decimal valorAdqNg = 0,
        decimal isc = 0,
        decimal icbper = 0,
        decimal otrosTribCargos = 0,
        DateTime? fechaEmisionDocModif = null,
        string? tipoCpModificado = null,
        string? serieCpModificado = null,
        string? nroCpModificado = null,
        string? codDamDsi = null,
        string? clasifBssSss = null,
        string? idProyectoOp = null,
        decimal? porcPart = null,
        decimal? imb = null,
        string? carOrigIndEI = null,
        string? detraccion = null,
        string? codigoTipoNota = null,
        string? incal = null,
        string? camposLibres = null)
    {
        return new Compra
        {
            IdCompra = Guid.NewGuid(),
            IdCarga = idCarga,
            EmpresaRuc = empresaRuc,
            Periodo = periodo,
            CarSunat = carSunat,
            CodigoTipoCp = codigoTipoCp,
            Serie = serie,
            Numero = numero,
            NumeroFinal = numeroFinal,
            AnioDocumento = anioDocumento,
            FechaEmision = fechaEmision,
            FechaVctoPago = fechaVctoPago,
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
            Isc = isc,
            Icbper = icbper,
            OtrosTribCargos = otrosTribCargos,
            TotalCp = totalCp,
            CodigoMoneda = codigoMoneda,
            TipoCambio = tipoCambio,
            FechaEmisionDocModif = fechaEmisionDocModif,
            TipoCpModificado = tipoCpModificado,
            SerieCpModificado = serieCpModificado,
            NroCpModificado = nroCpModificado,
            CodDamDsi = codDamDsi,
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
    /// Actualiza los campos editables del comprobante de compra (edición manual en el detalle).
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
