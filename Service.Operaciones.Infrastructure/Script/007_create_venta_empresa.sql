-- ============================================================================
-- Service.Operaciones - 007 - Tabla venta_empresa
-- Estructura para almacenar los Registros Internos de Ventas de la Empresa (Plantilla Excel)
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.venta_empresa (
    id_venta_empresa                    UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga                            UUID NOT NULL,
    empresa_ruc                         VARCHAR(11) NOT NULL,
    periodo                             VARCHAR(6) NOT NULL,
    numero_linea                        INT NOT NULL,
    fecha_emision                       DATE NOT NULL,
    codigo_tipo_cp                      VARCHAR(2) NOT NULL,
    serie                               VARCHAR(10) NOT NULL,
    numero                              VARCHAR(20) NOT NULL,
    codigo_tipo_doc_identidad           VARCHAR(2) NOT NULL,
    nro_doc_identidad                   VARCHAR(15) NOT NULL,
    total_comprobante                   NUMERIC(14,2) NOT NULL,
    codigo_moneda                       VARCHAR(3) NOT NULL,
    tipo_cambio                         NUMERIC(10,4) NOT NULL DEFAULT 1.0000,
    creado_por                          VARCHAR(150),
    fecha_creacion                      TIMESTAMP,
    modificado_por                      VARCHAR(150),
    fecha_modificacion                  TIMESTAMP,
    activo                              BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_venta_empresa PRIMARY KEY (id_venta_empresa),
    CONSTRAINT fk_ve_empresa_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_ve_carga              ON operaciones.venta_empresa (id_carga);
CREATE INDEX IF NOT EXISTS idx_ve_empresa_periodo    ON operaciones.venta_empresa (empresa_ruc, periodo, fecha_emision);
CREATE INDEX IF NOT EXISTS idx_ve_cruce_match        ON operaciones.venta_empresa (empresa_ruc, periodo, codigo_tipo_cp, serie, numero);
CREATE INDEX IF NOT EXISTS idx_ve_cliente            ON operaciones.venta_empresa (nro_doc_identidad, fecha_emision);
