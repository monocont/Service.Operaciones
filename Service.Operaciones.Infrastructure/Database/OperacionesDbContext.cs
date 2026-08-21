using Microsoft.EntityFrameworkCore;
using Service.Operaciones.Domain.Entities;

namespace Service.Operaciones.Infrastructure.Database;

public class OperacionesDbContext : DbContext
{
    public DbSet<ArchivoCarga> ArchivoCarga => Set<ArchivoCarga>();
    public DbSet<ArchivoCargaError> ArchivoCargaError => Set<ArchivoCargaError>();
    public DbSet<Compra> Compra => Set<Compra>();
    public DbSet<Venta> Venta => Set<Venta>();

    public OperacionesDbContext(DbContextOptions<OperacionesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("operaciones");

        modelBuilder.Entity<ArchivoCarga>(entity =>
        {
            entity.ToTable("archivo_carga");
            entity.HasKey(e => e.IdCarga);
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(11).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(6).IsRequired();
            entity.Property(e => e.TipoArchivo).HasColumnName("tipo_archivo").HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.Property(e => e.Formato).HasColumnName("formato").HasConversion<string>().HasMaxLength(4).IsRequired();
            entity.Property(e => e.NombreOriginal).HasColumnName("nombre_original").HasMaxLength(255).IsRequired();
            entity.Property(e => e.HashDocumento).HasColumnName("hash_documento").HasMaxLength(64).IsRequired();
            entity.Property(e => e.NumRegistros).HasColumnName("num_registros").HasDefaultValue(0);
            entity.Property(e => e.NumRegistrosValidos).HasColumnName("num_registros_validos").HasDefaultValue(0);
            entity.Property(e => e.NumRegistrosError).HasColumnName("num_registros_error").HasDefaultValue(0);
            entity.Property(e => e.TotalBaseImponible).HasColumnName("total_base_imponible").HasColumnType("numeric(14,2)").HasDefaultValue(0);
            entity.Property(e => e.TotalIgv).HasColumnName("total_igv").HasColumnType("numeric(14,2)").HasDefaultValue(0);
            entity.Property(e => e.TotalGeneral).HasColumnName("total_general").HasColumnType("numeric(14,2)").HasDefaultValue(0);
            entity.Property(e => e.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.TipoArchivo, e.HashDocumento })
                .IsUnique()
                .HasDatabaseName("uq_ac_hash");
            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.TipoArchivo })
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
            entity.Property(e => e.Severidad).HasColumnName("severidad").HasConversion<string>().HasMaxLength(15).IsRequired();
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");

            entity.HasOne<ArchivoCarga>()
                .WithMany()
                .HasForeignKey(e => e.IdCarga)
                .HasConstraintName("fk_ace_carga")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_ace_carga");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.ToTable("compra");
            entity.HasKey(e => e.IdCompra);
            entity.Property(e => e.IdCompra).HasColumnName("id_compra").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(11).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(6).IsRequired();
            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40).IsRequired();
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(2).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.AnioDocumento).HasColumnName("anio_documento").HasMaxLength(4);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVctoPago).HasColumnName("fecha_vcto_pago").HasColumnType("date");
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(2).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(15).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(200).IsRequired();
            entity.Property(e => e.BiGravadoDg).HasColumnName("bi_gravado_dg").HasColumnType("numeric(14,2)");
            entity.Property(e => e.IgvIpmDg).HasColumnName("igv_ipm_dg").HasColumnType("numeric(14,2)");
            entity.Property(e => e.BiGravadoDgng).HasColumnName("bi_gravado_dgng").HasColumnType("numeric(14,2)");
            entity.Property(e => e.IgvIpmDgng).HasColumnName("igv_ipm_dgng").HasColumnType("numeric(14,2)");
            entity.Property(e => e.BiGravadoDng).HasColumnName("bi_gravado_dng").HasColumnType("numeric(14,2)");
            entity.Property(e => e.IgvIpmDng).HasColumnName("igv_ipm_dng").HasColumnType("numeric(14,2)");
            entity.Property(e => e.ValorAdqNg).HasColumnName("valor_adq_ng").HasColumnType("numeric(14,2)");
            entity.Property(e => e.Isc).HasColumnName("isc").HasColumnType("numeric(14,2)");
            entity.Property(e => e.Icbper).HasColumnName("icbper").HasColumnType("numeric(14,2)");
            entity.Property(e => e.OtrosTribCargos).HasColumnName("otros_trib_cargos").HasColumnType("numeric(14,2)");
            entity.Property(e => e.TotalCp).HasColumnName("total_cp").HasColumnType("numeric(14,2)").IsRequired();
            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(10,4)");
            entity.Property(e => e.FechaEmisionDocModif).HasColumnName("fecha_emision_doc_modif").HasColumnType("date");
            entity.Property(e => e.TipoCpModificado).HasColumnName("tipo_cp_modificado").HasMaxLength(2);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(10);
            entity.Property(e => e.NroCpModificado).HasColumnName("nro_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.CodDamDsi).HasColumnName("cod_dam_dsi").HasMaxLength(20);
            entity.Property(e => e.ClasifBssSss).HasColumnName("clasif_bss_sss").HasMaxLength(10);
            entity.Property(e => e.IdProyectoOp).HasColumnName("id_proyecto_op").HasMaxLength(50);
            entity.Property(e => e.PorcPart).HasColumnName("porc_part").HasColumnType("numeric(5,2)");
            entity.Property(e => e.Imb).HasColumnName("imb").HasColumnType("numeric(14,2)");
            entity.Property(e => e.CarOrigIndEI).HasColumnName("car_orig_ind_e_i").HasMaxLength(20);
            entity.Property(e => e.Detraccion).HasColumnName("detraccion").HasMaxLength(1);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(2);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(2).IsRequired();
            entity.Property(e => e.Incal).HasColumnName("incal").HasMaxLength(1);
            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.CarSunat })
                .HasDatabaseName("idx_compra_car_sunat");
            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_compra_carga");
            entity.HasIndex(e => new { e.CodigoTipoCp, e.Serie, e.Numero }).HasDatabaseName("idx_compra_tipo_serie_num");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("venta");
            entity.HasKey(e => e.IdVenta);
            entity.Property(e => e.IdVenta).HasColumnName("id_venta").HasColumnType("uuid");
            entity.Property(e => e.IdCarga).HasColumnName("id_carga").HasColumnType("uuid");
            entity.Property(e => e.EmpresaRuc).HasColumnName("empresa_ruc").HasMaxLength(11).IsRequired();
            entity.Property(e => e.Periodo).HasColumnName("periodo").HasMaxLength(6).IsRequired();
            entity.Property(e => e.CarSunat).HasColumnName("car_sunat").HasMaxLength(40).IsRequired();
            entity.Property(e => e.CodigoTipoCp).HasColumnName("codigo_tipo_cp").HasMaxLength(2).IsRequired();
            entity.Property(e => e.Serie).HasColumnName("serie").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumeroFinal).HasColumnName("numero_final").HasMaxLength(20);
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired();
            entity.Property(e => e.FechaVctoPago).HasColumnName("fecha_vencimiento_pago").HasColumnType("date");
            entity.Property(e => e.CodigoTipoDocIdentidad).HasColumnName("codigo_tipo_doc_identidad").HasMaxLength(2).IsRequired();
            entity.Property(e => e.NroDocIdentidad).HasColumnName("nro_doc_identidad").HasMaxLength(15).IsRequired();
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ValorFactExp).HasColumnName("valor_facturado_exportacion").HasColumnType("numeric(14,2)");
            entity.Property(e => e.BiGravada).HasColumnName("base_imponible_gravada").HasColumnType("numeric(14,2)");
            entity.Property(e => e.DsctoBi).HasColumnName("descuento_base_imponible").HasColumnType("numeric(14,2)");
            entity.Property(e => e.IgvIpm).HasColumnName("igv_ipm").HasColumnType("numeric(14,2)");
            entity.Property(e => e.DsctoIgvIpm).HasColumnName("descuento_igv_ipm").HasColumnType("numeric(14,2)");
            entity.Property(e => e.MontoExonerado).HasColumnName("monto_exonerado").HasColumnType("numeric(14,2)");
            entity.Property(e => e.MontoInafecto).HasColumnName("monto_inafecto").HasColumnType("numeric(14,2)");
            entity.Property(e => e.Isc).HasColumnName("isc").HasColumnType("numeric(14,2)");
            entity.Property(e => e.BiGravIvap).HasColumnName("base_imponible_ivap").HasColumnType("numeric(14,2)");
            entity.Property(e => e.Ivap).HasColumnName("ivap").HasColumnType("numeric(14,2)");
            entity.Property(e => e.Icbper).HasColumnName("icbper").HasColumnType("numeric(14,2)");
            entity.Property(e => e.OtrosTributos).HasColumnName("otros_tributos").HasColumnType("numeric(14,2)");
            entity.Property(e => e.TotalCp).HasColumnName("total_comprobante").HasColumnType("numeric(14,2)").IsRequired();
            entity.Property(e => e.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
            entity.Property(e => e.TipoCambio).HasColumnName("tipo_cambio").HasColumnType("numeric(10,4)");
            entity.Property(e => e.FechaEmisionDocModif).HasColumnName("fecha_emision_doc_modificado").HasColumnType("date");
            entity.Property(e => e.TipoCpModificado).HasColumnName("tipo_cp_modificado").HasMaxLength(2);
            entity.Property(e => e.SerieCpModificado).HasColumnName("serie_cp_modificado").HasMaxLength(10);
            entity.Property(e => e.NroCpModificado).HasColumnName("nro_cp_modificado").HasMaxLength(20);
            entity.Property(e => e.IdProyectoOpAttr).HasColumnName("id_proyecto_operadores_atribucion").HasMaxLength(50);
            entity.Property(e => e.ValorFobEmbar).HasColumnName("valor_fob_embarcado").HasColumnType("numeric(14,2)");
            entity.Property(e => e.ValorOpGratuitas).HasColumnName("valor_operaciones_gratuitas").HasColumnType("numeric(14,2)");
            entity.Property(e => e.TipoOperacion).HasColumnName("tipo_operacion").HasMaxLength(4);
            entity.Property(e => e.DamCp).HasColumnName("dam_cp").HasMaxLength(20);
            entity.Property(e => e.CodigoTipoNota).HasColumnName("codigo_tipo_nota").HasMaxLength(2);
            entity.Property(e => e.CodigoEstadoComprobante).HasColumnName("codigo_estado_comprobante").HasMaxLength(2).IsRequired();
            entity.Property(e => e.CamposLibres).HasColumnName("campos_libres");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por").HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ModificadoPor).HasColumnName("modificado_por").HasMaxLength(150);
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.HasQueryFilter(e => e.Activo);

            entity.HasIndex(e => new { e.EmpresaRuc, e.Periodo, e.CarSunat })
                .HasDatabaseName("idx_venta_car_sunat");
            entity.HasIndex(e => e.IdCarga).HasDatabaseName("idx_venta_carga");
            entity.HasIndex(e => new { e.Serie, e.Numero }).HasDatabaseName("idx_venta_serie_num");
        });
    }
}
