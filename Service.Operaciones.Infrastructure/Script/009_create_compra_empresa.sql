-- ============================================================================
-- Service.Operaciones - 009 - Tabla compra_empresa
-- Estructura homologada para almacenar los Registros de Compras de la Empresa (ERP / Plantilla)
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.compra_empresa (
    -- Claves y Trazabilidad de Archivo
    id_compra_empresa                   UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(20) NOT NULL,
    periodo                             VARCHAR(20) NOT NULL,
    numero_linea                        INT NOT NULL,

    -- Datos Principales del Comprobante y Proveedor
    car_sunat                           VARCHAR(40),
    fecha_emision                       DATE NOT NULL,
    fecha_vencimiento                   DATE,
    codigo_tipo_cp                      VARCHAR(20) NOT NULL,
    serie                               VARCHAR(20) NOT NULL,
    anio_documento                      VARCHAR(20),
    numero                              VARCHAR(20) NOT NULL,
    numero_final                        VARCHAR(20),
    codigo_tipo_doc_identidad           VARCHAR(20) NOT NULL,
    nro_doc_identidad                   VARCHAR(20) NOT NULL,
    razon_social                        VARCHAR(1500) NOT NULL DEFAULT '',

    -- Bases Imponibles, Impuestos y Totales de Compras
    bi_gravado_dg                       NUMERIC(18,8) NOT NULL DEFAULT 0,
    igv_ipm_dg                          NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravado_dgng                     NUMERIC(18,8) NOT NULL DEFAULT 0,
    igv_ipm_dgng                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravado_dng                      NUMERIC(18,8) NOT NULL DEFAULT 0,
    igv_ipm_dng                         NUMERIC(18,8) NOT NULL DEFAULT 0,
    valor_adq_ng                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_isc                           NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_icbper                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_otros_tributos                NUMERIC(18,8) NOT NULL DEFAULT 0,
    total_cp                            NUMERIC(18,8) NOT NULL DEFAULT 0,

    -- Moneda y Tipo de Cambio
    codigo_moneda                       VARCHAR(20) NOT NULL DEFAULT 'PEN',
    tipo_cambio                         NUMERIC(18,8),

    -- Documentos Modificados / Referencias
    fecha_emision_doc_modificado        DATE,
    codigo_tipo_cp_modificado           VARCHAR(20),
    serie_cp_modificado                 VARCHAR(20),
    cod_dam_dsi                         VARCHAR(20),
    numero_cp_modificado                VARCHAR(20),

    -- Atributos Específicos de Compras RCE
    clasif_bss_sss                      VARCHAR(20),
    id_proyecto_op                      VARCHAR(50),
    porc_part                           NUMERIC(18,8),
    imb                                 NUMERIC(18,8) NOT NULL DEFAULT 0,
    car_orig_ind_e_i                    VARCHAR(40),
    detraccion                          VARCHAR(50),
    codigo_tipo_nota                    VARCHAR(20),
    codigo_estado_comprobante           VARCHAR(20) NOT NULL DEFAULT '1',
    incal                               VARCHAR(20),

    -- Campos Libres y Auditoría
    campos_libres                       VARCHAR(500),
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_compra_empresa PRIMARY KEY (id_compra_empresa),
    CONSTRAINT fk_ce_empresa_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

-- Índices optimizados para consulta y cruce (Match)
CREATE INDEX IF NOT EXISTS idx_ce_carga            ON operaciones.compra_empresa (id_carga);
CREATE INDEX IF NOT EXISTS idx_ce_empresa_periodo  ON operaciones.compra_empresa (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_ce_cruce_match      ON operaciones.compra_empresa (empresa_ruc, periodo, nro_doc_identidad, codigo_tipo_cp, serie, numero);
CREATE INDEX IF NOT EXISTS idx_ce_proveedor        ON operaciones.compra_empresa (nro_doc_identidad, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_ce_car_sunat        ON operaciones.compra_empresa (car_sunat);
