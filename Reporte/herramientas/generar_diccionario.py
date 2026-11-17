"""Genera Reporte/05_diccionario_datos.md desde information_schema.

Uso (PowerShell, desde la raíz del repo):
    $env:MYSQL_PWD = '<contraseña>'   # solo en la sesión, nunca en un archivo
    python -I Reporte/herramientas/generar_diccionario.py belissimo Sql/diccionario_datos.sql Reporte/05_diccionario_datos.md
"""
import re
import subprocess
import sys
import tempfile
import os

MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
db, src, dst = sys.argv[1:4]

sql = re.sub(r"\bbelissimo\b", db, open(src, encoding="utf-8").read())
tmp = os.path.join(tempfile.mkdtemp(), "dic.sql")
open(tmp, "w", encoding="utf-8").write(sql)
r = subprocess.run([MYSQL, "-u", "root", "--default-character-set=utf8mb4", "-B",
                    "-e", "source " + tmp.replace("\\", "/")],
                   capture_output=True, text=True, encoding="utf-8")
if r.returncode:
    sys.exit(r.stderr)

blocks, cur = [], None
for line in r.stdout.splitlines():
    if line.startswith("tabla\t"):
        cur = [line.split("\t")]
        blocks.append(cur)
    elif cur is not None:
        cur.append(line.split("\t"))

tablas, columnas, checks, fks, indices, triggers = blocks
desc = {row[0]: row[1] for row in tablas[1:]}

HEAD = {
    "atributo": "Atributo", "significado": "Significado", "tipo": "Tipo", "longitud": "Longitud",
    "nulo": "Nulo", "pk": "PK", "fk": "FK (referencia)", "valor_omision": "Por omisión",
    "restricciones": "Restricciones / extra",
}


def cell(v):
    v = v.replace("\\n", " ").replace("|", "\\|")
    return "" if v == "NULL" else v


def table(rows):
    h = rows[0]
    out = ["| " + " | ".join(HEAD.get(x, x) for x in h) + " |",
           "| " + " | ".join("---" for _ in h) + " |"]
    for r_ in rows[1:]:
        out.append("| " + " | ".join(cell(x) for x in r_) + " |")
    return "\n".join(out)


md = ["# 5. Diccionario de datos — base `belissimo`", "",
      "Generado automáticamente desde `information_schema` de **MySQL 8.0.46** con "
      "`Sql/diccionario_datos.sql` (no está escrito a mano). El significado de cada atributo "
      "es el `COMMENT` definido en `Sql/01_esquema.sql`.", "",
      f"Tablas: **{len(tablas) - 1}** · Columnas: **{len(columnas) - 1}** · "
      f"CHECK: **{len(checks) - 1}** · Llaves foráneas: **{len(fks) - 1}** · "
      f"Índices: **{len(indices) - 1}** · Triggers: **{len(triggers) - 1}**", ""]

md += ["## 5.1 Tablas", "", table(tablas), ""]
md += ["## 5.2 Atributos por tabla", ""]
by_table = {}
for row in columnas[1:]:
    by_table.setdefault(row[0], []).append(row[1:])
for t, rows in by_table.items():
    md += [f"### `{t}`", "", f"_{desc.get(t, '')}_", "",
           table([columnas[0][1:]] + rows), ""]
md += ["## 5.3 Restricciones CHECK", "", table(checks), ""]
md += ["## 5.4 Llaves foráneas (integridad referencial)", "", table(fks), ""]
md += ["## 5.5 Índices", "",
       "`PRIMARY` = llave primaria; los `uq_`/`UNIQUE` son claves candidatas; los `fk_` los crea "
       "MySQL para las llaves foráneas; los `idx_` se agregaron para búsquedas frecuentes.", "",
       table(indices), ""]
md += ["## 5.6 Triggers", "",
       "Lógica completa de cada trigger: `Sql/03_rutinas.sql` y sección 9.5.", "",
       table(triggers), ""]

text = "\n".join(md).replace(db, "belissimo")
open(dst, "w", encoding="utf-8", newline="\n").write(text)
print("ok", dst, len(columnas) - 1, "columnas")
