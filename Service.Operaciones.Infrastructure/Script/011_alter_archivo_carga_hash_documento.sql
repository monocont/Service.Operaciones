-- ============================================================================
-- Service.Operaciones - 011 - Ampliar columna hash_documento en archivo_carga
-- Permite identificadores/hashes descriptivos más largos (ej. Match de Compras/Ventas)
-- ============================================================================

ALTER TABLE operaciones.archivo_carga 
    ALTER COLUMN hash_documento TYPE VARCHAR(250);
