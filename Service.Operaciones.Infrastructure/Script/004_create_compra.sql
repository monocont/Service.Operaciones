-- ============================================================================
-- Service.Operaciones - 004 - Tabla compra
-- Layout oficial SUNAT Registro de Compras (80 columnas)
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.compra (
    id_compra              UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga               UUID NOT NULL,
    empresa_ruc            VARCHAR(11) NOT NULL,
    periodo                VARCHAR(6) NOT NULL,
    car_sunat              VARCHAR(40) NOT NULL,
    codigo_tipo_cp         VARCHAR(2) NOT NULL,
    serie                  VARCHAR(10) NOT NULL,
    numero                 VARCHAR(20) NOT NULL,
    numero_final           VARCHAR(20),
    anio_documento         VARCHAR(4),
    fecha_emision          DATE NOT NULL,
    fecha_vcto_pago        DATE,
    codigo_tipo_doc_identidad VARCHAR(2) NOT NULL,
    nro_doc_identidad      VARCHAR(15) NOT NULL,
    razon_social           VARCHAR(200) NOT NULL,
    bi_gravado_dg          NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dg             NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravado_dgng        NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dgng           NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravado_dng         NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dng            NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_adq_ng            NUMERIC(14,2) NOT NULL DEFAULT 0,
    isc                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    icbper                  NUMERIC(14,2) NOT NULL DEFAULT 0,
    otros_trib_cargos       NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_cp                NUMERIC(14,2) NOT NULL,
    codigo_moneda           VARCHAR(3) NOT NULL,
    tipo_cambio             NUMERIC(10,4) NOT NULL DEFAULT 1.0000,
    fecha_emision_doc_modif DATE,
    tipo_cp_modificado      VARCHAR(2),
    serie_cp_modificado     VARCHAR(10),
    nro_cp_modificado       VARCHAR(20),
    cod_dam_dsi             VARCHAR(20),
    clasif_bss_sss          VARCHAR(10),
    id_proyecto_op          VARCHAR(50),
    porc_part               NUMERIC(5,2),
    imb                     NUMERIC(14,2),
    car_orig_ind_e_i        VARCHAR(20),
    detraccion              VARCHAR(1),
    codigo_tipo_nota        VARCHAR(2),
    codigo_estado_comprobante VARCHAR(2) NOT NULL,
    incal                   VARCHAR(1),
    campos_libres           TEXT,
    creado_por              VARCHAR(150),
    fecha_creacion          TIMESTAMP,
    modificado_por          VARCHAR(150),
    fecha_modificacion      TIMESTAMP,
    activo                  BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_compra PRIMARY KEY (id_compra),
    CONSTRAINT uq_compra_car_sunat UNIQUE (empresa_ruc, periodo, car_sunat),
    CONSTRAINT fk_co_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_compra_carga        ON operaciones.compra (id_carga);
CREATE INDEX IF NOT EXISTS idx_compra_empresa_per  ON operaciones.compra (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_compra_tipo_serie   ON operaciones.compra (codigo_tipo_cp, serie, numero);
CREATE INDEX IF NOT EXISTS idx_compra_proveedor    ON operaciones.compra (nro_doc_identidad, fecha_emision);
