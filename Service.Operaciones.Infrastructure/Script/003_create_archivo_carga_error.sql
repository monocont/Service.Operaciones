-- ============================================================================
-- Service.Operaciones - 003 - Tabla archivo_carga_error
-- Registro de errores por línea durante el procesamiento del archivo
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.archivo_carga_error (
    id_error        UUID NOT NULL DEFAULT gen_random_uuid(),
    id_carga        UUID NOT NULL,
    numero_linea    INT NOT NULL,
    tipo_error      VARCHAR(20) NOT NULL,
    campo_error     VARCHAR(50),
    valor_lectura   VARCHAR(500),
    mensaje         VARCHAR(500) NOT NULL,
    severidad       VARCHAR(7) NOT NULL DEFAULT 'Error',
    fecha_registro  TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_archivo_carga_error PRIMARY KEY (id_error),
    CONSTRAINT fk_ace_carga FOREIGN KEY (id_carga)
        REFERENCES operaciones.archivo_carga(id_carga) ON DELETE CASCADE,
    CONSTRAINT chk_ace_severidad CHECK (severidad IN ('Error','Warning')),
    CONSTRAINT chk_ace_tipo CHECK (tipo_error IN ('Formato','Validacion','Duplicado','Negocio','Secuencia'))
);

CREATE INDEX IF NOT EXISTS idx_ace_carga
    ON operaciones.archivo_carga_error (id_carga);
