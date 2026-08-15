-- ============================================================================
-- Service.Operaciones - 005 - Tabla venta
-- Layout oficial SUNAT Registro de Ventas LE (41 columnas)
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.venta (
    id_venta                UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                UUID NOT NULL,
    empresa_ruc             VARCHAR(11) NOT NULL,
    periodo                 VARCHAR(6) NOT NULL,
    car_sunat               VARCHAR(40) NOT NULL,
    codigo_tipo_cp          VARCHAR(2) NOT NULL,
    serie                   VARCHAR(10) NOT NULL,
    numero                  VARCHAR(20) NOT NULL,
    numero_final            VARCHAR(20),
    fecha_emision           DATE NOT NULL,
    fecha_vcto_pago         DATE,
    codigo_tipo_doc_identidad VARCHAR(2) NOT NULL,
    nro_doc_identidad       VARCHAR(15) NOT NULL,
    razon_social            VARCHAR(200) NOT NULL,
    valor_fact_exp          NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravada              NUMERIC(14,2) NOT NULL DEFAULT 0,
    dscto_bi                 NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm                  NUMERIC(14,2) NOT NULL DEFAULT 0,
    dscto_igv_ipm            NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_exonerado          NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_inafecto           NUMERIC(14,2) NOT NULL DEFAULT 0,
    isc                      NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_grav_ivap             NUMERIC(14,2) NOT NULL DEFAULT 0,
    ivap                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    icbper                   NUMERIC(14,2) NOT NULL DEFAULT 0,
    otros_tributos           NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_cp                 NUMERIC(14,2) NOT NULL,
    codigo_moneda            VARCHAR(3) NOT NULL,
    tipo_cambio              NUMERIC(10,4) NOT NULL DEFAULT 1.0000,
    fecha_emision_doc_modif  DATE,
    tipo_cp_modificado       VARCHAR(2),
    serie_cp_modificado      VARCHAR(10),
    nro_cp_modificado        VARCHAR(20),
    id_proyecto_op_attr      VARCHAR(50),
    valor_fob_embar           NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_op_gratuitas       NUMERIC(14,2) NOT NULL DEFAULT 0,
    tipo_operacion            VARCHAR(4),
    dam_cp                    VARCHAR(20),
    codigo_tipo_nota          VARCHAR(2),
    codigo_estado_comprobante VARCHAR(2) NOT NULL,
    creado_por               VARCHAR(150),
    fecha_creacion           TIMESTAMP,
    modificado_por           VARCHAR(150),
    fecha_modificacion       TIMESTAMP,
    activo                   BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_venta PRIMARY KEY (id_venta),
    CONSTRAINT uq_venta_car_sunat UNIQUE (empresa_ruc, periodo, car_sunat),
    CONSTRAINT fk_ve_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_venta_carga         ON operaciones.venta (id_carga);
CREATE INDEX IF NOT EXISTS idx_venta_empresa_per   ON operaciones.venta (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_venta_serie_num     ON operaciones.venta (serie, numero);
CREATE INDEX IF NOT EXISTS idx_venta_cliente      ON operaciones.venta (nro_doc_identidad, fecha_emision);
