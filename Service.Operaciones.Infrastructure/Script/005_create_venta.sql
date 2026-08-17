-- ============================================================================
-- Service.Operaciones - 005 - Tabla venta
-- Layout oficial SUNAT Registro de Ventas e Ingresos Electrónico (RVIE)
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.venta (
    id_venta                            UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(11) NOT NULL,
    periodo                             VARCHAR(6) NOT NULL,
    car_sunat                           VARCHAR(40) NOT NULL,
    codigo_tipo_cp                      VARCHAR(2) NOT NULL,
    serie                               VARCHAR(10) NOT NULL,
    numero                              VARCHAR(20) NOT NULL,
    numero_final                        VARCHAR(20),
    fecha_emision                       DATE NOT NULL,
    fecha_vencimiento_pago              DATE,
    codigo_tipo_doc_identidad           VARCHAR(2) NOT NULL,
    nro_doc_identidad                   VARCHAR(15) NOT NULL,
    razon_social                        VARCHAR(200) NOT NULL,
    valor_facturado_exportacion         NUMERIC(14,2) NOT NULL DEFAULT 0,
    base_imponible_gravada              NUMERIC(14,2) NOT NULL DEFAULT 0,
    descuento_base_imponible            NUMERIC(14,2) NOT NULL DEFAULT 0,
    igv_ipm                             NUMERIC(14,2) NOT NULL DEFAULT 0,
    descuento_igv_ipm                   NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_exonerado                     NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_inafecto                      NUMERIC(14,2) NOT NULL DEFAULT 0,
    isc                                 NUMERIC(14,2) NOT NULL DEFAULT 0,
    base_imponible_ivap                 NUMERIC(14,2) NOT NULL DEFAULT 0,
    ivap                                NUMERIC(14,2) NOT NULL DEFAULT 0,
    icbper                              NUMERIC(14,2) NOT NULL DEFAULT 0,
    otros_tributos                      NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_comprobante                   NUMERIC(14,2) NOT NULL,
    codigo_moneda                       VARCHAR(3) NOT NULL,
    tipo_cambio                         NUMERIC(10,4) NOT NULL DEFAULT 1.0000,
    fecha_emision_doc_modificado        DATE,
    tipo_cp_modificado                  VARCHAR(2),
    serie_cp_modificado                 VARCHAR(10),
    nro_cp_modificado                   VARCHAR(20),
    id_proyecto_operadores_atribucion   VARCHAR(50),
    valor_fob_embarcado                 NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_operaciones_gratuitas         NUMERIC(14,2) NOT NULL DEFAULT 0,
    tipo_operacion                      VARCHAR(4),
    dam_cp                              VARCHAR(20),
    codigo_tipo_nota                    VARCHAR(2),
    codigo_estado_comprobante           VARCHAR(2) NOT NULL,
    campos_libres                       TEXT,
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_venta PRIMARY KEY (id_venta),
    CONSTRAINT fk_ve_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_venta_carga         ON operaciones.venta (id_carga);
CREATE INDEX IF NOT EXISTS idx_venta_car_sunat     ON operaciones.venta (empresa_ruc, periodo, car_sunat);
CREATE INDEX IF NOT EXISTS idx_venta_empresa_per   ON operaciones.venta (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_venta_serie_num     ON operaciones.venta (serie, numero);
CREATE INDEX IF NOT EXISTS idx_venta_cliente       ON operaciones.venta (nro_doc_identidad, fecha_emision);
