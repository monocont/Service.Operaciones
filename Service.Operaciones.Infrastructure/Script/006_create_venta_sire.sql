-- ============================================================================
-- Service.Operaciones - 006 - Tabla venta_sire
-- Almacena los comprobantes de ventas (RVIE) procesados desde SUNAT SIRE
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.venta_sire (
    id_venta                            UUID NOT NULL DEFAULT gen_random_uuid(),
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
    numero_final                        VARCHAR(20),
    codigo_tipo_doc_identidad           VARCHAR(2) NOT NULL,
    nro_doc_identidad                   VARCHAR(15) NOT NULL,
    razon_social                        VARCHAR(1500) NOT NULL,
    valor_facturado_exportacion         NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravada                          NUMERIC(14,2) NOT NULL DEFAULT 0,
    descuento_bi                        NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm                             NUMERIC(14,2) NOT NULL DEFAULT 0,
    descuento_igv                       NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_exonerado                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_inafecto                      NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_isc                           NUMERIC(14,2) NOT NULL DEFAULT 0,
    bi_gravada_ivap                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_ivap                          NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_icbper                        NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_otros_tributos                NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_cp                            NUMERIC(14,2) NOT NULL DEFAULT 0,
    codigo_moneda                       VARCHAR(3) NOT NULL,
    tipo_cambio                         NUMERIC(10,4),
    fecha_emision_doc_modificado        DATE,
    codigo_tipo_cp_modificado           VARCHAR(2),
    serie_cp_modificado                 VARCHAR(10),
    numero_cp_modificado                VARCHAR(20),
    codigo_estado_comprobante           VARCHAR(2) NOT NULL DEFAULT '1',
    codigo_tipo_nota                    VARCHAR(2),
    tipo_operacion                      VARCHAR(2),
    campos_libres                       VARCHAR(500),
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_venta_sire PRIMARY KEY (id_venta),
    CONSTRAINT fk_venta_sire_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_venta_sire_carga ON operaciones.venta_sire (id_carga);
CREATE INDEX IF NOT EXISTS idx_venta_sire_serie_num ON operaciones.venta_sire (serie, numero);
