/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : diccionario_datos.sql
   Motor   : MySQL 8.0

   Genera el diccionario de datos DIRECTAMENTE de information_schema
   (no se escribe a mano: siempre coincide con la base real).
   Una fila por columna: tabla, atributo, significado (COMMENT), tipo,
   longitud, nulo, PK, FK, valor por omisión y restricciones.
   ===================================================================== */

USE belissimo;

-- 1) Tablas con su descripción
SELECT t.TABLE_NAME AS tabla,
       t.TABLE_COMMENT AS descripcion,
       t.ENGINE AS motor,
       t.TABLE_COLLATION AS intercalacion
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = DATABASE()
  AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME;

-- 2) Diccionario de columnas
SELECT
    c.TABLE_NAME  AS tabla,
    c.COLUMN_NAME AS atributo,
    c.COLUMN_COMMENT AS significado,
    c.COLUMN_TYPE AS tipo,
    COALESCE(CAST(c.CHARACTER_MAXIMUM_LENGTH AS CHAR),
             CASE WHEN c.NUMERIC_PRECISION IS NOT NULL
                  THEN CONCAT(c.NUMERIC_PRECISION,
                              IF(c.NUMERIC_SCALE > 0, CONCAT(',', c.NUMERIC_SCALE), ''))
             END, '') AS longitud,
    IF(c.IS_NULLABLE = 'YES', 'Sí', 'No') AS nulo,
    IF(c.COLUMN_KEY = 'PRI', 'PK', '') AS pk,
    IFNULL((SELECT CONCAT(k.REFERENCED_TABLE_NAME, '.', k.REFERENCED_COLUMN_NAME,
                          ' (', k.CONSTRAINT_NAME, ')')
            FROM information_schema.KEY_COLUMN_USAGE k
            WHERE k.TABLE_SCHEMA = c.TABLE_SCHEMA
              AND k.TABLE_NAME = c.TABLE_NAME
              AND k.COLUMN_NAME = c.COLUMN_NAME
              AND k.REFERENCED_TABLE_NAME IS NOT NULL
            LIMIT 1), '') AS fk,
    IFNULL(c.COLUMN_DEFAULT, '') AS valor_omision,
    CONCAT_WS(', ',
        NULLIF(c.EXTRA, ''),
        (SELECT GROUP_CONCAT(DISTINCT tc.CONSTRAINT_NAME ORDER BY tc.CONSTRAINT_NAME SEPARATOR ', ')
         FROM information_schema.TABLE_CONSTRAINTS tc
         INNER JOIN information_schema.KEY_COLUMN_USAGE k
                 ON k.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                AND k.CONSTRAINT_NAME   = tc.CONSTRAINT_NAME
                AND k.TABLE_NAME        = tc.TABLE_NAME
         WHERE tc.TABLE_SCHEMA = c.TABLE_SCHEMA
           AND tc.TABLE_NAME = c.TABLE_NAME
           AND tc.CONSTRAINT_TYPE = 'UNIQUE'
           AND k.COLUMN_NAME = c.COLUMN_NAME),
        (SELECT GROUP_CONCAT(tc.CONSTRAINT_NAME ORDER BY tc.CONSTRAINT_NAME SEPARATOR ', ')
         FROM information_schema.TABLE_CONSTRAINTS tc
         INNER JOIN information_schema.CHECK_CONSTRAINTS cc
                 ON cc.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                AND cc.CONSTRAINT_NAME   = tc.CONSTRAINT_NAME
         WHERE tc.TABLE_SCHEMA = c.TABLE_SCHEMA
           AND tc.TABLE_NAME = c.TABLE_NAME
           AND tc.CONSTRAINT_TYPE = 'CHECK'
           AND cc.CHECK_CLAUSE LIKE CONCAT('%`', c.COLUMN_NAME, '`%'))
    ) AS restricciones
FROM information_schema.COLUMNS c
INNER JOIN information_schema.TABLES t
        ON t.TABLE_SCHEMA = c.TABLE_SCHEMA
       AND t.TABLE_NAME = c.TABLE_NAME
       AND t.TABLE_TYPE = 'BASE TABLE'
WHERE c.TABLE_SCHEMA = DATABASE()
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

-- 3) Restricciones CHECK con su expresión
SELECT tc.TABLE_NAME AS tabla, cc.CONSTRAINT_NAME AS restriccion, cc.CHECK_CLAUSE AS expresion
FROM information_schema.CHECK_CONSTRAINTS cc
INNER JOIN information_schema.TABLE_CONSTRAINTS tc
        ON tc.CONSTRAINT_SCHEMA = cc.CONSTRAINT_SCHEMA
       AND tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
WHERE cc.CONSTRAINT_SCHEMA = DATABASE()
ORDER BY tc.TABLE_NAME, cc.CONSTRAINT_NAME;

-- 4) Llaves foráneas con sus reglas de actualización y borrado
SELECT rc.TABLE_NAME AS tabla, rc.CONSTRAINT_NAME AS llave_foranea,
       rc.REFERENCED_TABLE_NAME AS tabla_referenciada,
       rc.UPDATE_RULE AS al_actualizar, rc.DELETE_RULE AS al_borrar
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = DATABASE()
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME;

-- 5) Índices
SELECT s.TABLE_NAME AS tabla, s.INDEX_NAME AS indice,
       GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ', ') AS columnas,
       IF(MAX(s.NON_UNIQUE) = 0, 'Único', 'No único') AS tipo
FROM information_schema.STATISTICS s
WHERE s.TABLE_SCHEMA = DATABASE()
GROUP BY s.TABLE_NAME, s.INDEX_NAME
ORDER BY s.TABLE_NAME, s.INDEX_NAME;
