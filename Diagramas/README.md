# Diagramas del modelo — base `belissimo`

Los cinco diagramas representan **el mismo modelo**: 11 tablas y 8 llaves foráneas, tomado de
`information_schema` de MySQL 8.0.46 después de reconstruir con `Sql/01–04`.

| # | Notación | Archivo | Cómo se obtuvo |
| - | -------- | ------- | -------------- |
| 1 | Crow's Foot | `crows_foot.png` / `.pdf` | Generado por script (misma distribución en todos) |
| 2 | Classic | `workbench/02_classic.png` | **Captura de MySQL Workbench** (pasos abajo) |
| 3 | Connect to Columns | `connect_to_columns.png` / `.pdf` | Generado por script |
| 4 | UML | `uml.png` / `.pdf` | Generado por script |
| 5 | IDEF1X | `idef1x.png` / `.pdf` | Generado por script |

**Fuentes editables:** `fuentes/modelo.json` (tablas, columnas y llaves exportadas de MySQL con
`fuentes/exportar_modelo.py`) y `fuentes/generar_diagramas.py` (dibuja las 4 notaciones).
Si cambia el esquema:

```powershell
$env:MYSQL_PWD = '<contraseña>'          # solo en la sesión, nunca en un archivo
python -I fuentes/exportar_modelo.py belissimo fuentes/modelo.json
python -I fuentes/generar_diagramas.py fuentes/modelo.json .
```

La notación **Classic** es propia de Workbench y no se dibuja por script para no inventar su
simbología: se toma de Workbench junto con las otras cuatro para el reporte, si el maestro las
quiere todas desde la misma herramienta.

## ¿Qué notación es más apropiada?

**Crow's Foot.** Muestra en el mismo símbolo la **cardinalidad máxima** (uno o muchos) y la
**participación** (obligatoria `|` u opcional `o`), que es justo lo que importa en este modelo.
Por ejemplo, `pago_semanal.id_usuario` es opcional (`o|`) y `pago_semanal.id_empleado` es
obligatorio (`||`). Además es la notación por omisión de Workbench y la más leída en la industria.

- **Connect to Columns** sirve para revisar **qué columna** implementa cada relación. Con muchas
  FK hacia `empleado.id_empleado` las líneas se juntan en el mismo renglón, así que es peor para
  ver el modelo completo.
- **UML** expresa lo mismo con multiplicidades (`1`, `0..1`, `0..*`), pero es una notación de
  clases: no distingue PK/FK de forma nativa (se usan estereotipos `{PK}` `{FK}`).
- **IDEF1X** es la más precisa formalmente: separa el área de llave, marca llaves alternas (AK) y
  distingue relaciones identificadoras (línea continua) de no identificadoras (punteada). Aquí
  todas son no identificadoras porque cada tabla tiene su propia PK sustituta (`AUTO_INCREMENT`).
- **Classic** sirve para ver dirección y cardinalidad básica, pero no muestra la participación con
  tanta claridad.

## Pasos en MySQL Workbench (Classic, `.mwb` nuevo y capturas de las 5 notaciones)

El `Floreria_Checador.mwb` anterior no abre en Workbench y además es de un esquema viejo (sin
índices, comentarios ni cambios del 2026-11-16). Se reemplaza así:

1. Reconstruir la base `belissimo` con `Sql/01` a `04`.
2. Workbench → **Database → Reverse Engineer...** → conexión *Local instance MySQL80* → Next →
   marcar **belissimo** → Next → Next → en *Select Objects* dejar **Import MySQL Table Objects**
   (y vistas, rutinas y triggers si se desea) con **Place imported objects on a diagram** →
   Execute → Next → Finish.
3. Acomodar: **Arrange → Autolayout**, o mover las tablas como en `crows_foot.png`.
4. Guardar: **File → Save Model As...** → `Diagramas/belissimo.mwb`.
5. Para cada notación, primero **Model → Object Notation** y luego **Model → Relationship Notation**,
   después **File → Export → Export as PNG...**:

| Captura | Object Notation | Relationship Notation | Guardar como |
| ------- | --------------- | --------------------- | ------------ |
| 1 | Workbench (Default) | Crow's Foot | `Diagramas/workbench/01_crows_foot.png` |
| 2 | Classic | Classic | `Diagramas/workbench/02_classic.png` |
| 3 | Workbench (Default) | Connect to Columns | `Diagramas/workbench/03_connect_to_columns.png` |
| 4 | Workbench (Default) | UML | `Diagramas/workbench/04_uml.png` |
| 5 | IDEF1X | IDEF1X | `Diagramas/workbench/05_idef1x.png` |

   Opcional: **File → Export → Export as Single Page PDF...** con el mismo nombre en `.pdf`.
6. Revisar que en cada captura se vean las **11 tablas** y las **8 relaciones**.
