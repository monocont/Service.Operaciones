-- ============================================================================
-- Service.Operaciones - 003 - Tabla archivo_carga
-- Audita cada carga de archivo SUNAT / Empresa / Match realizada por un usuario
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.archivo_carga (
    id_carga               UUID NOT NULL DEFAULT gen_random_uuid(),
    empresa_ruc            VARCHAR(11) NOT NULL,
    periodo                VARCHAR(6) NOT NULL,
    id_tipo_operacion      INT NOT NULL,
    formato                VARCHAR(4) NOT NULL,
    nombre_original        VARCHAR(255) NOT NULL,
    hash_documento         VARCHAR(64) NOT NULL,
    num_registros          INT NOT NULL DEFAULT 0,
    num_registros_validos  INT NOT NULL DEFAULT 0,
    num_registros_error    INT NOT NULL DEFAULT 0,
    total_base_imponible   NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_igv              NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_general          NUMERIC(14,2) NOT NULL DEFAULT 0,
    estado                 VARCHAR(12) NOT NULL DEFAULT 'Procesando',
    observaciones          VARCHAR(500),
    creado_por             VARCHAR(150),
    fecha_creacion         TIMESTAMP,
    modificado_por         VARCHAR(150),
    fecha_modificacion     TIMESTAMP,
    activo                 BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_archivo_carga PRIMARY KEY (id_carga),
    CONSTRAINT fk_ac_tipo_operacion FOREIGN KEY (id_tipo_operacion)
        REFERENCES operaciones.tipo_operacion(id_tipo_operacion),
    CONSTRAINT uq_ac_hash UNIQUE (empresa_ruc, periodo, id_tipo_operacion, hash_documento),
    CONSTRAINT chk_ac_formato CHECK (formato IN ('Txt','Csv','Xlsx','Xls')),
    CONSTRAINT chk_ac_estado CHECK (estado IN ('Procesando','Ok','Error','Duplicado'))
);

CREATE INDEX IF NOT EXISTS idx_ac_empresa_periodo
    ON operaciones.archivo_carga (empresa_ruc, periodo, id_tipo_operacion);
