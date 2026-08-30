-- ============================================================================
-- Service.Operaciones - 002 - Tabla tipo_operacion (Catálogo de Tipos de Operación)
-- Identifica las operaciones de Ventas, Compras y Cruce/Match
-- ============================================================================

CREATE TABLE IF NOT EXISTS operaciones.tipo_operacion (
    id_tipo_operacion      INT NOT NULL,
    codigo                 VARCHAR(30) NOT NULL,
    nombre                 VARCHAR(100) NOT NULL,
    modulo                 VARCHAR(20) NOT NULL,
    descripcion            VARCHAR(250),
    creado_por             VARCHAR(150) DEFAULT 'sistema',
    fecha_creacion         TIMESTAMP DEFAULT NOW(),
    modificado_por         VARCHAR(150),
    fecha_modificacion     TIMESTAMP,
    activo                 BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_tipo_operacion PRIMARY KEY (id_tipo_operacion),
    CONSTRAINT uq_to_codigo UNIQUE (codigo)
);

-- ============================================================================
-- Poblado de datos iniciales
-- ============================================================================
INSERT INTO operaciones.tipo_operacion (id_tipo_operacion, codigo, nombre, modulo, descripcion, creado_por, fecha_creacion, activo)
VALUES
    (1001, 'VENTA_SIRE',     'Venta SIRE - SUNAT',     'Ventas',  'Propuesta de Ventas descargada de SUNAT SIRE', 'sistema', NOW(), TRUE),
    (1002, 'VENTA_EMPRESA',  'Venta Empresa',          'Ventas',  'Registros Internos de Ventas de la Empresa',    'sistema', NOW(), TRUE),
    (1003, 'VENTA_MATCH',    'Match Ventas',           'Ventas',  'Resultado de Conciliación SIRE vs Empresa',    'sistema', NOW(), TRUE),
    (2001, 'COMPRA_SIRE',    'Compra SIRE - SUNAT',    'Compras', 'Propuesta de Compras descargada de SUNAT SIRE', 'sistema', NOW(), TRUE),
    (2002, 'COMPRA_EMPRESA', 'Compra Empresa',         'Compras', 'Registros Internos de Compras de la Empresa',   'sistema', NOW(), TRUE),
    (2003, 'COMPRA_MATCH',   'Match Compras',          'Compras', 'Resultado de Conciliación SIRE vs Empresa',    'sistema', NOW(), TRUE)
ON CONFLICT (id_tipo_operacion) DO NOTHING;
