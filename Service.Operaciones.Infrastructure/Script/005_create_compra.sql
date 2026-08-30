-- ============================================================================
-- Service.Operaciones - 005 - Tabla compra
-- Almacena los comprobantes de compras (RCE) procesados desde SUNAT
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.compra (
    id_compra                           UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(11) NOT NULL,
    periodo                             VARCHAR(6) NOT NULL,
    numero_linea                        INT NOT NULL,
    car_sunat                           VARCHAR(35) NOT NULL,
    fecha_emision                       DATE NOT NULL,
    fecha_vencimiento                   DATE,
    codigo_tipo_cp                      VARCHAR(2) NOT NULL,
    serie                               VARCHAR(10) NOT NULL,
    numero                              VARCHAR(20) NOT NULL,
    codigo_tipo_doc_identidad           VARCHAR(2) NOT NULL,
    nro_doc_identidad                   VARCHAR(15) NOT NULL,
    razon_social                        VARCHAR(1500) NOT NULL,
    bi_gravado_dg                       NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dg                          NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravado_dgng                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dgng                        NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravado_dng                      NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm_dng                         NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_adq_ng                        NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_isc                           NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_icbper                        NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_otros_tributos                NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_cp                            NUMERIC(14,2) NOT NULL DEFAULT 0,
    codigo_moneda                       VARCHAR(3) NOT NULL,
    tipo_cambio                         NUMERIC(10,4),
    fecha_emision_doc_modificado        DATE,
    codigo_tipo_cp_modificado           VARCHAR(2),
    serie_cp_modificado                 VARCHAR(10),
    numero_cp_modificado                VARCHAR(20),
    clasif_bss_sss                      VARCHAR(10),
    codigo_estado_comprobante           VARCHAR(2) NOT NULL DEFAULT '1',
    detraccion                          VARCHAR(50),
    codigo_tipo_nota                    VARCHAR(2),
    es_operacion_sujeta_detraccion      BOOLEAN,
    tipo_operacion                      VARCHAR(2),
    campos_libres                       VARCHAR(500),
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_compra PRIMARY KEY (id_compra),
    CONSTRAINT fk_compra_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_compra_carga ON operaciones.compra (id_carga);
CREATE INDEX IF NOT EXISTS idx_compra_serie_num ON operaciones.compra (serie, numero);
