-- ============================================================================
-- Service.Operaciones - 010 - Tabla compra_match
-- Almacena los resultados del proceso de Match entre Compras SIRE y Compras Empresa
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.compra_match (
    -- 1. Clave Primaria e Identificadores de Control de Carga
    id_compra_match                     UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(20) NOT NULL,
    periodo                             VARCHAR(20) NOT NULL,
    numero_linea                        INT NOT NULL,

    -- 2. Banderas y origen de Match
    origen_dato                         VARCHAR(20) NOT NULL, -- 'SIRE' o 'EMPRESA'
    es_coincidencia_exacta              BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Ideal (coincide todo)
    es_diferencia                       BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Erróneo (discrepancias)
    es_solo_un_origen                   BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Ideal Parcial (solo en SIRE o solo en Empresa)

    -- 3. Identificación del Comprobante
    car_sunat                           VARCHAR(40),
    fecha_emision                       DATE NOT NULL,
    fecha_vencimiento                   DATE,
    codigo_tipo_cp                      VARCHAR(20) NOT NULL, -- 01: Factura, 03: Boleta, 07: NC, 08: ND, etc.
    serie                               VARCHAR(20) NOT NULL,
    anio_documento                      VARCHAR(20),
    numero                              VARCHAR(20) NOT NULL, -- Número normalizado sin ceros a la izquierda
    numero_final                        VARCHAR(20),

    -- 4. Datos del Proveedor
    codigo_tipo_doc_identidad           VARCHAR(20) NOT NULL, -- 6: RUC, 1: DNI, etc.
    nro_doc_identidad                   VARCHAR(20) NOT NULL,
    razon_social                        VARCHAR(1500) NOT NULL DEFAULT '',

    -- 5. Bases Imponibles, Impuestos y Totales de Compras (RCE)
    bi_gravado_dg                       NUMERIC(18,8) NOT NULL DEFAULT 0, -- Adquisiciones gravadas para ventas gravadas
    igv_ipm_dg                          NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravado_dgng                     NUMERIC(18,8) NOT NULL DEFAULT 0, -- Adquisiciones gravadas para ventas gravadas y no gravadas (prorrata)
    igv_ipm_dgng                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravado_dng                      NUMERIC(18,8) NOT NULL DEFAULT 0, -- Adquisiciones gravadas para ventas no gravadas
    igv_ipm_dng                         NUMERIC(18,8) NOT NULL DEFAULT 0,
    valor_adq_ng                        NUMERIC(18,8) NOT NULL DEFAULT 0, -- Adquisiciones no gravadas
    monto_isc                           NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_icbper                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_otros_tributos                NUMERIC(18,8) NOT NULL DEFAULT 0,
    total_cp                            NUMERIC(18,8) NOT NULL DEFAULT 0,

    -- 6. Moneda y Tipo de Cambio
    codigo_moneda                       VARCHAR(20) NOT NULL DEFAULT 'PEN',
    tipo_cambio                         NUMERIC(18,8),

    -- 7. Documento Modificado / Referencias (para Notas de Crédito / Débito)
    fecha_emision_doc_modificado        DATE,
    codigo_tipo_cp_modificado           VARCHAR(20),
    serie_cp_modificado                 VARCHAR(20),
    cod_dam_dsi                         VARCHAR(20),
    numero_cp_modificado                VARCHAR(20),

    -- 8. Atributos Específicos de Compras RCE
    clasif_bss_sss                      VARCHAR(20),
    id_proyecto_op                      VARCHAR(50),
    porc_part                           NUMERIC(18,8),
    imb                                 NUMERIC(18,8) NOT NULL DEFAULT 0,
    car_orig_ind_e_i                    VARCHAR(40),
    detraccion                          VARCHAR(50),
    codigo_tipo_nota                    VARCHAR(20),
    codigo_estado_comprobante           VARCHAR(20) NOT NULL DEFAULT '1',
    incal                               VARCHAR(20),
    campos_libres                       VARCHAR(500),

    -- 9. Auditoría Estándar
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_compra_match PRIMARY KEY (id_compra_match),
    CONSTRAINT fk_compra_match_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

-- =============================================================================
-- ÍNDICES OPTIMIZADOS PARA CONSULTA Y MATCH
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_cm_carga            ON operaciones.compra_match (id_carga);
CREATE INDEX IF NOT EXISTS idx_cm_empresa_periodo  ON operaciones.compra_match (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_cm_cruce_match      ON operaciones.compra_match (empresa_ruc, periodo, nro_doc_identidad, codigo_tipo_cp, serie, numero);
CREATE INDEX IF NOT EXISTS idx_cm_proveedor        ON operaciones.compra_match (nro_doc_identidad, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_cm_car_sunat        ON operaciones.compra_match (car_sunat);
