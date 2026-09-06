using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Database;

public class OperacionesDbContext : DbContext
{
    public DbSet<TipoOperacionCatalogo> TipoOperacion => Set<TipoOperacionCatalogo>();
    public DbSet<ArchivoCarga> ArchivoCarga => Set<ArchivoCarga>();
    public DbSet<ArchivoCargaError> ArchivoCargaError => Set<ArchivoCargaError>();
    public DbSet<CompraSire> CompraSire => Set<CompraSire>();
    public DbSet<Venta> Venta => Set<Venta>();
    public DbSet<VentaEmpresa> VentaEmpresa => Set<VentaEmpresa>();
    public DbSet<VentaMatch> VentaMatch => Set<VentaMatch>();
    public DbSet<CompraEmpresa> CompraEmpresa => Set<CompraEmpresa>();
    public DbSet<CompraMatch> CompraMatch => Set<CompraMatch>();

    public OperacionesDbContext(DbContextOptions<OperacionesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("operaciones");

        modelBuilder.Entity<TipoOperacionCatalogo>(entity =>
        {
            entity.ToTable("tipo_operacion");
            entity.HasKey(e => e.IdTipoOperacion);
            entity.Property(e => e.IdTipoOperacion).HasColumnName("id_tipo_operacion").ValueGeneratedNever();
            entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(30).IsRequired();
            entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Modulo).HasColumnName("modulo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(250);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
        });

        modelBuilder.Entity<ArchivoCarga>(entity =>
        {
            entity.ToTable("archivo_carga");
            entity.HasKey(e => e.IdCarga);
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.IdTipoOperacion).HasColumnName("id_tipo_operacion").HasConversion<int>().IsRequired();
            entity.Property(e => e.Formato).HasColumnName("formato").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.NombreOriginal).HasColumnName("nombre_original").HasMaxLength(255).IsRequired();
            entity.Property(e => e.HashDocumento).HasColumnName("hash_documento").HasMaxLength(250).IsRequired();
            entity.Property(e => e.NumRegistros).HasColumnName("num_registros").HasDefaultValue(0);
            entity.Property(e => e.NumRegistrosValidos).HasColumnName("num_registros_validos").HasDefaultValue(0);
            entity.Property(e => e.NumRegistrosError).HasColumnName("num_registros_error").HasDefaultValue(0);
            entity.Property(e => e.TotalBaseImponible).HasColumnName("total_base_imponible").HasColumnType("numeric(18,8)").HasDefaultValue(0);
            entity.Property(e => e.TotalIgv).HasColumnName("total_igv").HasColumnType("numeric(18,8)").HasDefaultValue(0);
            entity.Property(e => e.TotalGeneral).HasColumnName("total_general").HasColumnType("numeric(18,8)").HasDefaultValue(0);
            entity.Property(e => e.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.IdTipoOperacion, e.HashDocumento })
                .IsUnique()
                .HasDatabaseName("uq_ac_hash");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.IdTipoOperacion })
                .HasDatabaseName("idx_ac_empresa_periodo");
        });

        modelBuilder.Entity<ArchivoCargaError>(entity =>
        {
            entity.ToTable("archivo_carga_error");
            entity.HasKey(e => e.IdError);
            entity.Property(e => e.IdError).HasColumnName("id_error").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea");
            entity.Property(e => e.TipoError).HasColumnName("tipo_error").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.CampoError).HasColumnName("campo_error").HasMaxLength(50);
            entity.Property(e => e.ValorLectura).HasColumnName("valor_lectura").HasMaxLength(500);
            entity.Property(e => e.Mensaje).HasColumnName("mensaje").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Severidad).HasColumnName("severidad").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");

            entity.HasOne<ArchivoCarga>()
                .WithMany()
                .HasForeignKey(e => e.IdCarga)
                .HasConstraintName("fk_ace_carga")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_ace_carga");
        });

        modelBuilder.Entity<CompraSire>(entity =>
        {
            entity.ToTable("compra_sire");
            entity.HasKey(e => e.IdCompra);
            entity.Property(e => e.IdCompra).HasColumnName("id_compra").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();

            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40).IsRequired();
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.AnioDocumento).HasColumnName("anio_documento").HasMaxLength(20);
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();

            entity.Property(e => e.BiGravadoDg).HasColumnName("bi_gravado_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDg).HasColumnName("igv_ipm_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDgng).HasColumnName("bi_gravado_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDgng).HasColumnName("igv_ipm_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDng).HasColumnName("bi_gravado_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDng).HasColumnName("igv_ipm_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.ValorAdqNg).HasColumnName("valor_adq_ng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIsc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIcbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoOtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();

            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)");

            entity.Property(e => e.FechaEmisionDocModificado).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.CodDamDsi).HasColumnName("cod_dam_dsi").HasMaxLength(20);
            entity.Property(e => e.NumeroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);

            entity.Property(e => e.ClasifBssSss).HasColumnName("clasif_bss_sss").HasMaxLength(20);
            entity.Property(e => e.IdProyectoOp).HasColumnName("id_proyecto_op").HasMaxLength(50);
            entity.Property(e => e.PorcPart).HasColumnName("porc_part").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Imb).HasColumnName("imb").HasColumnType("numeric(18,8)");
            entity.Property(e => e.CarOrigIndEI).HasColumnName("car_orig_ind_e_i").HasMaxLength(40);
            entity.Property(e => e.Detraccion).HasColumnName("detraccion").HasMaxLength(50);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Incal).HasColumnName("incal").HasMaxLength(20);

            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres").HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_compra_sire_carga");
            entity.HasIndex(e => new { e.Serie, e.Numero }).HasDatabaseName("idx_compra_sire_serie_num");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.CarSunat }).HasDatabaseName("idx_compra_sire_car_sunat");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo }).HasDatabaseName("idx_compra_sire_empresa_periodo");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("venta_sire");
            entity.HasKey(e => e.IdVenta);
            entity.Property(e => e.IdVenta).HasColumnName("id_venta").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();
            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40).IsRequired();
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVctoPago).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();
            entity.Property(e => e.ValorFactExp).HasColumnName("valor_facturado_exportacion").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravada).HasColumnName("bi_gravada").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DsctoBi).HasColumnName("descuento_bi").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpm).HasColumnName("igv_ipm").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DsctoIgvIpm).HasColumnName("descuento_igv").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoExonerado).HasColumnName("monto_exonerado").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoInafecto).HasColumnName("monto_inafecto").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Isc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravIvap).HasColumnName("bi_gravada_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Ivap).HasColumnName("monto_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Icbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.OtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();
            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)");
            entity.Property(e => e.FechaEmisionDocModif).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.TipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.NroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.TipoOperacion).HasColumnName("tipo_operacion").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            // Ignorar campos que no están en la tabla SQL operaciones.venta_sire
            entity.Ignore(e => e.DamCp);
            entity.Ignore(e => e.IdProyectoOpAttr);
            entity.Ignore(e => e.ValorFobEmbar);
            entity.Ignore(e => e.ValorOpGratuitas);

            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.CarSunat })
                .HasDatabaseName("idx_venta_car_sunat");
            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_venta_carga");
            entity.HasIndex(e => new { e.Serie, e.Numero }).HasDatabaseName("idx_venta_serie_num");
        });

        modelBuilder.Entity<VentaEmpresa>(entity =>
        {
            entity.ToTable("venta_empresa");
            entity.HasKey(e => e.IdVentaEmpresa);
            entity.Property(e => e.IdVentaEmpresa).HasColumnName("id_venta_empresa").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();
            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();
            entity.Property(e => e.ValorFacturadoExportacion).HasColumnName("valor_facturado_exportacion").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravada).HasColumnName("bi_gravada").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DescuentoBi).HasColumnName("descuento_bi").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpm).HasColumnName("igv_ipm").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DescuentoIgv).HasColumnName("descuento_igv").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoExonerado).HasColumnName("monto_exonerado").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoInafecto).HasColumnName("monto_inafecto").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIsc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadaIvap).HasColumnName("bi_gravada_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIvap).HasColumnName("monto_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIcbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoOtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();
            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)").HasDefaultValue(1.00000000m);
            entity.Property(e => e.FechaEmisionDocModificado).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.NumeroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.TipoOperacion).HasColumnName("tipo_operacion").HasMaxLength(20);
            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_ve_carga");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.FechaEmision }).HasDatabaseName("idx_ve_empresa_periodo");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.CodigoTipoCp, e.Serie, e.Numero }).HasDatabaseName("idx_ve_cruce_match");
            entity.HasIndex(e => new { e.NroDocIdentidad, e.FechaEmision }).HasDatabaseName("idx_ve_cliente");
        });

        modelBuilder.Entity<VentaMatch>(entity =>
        {
            entity.ToTable("venta_match");
            entity.HasKey(e => e.IdVentaMatch);
            entity.Property(e => e.IdVentaMatch).HasColumnName("id_venta_match").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();

            entity.Property(e => e.OrigenDato).HasColumnName("origen_dato").HasMaxLength(20).IsRequired();
            entity.Property(e => e.EsCoincidenciaExacta).HasColumnName("es_coincidencia_exacta").IsRequired();
            entity.Property(e => e.EsDiferencia).HasColumnName("es_diferencia").IsRequired();
            entity.Property(e => e.EsSoloUnOrigen).HasColumnName("es_solo_un_origen").IsRequired();

            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40);
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();

            entity.Property(e => e.ValorFacturadoExportacion).HasColumnName("valor_facturado_exportacion").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravada).HasColumnName("bi_gravada").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DescuentoBi).HasColumnName("descuento_bi").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpm).HasColumnName("igv_ipm").HasColumnType("numeric(18,8)");
            entity.Property(e => e.DescuentoIgv).HasColumnName("descuento_igv").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoExonerado).HasColumnName("monto_exonerado").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoInafecto).HasColumnName("monto_inafecto").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIsc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadaIvap).HasColumnName("bi_gravada_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIvap).HasColumnName("monto_ivap").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIcbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoOtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();
            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)");

            entity.Property(e => e.FechaEmisionDocModificado).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.NumeroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.TipoOperacion).HasColumnName("tipo_operacion").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres");

            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_venta_match_carga");
            entity.HasIndex(e => new { e.Serie, e.Numero }).HasDatabaseName("idx_venta_match_serie_num");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo }).HasDatabaseName("idx_venta_match_empresa_periodo");
        });

        modelBuilder.Entity<CompraEmpresa>(entity =>
        {
            entity.ToTable("compra_empresa");
            entity.HasKey(e => e.IdCompraEmpresa);
            entity.Property(e => e.IdCompraEmpresa).HasColumnName("id_compra_empresa").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();

            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.AnioDocumento).HasColumnName("anio_documento").HasMaxLength(20);
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();

            entity.Property(e => e.BiGravadoDg).HasColumnName("bi_gravado_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDg).HasColumnName("igv_ipm_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDgng).HasColumnName("bi_gravado_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDgng).HasColumnName("igv_ipm_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDng).HasColumnName("bi_gravado_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDng).HasColumnName("igv_ipm_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.ValorAdqNg).HasColumnName("valor_adq_ng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIsc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIcbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoOtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();

            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)");

            entity.Property(e => e.FechaEmisionDocModificado).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.CodDamDsi).HasColumnName("cod_dam_dsi").HasMaxLength(20);
            entity.Property(e => e.NumeroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);

            entity.Property(e => e.ClasifBssSss).HasColumnName("clasif_bss_sss").HasMaxLength(20);
            entity.Property(e => e.IdProyectoOp).HasColumnName("id_proyecto_op").HasMaxLength(50);
            entity.Property(e => e.PorcPart).HasColumnName("porc_part").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Imb).HasColumnName("imb").HasColumnType("numeric(18,8)");
            entity.Property(e => e.CarOrigIndEI).HasColumnName("car_orig_ind_e_i").HasMaxLength(40);
            entity.Property(e => e.Detraccion).HasColumnName("detraccion").HasMaxLength(50);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Incal).HasColumnName("incal").HasMaxLength(20);

            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres").HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_ce_carga");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.FechaEmision }).HasDatabaseName("idx_ce_empresa_periodo");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.NroDocIdentidad, e.CodigoTipoCp, e.Serie, e.Numero }).HasDatabaseName("idx_ce_cruce_match");
            entity.HasIndex(e => new { e.NroDocIdentidad, e.FechaEmision }).HasDatabaseName("idx_ce_proveedor");
            entity.HasIndex(e => e.CarSunat).HasDatabaseName("idx_ce_car_sunat");
        });

        modelBuilder.Entity<CompraMatch>(entity =>
        {
            entity.ToTable("compra_match");
            entity.HasKey(e => e.IdCompraMatch);
            entity.Property(e => e.IdCompraMatch).HasColumnName("id_compra_match").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea").IsRequired();

            entity.Property(e => e.OrigenDato).HasColumnName("origen_dato").HasMaxLength(20).IsRequired();
            entity.Property(e => e.EsCoincidenciaExacta).HasColumnName("es_coincidencia_exacta").IsRequired();
            entity.Property(e => e.EsDiferencia).HasColumnName("es_diferencia").IsRequired();
            entity.Property(e => e.EsSoloUnOrigen).HasColumnName("es_solo_un_origen").IsRequired();

            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(20).IsRequired();
            entity.Property(e => e.AnioDocumento).HasColumnName("anio_documento").HasMaxLength(20);
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(1500).IsRequired();

            entity.Property(e => e.BiGravadoDg).HasColumnName("bi_gravado_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDg).HasColumnName("igv_ipm_dg").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDgng).HasColumnName("bi_gravado_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDgng).HasColumnName("igv_ipm_dgng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.BiGravadoDng).HasColumnName("bi_gravado_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.IgvIpmDng).HasColumnName("igv_ipm_dng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.ValorAdqNg).HasColumnName("valor_adq_ng").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIsc).HasColumnName("monto_isc").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoIcbper).HasColumnName("monto_icbper").HasColumnType("numeric(18,8)");
            entity.Property(e => e.MontoOtrosTributos).HasColumnName("monto_otros_tributos").HasColumnType("numeric(18,8)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(18,8)").IsRequired();

            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(18,8)");

            entity.Property(e => e.FechaEmisionDocModificado).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.CodigoTipoCpModificado).HasColumnName("codigo_tipo_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.CodDamDsi).HasColumnName("cod_dam_dsi").HasMaxLength(20);
            entity.Property(e => e.NumeroCpModificado).HasColumnName("numero_cp_modificado").HasMaxLength(20);

            entity.Property(e => e.ClasifBssSss).HasColumnName("clasif_bss_sss").HasMaxLength(20);
            entity.Property(e => e.IdProyectoOp).HasColumnName("id_proyecto_op").HasMaxLength(50);
            entity.Property(e => e.PorcPart).HasColumnName("porc_part").HasColumnType("numeric(18,8)");
            entity.Property(e => e.Imb).HasColumnName("imb").HasColumnType("numeric(18,8)");
            entity.Property(e => e.CarOrigIndEI).HasColumnName("car_orig_ind_e_i").HasMaxLength(40);
            entity.Property(e => e.Detraccion).HasColumnName("detraccion").HasMaxLength(50);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(20);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Incal).HasColumnName("incal").HasMaxLength(20);

            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres").HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_cm_carga");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.FechaEmision }).HasDatabaseName("idx_cm_empresa_periodo");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.NroDocIdentidad, e.CodigoTipoCp, e.Serie, e.Numero }).HasDatabaseName("idx_cm_cruce_match");
            entity.HasIndex(e => new { e.NroDocIdentidad, e.FechaEmision }).HasDatabaseName("idx_cm_proveedor");
            entity.HasIndex(e => e.CarSunat).HasDatabaseName("idx_cm_car_sunat");
        });
    }
}
