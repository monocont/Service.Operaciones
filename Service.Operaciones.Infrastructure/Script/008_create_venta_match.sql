-- ============================================================================
-- Service.Operaciones - 008 - Tabla venta_match
-- Almacena los resultados del proceso de Match entre Ventas SIRE y Ventas Empresa
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.venta_match (
    id_venta_match                      UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(20) NOT NULL,
    periodo                             VARCHAR(20) NOT NULL,
    numero_linea                        INT NOT NULL,

    -- Banderas y origen de Match
    origen_dato                         VARCHAR(20) NOT NULL, -- 'SIRE' o 'EMPRESA'
    es_coincidencia_exacta              BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Ideal (coincide todo)
    es_diferencia                       BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Erróneo (discrepancias)
    es_solo_un_origen                   BOOLEAN NOT NULL DEFAULT FALSE, -- Caso Ideal Parcial (solo en SIRE o solo en Empresa)

    -- Campos de Comprobante (Estructura estándar de venta)
    car_sunat                           VARCHAR(40),
    fecha_emision                       DATE NOT NULL,
    fecha_vencimiento                   DATE,
    codigo_tipo_cp                      VARCHAR(20) NOT NULL,
    serie                               VARCHAR(20) NOT NULL,
    numero                              VARCHAR(20) NOT NULL,
    numero_final                        VARCHAR(20),
    codigo_tipo_doc_identidad           VARCHAR(20) NOT NULL,
    nro_doc_identidad                   VARCHAR(20) NOT NULL,
    razon_social                        VARCHAR(1500) NOT NULL,

    -- Montos e Impuestos
    valor_facturado_exportacion         NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravada                          NUMERIC(18,8) NOT NULL DEFAULT 0,
    descuento_bi                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    igv_ipm                             NUMERIC(18,8) NOT NULL DEFAULT 0,
    descuento_igv                       NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_exonerado                     NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_inafecto                      NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_isc                           NUMERIC(18,8) NOT NULL DEFAULT 0,
    bi_gravada_ivap                     NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_ivap                          NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_icbper                        NUMERIC(18,8) NOT NULL DEFAULT 0,
    monto_otros_tributos                NUMERIC(18,8) NOT NULL DEFAULT 0,
    total_cp                            NUMERIC(18,8) NOT NULL DEFAULT 0,
    codigo_moneda                       VARCHAR(20) NOT NULL DEFAULT 'PEN',
    tipo_cambio                         NUMERIC(18,8),

    -- Referencias de Documentos Modificados
    fecha_emision_doc_modificado        DATE,
    codigo_tipo_cp_modificado           VARCHAR(20),
    serie_cp_modificado                 VARCHAR(20),
    numero_cp_modificado                VARCHAR(20),

    -- Estados SUNAT y Clasificación
    codigo_estado_comprobante           VARCHAR(20) NOT NULL DEFAULT '1',
    codigo_tipo_nota                    VARCHAR(20),
    tipo_operacion                      VARCHAR(20),
    campos_libres                       VARCHAR(500),

    -- Auditoría Estándar
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_venta_match PRIMARY KEY (id_venta_match),
    CONSTRAINT fk_venta_match_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_venta_match_carga ON operaciones.venta_match (id_carga);
CREATE INDEX IF NOT EXISTS idx_venta_match_serie_num ON operaciones.venta_match (serie, numero);
CREATE INDEX IF NOT EXISTS idx_venta_match_empresa_periodo ON operaciones.venta_match (empresa_ruc, periodo);
