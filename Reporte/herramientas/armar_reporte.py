"""Une las secciones de BD del reporte en Reporte_BD.md, Reporte_BD.html y Reporte_BD.pdf.

Uso (desde la raíz del repo):
    python -I Reporte/herramientas/armar_reporte.py
Requiere:  pip install markdown      (solo para el HTML/PDF)
El PDF se imprime con Microsoft Edge en modo headless (si está instalado).
"""
import os
import re
import subprocess
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
REP = os.path.dirname(AQUI)
RAIZ = os.path.dirname(REP)

SECCIONES = [
    ("00_portada.md", None),
    ("01_problema.md", None),
    ("02_reglas_y_requerimientos.md", None),
    ("02b_arquitectura.md", None),
    ("03_entidades_y_relaciones.md", None),
    ("04_diagramas_modelo_relacional.md", None),
    ("05_diccionario_datos.md", None),
    ("06_normalizacion.md", None),
    ("07_algebra_relacional.md", None),
    ("08_consultas.md", None),
    ("09_implementacion.md", None),
    ("../Pruebas/matriz_pruebas.md", "11. Pruebas de base de datos"),
    ("../Pruebas/incidencias.md", "12. Incidencias"),
    ("12_aplicacion_web.md", None),
    ("13_conclusiones.md", None),
    ("15_referencias.md", None),
]


def renumerar(texto, titulo):
    """Cambia el H1 por el título numerado y numera los H2 como N.1, N.2…"""
    n = titulo.split(".")[0]
    texto = re.sub(r"^# .*$", "# " + titulo, texto, count=1, flags=re.M)
    k = 0

    def h2(m):
        nonlocal k
        k += 1
        return f"## {n}.{k} {m.group(1)}"
    return re.sub(r"^## (?:\d+\.\s*)?(.*)$", h2, texto, flags=re.M)


partes = []
for archivo, titulo in SECCIONES:
    with open(os.path.join(REP, archivo), encoding="utf-8") as fh:
        t = fh.read().strip()
    if titulo:
        t = renumerar(t, titulo)
    partes.append(t)
md = "\n\n<div class=\"salto\"></div>\n\n".join(partes) + "\n"
salida_md = os.path.join(REP, "Reporte_BD.md")
open(salida_md, "w", encoding="utf-8").write(md)
print("ok", salida_md)

try:
    import markdown
except ImportError:
    sys.exit("Para el HTML/PDF instala:  pip install markdown")

cuerpo = markdown.markdown(md, extensions=["tables", "fenced_code", "toc"])
css = """
body{font-family:'Segoe UI',Arial,sans-serif;font-size:10.5pt;line-height:1.45;color:#1f2933;margin:0 auto;max-width:1100px;padding:24px}
h1{font-size:18pt;border-bottom:2px solid #1f2933;padding-bottom:4px;margin-top:28px}
h2{font-size:13.5pt;margin-top:22px} h3{font-size:11.5pt}
table{border-collapse:collapse;width:100%;margin:10px 0;font-size:8.6pt;page-break-inside:auto}
th,td{border:1px solid #9aa5b1;padding:3px 5px;vertical-align:top;text-align:left}
th{background:#e4ebf2} tr{page-break-inside:avoid}
code{font-family:Consolas,monospace;font-size:8.8pt;background:#f1f4f7;padding:0 2px}
pre{background:#f1f4f7;padding:8px;overflow-x:auto;font-size:8.6pt;white-space:pre-wrap}
pre code{background:none}
img{max-width:100%;border:1px solid #d0d7de;margin:6px 0}
blockquote{border-left:4px solid #c9a227;background:#fffbea;margin:10px 0;padding:6px 12px}
.salto{page-break-after:always}
@page{size:Letter landscape;margin:12mm}
"""
html = ("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\">"
        "<title>Reporte BD - Florería Bellissimo</title><style>" + css + "</style></head><body>"
        + cuerpo + "</body></html>")
salida_html = os.path.join(REP, "Reporte_BD.html")
open(salida_html, "w", encoding="utf-8").write(html)
print("ok", salida_html)

edge = [p for p in (r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe") if os.path.exists(p)]
if edge:
    salida_pdf = os.path.join(REP, "Reporte_BD.pdf")
    subprocess.run([edge[0], "--headless=new", "--disable-gpu", "--no-pdf-header-footer",
                    f"--print-to-pdf={salida_pdf}", "file:///" + salida_html.replace("\\", "/")],
                   capture_output=True, timeout=180)
    print("ok" if os.path.exists(salida_pdf) else "no se generó", salida_pdf)
else:
    print("Edge no encontrado: abre Reporte_BD.html e imprime a PDF.")
