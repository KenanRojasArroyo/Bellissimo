"""Exporta el modelo (tablas, columnas, llaves) de MySQL a modelo.json.

Uso (PowerShell):
    $env:MYSQL_PWD = '<contraseña>'   # solo en la sesión; nunca en un archivo
    python -I exportar_modelo.py belissimo modelo.json
"""
import json
import subprocess
import sys

MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
db, dst = sys.argv[1], sys.argv[2]


def q(sql):
    r = subprocess.run([MYSQL, "-u", "root", "--default-character-set=utf8mb4", "-B", "-N",
                        "-e", sql], capture_output=True, text=True, encoding="utf-8")
    if r.returncode:
        sys.exit(r.stderr)
    return [line.split("\t") for line in r.stdout.splitlines() if line]


tablas = {}
for t, comment in q(f"SELECT TABLE_NAME, TABLE_COMMENT FROM information_schema.TABLES "
                    f"WHERE TABLE_SCHEMA='{db}' AND TABLE_TYPE='BASE TABLE'"):
    tablas[t] = {"comentario": comment, "columnas": []}
for t, c, tipo, nulo, key in q(
        f"SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY "
        f"FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='{db}' "
        f"ORDER BY TABLE_NAME, ORDINAL_POSITION"):
    if t in tablas:
        tablas[t]["columnas"].append({"nombre": c, "tipo": tipo.upper(),
                                      "nulo": nulo == "YES", "pk": key == "PRI"})
fks = [{"nombre": n, "tabla": t, "columna": c, "ref_tabla": rt, "ref_columna": rc}
       for n, t, c, rt, rc in q(
           f"SELECT CONSTRAINT_NAME, TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, "
           f"REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE "
           f"WHERE TABLE_SCHEMA='{db}' AND REFERENCED_TABLE_NAME IS NOT NULL "
           f"ORDER BY TABLE_NAME, CONSTRAINT_NAME")]
uniques = {}
for t, n, c in q(f"SELECT k.TABLE_NAME, k.CONSTRAINT_NAME, k.COLUMN_NAME "
                 f"FROM information_schema.TABLE_CONSTRAINTS tc "
                 f"JOIN information_schema.KEY_COLUMN_USAGE k ON k.CONSTRAINT_SCHEMA=tc.CONSTRAINT_SCHEMA "
                 f"AND k.CONSTRAINT_NAME=tc.CONSTRAINT_NAME AND k.TABLE_NAME=tc.TABLE_NAME "
                 f"WHERE tc.TABLE_SCHEMA='{db}' AND tc.CONSTRAINT_TYPE='UNIQUE' "
                 f"ORDER BY k.TABLE_NAME, k.CONSTRAINT_NAME, k.ORDINAL_POSITION"):
    uniques.setdefault(t, {}).setdefault(n, []).append(c)
for t, u in uniques.items():
    tablas[t]["unicos"] = u

json.dump({"base": "belissimo", "tablas": tablas, "fks": fks}, open(dst, "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
print("ok", len(tablas), "tablas", len(fks), "fks")
