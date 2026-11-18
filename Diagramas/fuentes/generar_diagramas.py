"""Dibuja el modelo relacional de `belissimo` en varias notaciones a partir de modelo.json.

Todas las notaciones usan la MISMA distribución de tablas, así que son coherentes entre sí.

Uso:  python -I generar_diagramas.py modelo.json <carpeta_salida>
Genera PNG (200 dpi) y PDF de: crows_foot, connect_to_columns, uml, idef1x.
Requiere matplotlib.
"""
import json
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
from matplotlib.patches import FancyBboxPatch, Polygon, Rectangle, Circle  # noqa: E402

ROW = 0.30          # alto de un renglón de columna
HEAD = 0.42         # alto del encabezado de la tabla
WIDTH = 3.75        # ancho de la tabla
STUB = 0.55         # tramo horizontal junto a la tabla (donde van los símbolos)
INK = "#1f2933"
HEAD_BG = {"crows_foot": "#dbe7f3", "connect_to_columns": "#dbe7f3",
           "uml": "#fff4d6", "idef1x": "#ffffff"}

# Posición (x, y_superior) de cada tabla. Columnas de izquierda a derecha.
POS = {
    "puesto": (0.0, 0.0),
    "ausencia": (0.0, -3.2),
    "empleado": (5.2, 0.0),
    "semana": (5.2, -6.6),
    "registro_asistencia": (10.4, 0.0),
    "pago_semanal": (10.4, -4.6),
    "usuario": (15.6, 0.0),
    "detalle_pago": (15.6, -4.6),
    "horario_general": (21.4, 0.0),
    "configuracion": (21.4, -2.0),
    "auditoria_registro": (21.4, -4.0),
}

TITULOS = {
    "crows_foot": "Notación Crow's Foot (pata de gallo)",
    "connect_to_columns": "Notación Connect to Columns (líneas a las columnas de la llave)",
    "uml": "Notación UML (diagrama de clases con multiplicidades)",
    "idef1x": "Notación IDEF1X",
}

LEYENDAS = {
    "crows_foot": "‖ exactamente uno    o| cero o uno    o< cero o muchos    PK llave primaria · FK llave foránea · UQ única",
    "connect_to_columns": "Cada línea une la columna FK del hijo con la PK del padre.   ‖ uno   o| cero o uno   o< cero o muchos",
    "uml": "Multiplicidad: 1 = exactamente uno · 0..1 = cero o uno · 0..* = cero o muchos · {PK} {FK} {UQ}",
    "idef1x": "● hijo: cero, uno o muchos · ◇ padre opcional (FK nula) · línea punteada = relación no identificadora · (FK) (AK) llave alterna",
}


def box_height(t):
    return HEAD + ROW * len(t["columnas"])


def col_y(model, tabla, columna):
    x, y = POS[tabla]
    cols = [c["nombre"] for c in model["tablas"][tabla]["columnas"]]
    return y - HEAD - ROW * (cols.index(columna) + 0.5)


def uq_cols(t):
    return {c for cols in t.get("unicos", {}).values() for c in cols}


def ak_label(t, col):
    keys = list(t.get("unicos", {}).items())
    tags = [f"AK{i + 1}" for i, (_, cols) in enumerate(keys) if col in cols]
    return ",".join(tags)


def draw_table(ax, model, name, notation, fk_cols):
    t = model["tablas"][name]
    x, y = POS[name]
    h = box_height(t)
    uq = uq_cols(t)
    if notation == "idef1x":
        # Entidad independiente (esquinas rectas): nombre FUERA de la caja, arriba;
        # área de llave primaria arriba de la línea divisoria, resto de atributos abajo.
        top = y - 0.12
        ax.text(x, top + 0.06, name, fontsize=8.5, fontweight="bold", color=INK, va="bottom")
        ax.add_patch(Rectangle((x, y - h), WIDTH, top - (y - h), fc="white", lw=1.1, ec=INK))
        pk = [c for c in t["columnas"] if c["pk"]]
        rest = [c for c in t["columnas"] if not c["pk"]]
        cy = top - 0.04
        for c in pk:
            tag = " (FK)" if (name, c["nombre"]) in fk_cols else ""
            ax.text(x + 0.12, cy - ROW * 0.5, c["nombre"] + tag, fontsize=6.8, color=INK,
                    va="center", fontweight="bold")
            cy -= ROW
        div = cy - 0.06
        ax.plot([x, x + WIDTH], [div, div], color=INK, lw=0.9)
        cy = div - 0.06
        for c in rest:
            tags = []
            if (name, c["nombre"]) in fk_cols:
                tags.append("FK")
            ak = ak_label(t, c["nombre"])
            if ak:
                tags.append(ak)
            tag = f" ({','.join(tags)})" if tags else ""
            ax.text(x + 0.12, cy - ROW * 0.5, c["nombre"] + tag, fontsize=6.8, color=INK,
                    va="center")
            cy -= ROW
        return
    ax.add_patch(FancyBboxPatch((x, y - h), WIDTH, h, boxstyle="square,pad=0", lw=1.1,
                                ec=INK, fc="white"))
    ax.add_patch(Rectangle((x, y - HEAD), WIDTH, HEAD, lw=1.1, ec=INK, fc=HEAD_BG[notation]))
    label = name if notation != "uml" else f"«table» {name}"
    ax.text(x + WIDTH / 2, y - HEAD / 2, label, ha="center", va="center", fontsize=8.5,
            fontweight="bold", color=INK)
    for i, c in enumerate(t["columnas"]):
        cy = y - HEAD - ROW * (i + 0.5)
        is_fk = (name, c["nombre"]) in fk_cols
        tags = (["PK"] if c["pk"] else []) + (["FK"] if is_fk else []) + \
               (["UQ"] if c["nombre"] in uq and not c["pk"] else [])
        tipo = c["tipo"].replace("ENUM", "ENUM").split("(")[0] if c["tipo"].startswith("ENUM") \
            else c["tipo"]
        if notation == "uml":
            extra = (" {" + ",".join(tags) + "}") if tags else ""
            txt = f"{c['nombre']} : {tipo}{extra}"
            ax.text(x + 0.12, cy, txt, fontsize=6.6, va="center", color=INK)
        else:
            ax.text(x + 0.10, cy, ",".join(tags), fontsize=5.8, va="center", color="#7b2d26",
                    fontweight="bold")
            weight = "bold" if c["pk"] else "normal"
            ax.text(x + 0.62, cy, c["nombre"] + ("" if not c["nulo"] else " ?"), fontsize=6.8,
                    va="center", color=INK, fontweight=weight)
            ax.text(x + WIDTH - 0.08, cy, tipo, fontsize=6.0, va="center", ha="right",
                    color="#52606d")


def side_points(model, fk, notation, slots):
    """Devuelve (punto_hijo, dir_hijo, punto_padre, dir_padre)."""
    ch, pa = fk["tabla"], fk["ref_tabla"]
    cx, cy0 = POS[ch]
    px, py0 = POS[pa]
    if px > cx:
        c_side, p_side = "R", "L"
    else:
        c_side, p_side = "L", "R"
    c_x = cx + WIDTH if c_side == "R" else cx
    p_x = px + WIDTH if p_side == "R" else px
    if notation == "connect_to_columns":
        c_y = col_y(model, ch, fk["columna"])
        p_y = col_y(model, pa, fk["ref_columna"])
    else:
        c_y = slots[(ch, c_side)][fk["nombre"]]
        p_y = slots[(pa, p_side)][fk["nombre"]]
    c_dir = 1 if c_side == "R" else -1
    p_dir = 1 if p_side == "R" else -1
    return (c_x, c_y), c_dir, (p_x, p_y), p_dir


def compute_slots(model):
    groups = {}
    for fk in model["fks"]:
        ch, pa = fk["tabla"], fk["ref_tabla"]
        right = POS[pa][0] > POS[ch][0]
        groups.setdefault((ch, "R" if right else "L"), []).append((fk, pa))
        groups.setdefault((pa, "L" if right else "R"), []).append((fk, ch))
    slots = {}
    for (tabla, side), items in groups.items():
        x, y = POS[tabla]
        h = box_height(model["tablas"][tabla])
        items.sort(key=lambda it: -POS[it[1]][1])  # el de más arriba primero
        n = len(items)
        slots[(tabla, side)] = {}
        for i, (fk, _) in enumerate(items):
            top, bottom = y - HEAD - 0.15, y - h + 0.15
            yy = (top + bottom) / 2 if n == 1 else top - (top - bottom) * (i + 0.5) / n
            yy = min(yy, y - HEAD - 0.2)
            slots[(tabla, side)][fk["nombre"]] = yy
    return slots


def bar(ax, x, y, d, off):
    ax.plot([x + d * off, x + d * off], [y - 0.13, y + 0.13], color=INK, lw=1.1)


def circle(ax, x, y, d, off, filled=False):
    ax.add_patch(Circle((x + d * off, y), 0.075, fc=INK if filled else "white", ec=INK, lw=1.0,
                        zorder=5))


def crow(ax, x, y, d):
    tip = x + d * 0.30
    for dy in (-0.14, 0, 0.14):
        ax.plot([tip, x], [y, y + dy], color=INK, lw=1.1)


def diamond(ax, x, y, d, off):
    cx = x + d * off
    ax.add_patch(Polygon([(cx - 0.11, y), (cx, y + 0.09), (cx + 0.11, y), (cx, y - 0.09)],
                         closed=True, fc="white", ec=INK, lw=1.0, zorder=5))


def lanes(model, notation, slots):
    """Asigna un carril vertical distinto a las relaciones que comparten canal."""
    groups = {}
    for fk in model["fks"]:
        (c_x, _), cd, (p_x, _), pd = side_points(model, fk, notation, slots)
        mid = round(((c_x + cd * STUB) + (p_x + pd * STUB)) / 2, 2)
        groups.setdefault(mid, []).append(fk["nombre"])
    out = {}
    for mid, names in groups.items():
        n = len(names)
        for i, name in enumerate(sorted(names)):
            out[name] = mid + (i - (n - 1) / 2) * 0.22
    return out


def draw_edge(ax, model, fk, notation, slots, lane):
    (c_x, c_y), cd, (p_x, p_y), pd = side_points(model, fk, notation, slots)
    c_s = (c_x + cd * STUB, c_y)
    p_s = (p_x + pd * STUB, p_y)
    mid = lane[fk["nombre"]]
    xs = [c_x, mid, mid, p_x]
    ys = [c_y, c_y, p_y, p_y]
    style = "--" if notation == "idef1x" else "-"
    ax.plot(xs, ys, color=INK, lw=1.0, ls=style, zorder=1)
    nullable = next(c["nulo"] for c in model["tablas"][fk["tabla"]]["columnas"]
                    if c["nombre"] == fk["columna"])
    if notation in ("crows_foot", "connect_to_columns"):
        crow(ax, c_x, c_y, cd)
        circle(ax, c_x, c_y, cd, 0.42)
        bar(ax, p_x, p_y, pd, 0.18)
        if nullable:
            circle(ax, p_x, p_y, pd, 0.36)
        else:
            bar(ax, p_x, p_y, pd, 0.30)
    elif notation == "uml":
        ax.text(c_x + cd * 0.08, c_y + 0.10, "0..*", fontsize=6.5, color="#7b2d26",
                ha="left" if cd > 0 else "right", va="bottom")
        ax.text(p_x + pd * 0.08, p_y + 0.10, "0..1" if nullable else "1", fontsize=6.5,
                color="#7b2d26", ha="left" if pd > 0 else "right", va="bottom")
    elif notation == "idef1x":
        circle(ax, c_x, c_y, cd, 0.09, filled=True)
        if nullable:
            diamond(ax, p_x, p_y, pd, 0.14)
    # nombre de la FK sobre el tramo vertical
    ax.text(mid + 0.06, (c_y + p_y) / 2, fk["nombre"], fontsize=5.4, color="#52606d",
            rotation=90, va="center", ha="left")


def render(model, notation, outdir):
    fk_cols = {(fk["tabla"], fk["columna"]) for fk in model["fks"]}
    slots = compute_slots(model)
    fig, ax = plt.subplots(figsize=(27, 13.5))
    for name in model["tablas"]:
        draw_table(ax, model, name, notation, fk_cols)
    lane = lanes(model, notation, slots)
    for fk in model["fks"]:
        draw_edge(ax, model, fk, notation, slots, lane)
    ax.text(0, 1.35, f"Base de datos belissimo — Florería Bellissimo · {TITULOS[notation]}",
            fontsize=14, fontweight="bold", color=INK)
    ax.text(0, 0.85, LEYENDAS[notation] + "   ·   '?' = admite NULL"
            if notation in ("crows_foot", "connect_to_columns") else LEYENDAS[notation],
            fontsize=8.5, color="#52606d")
    ax.text(21.4, -10.6, "Tablas sin relaciones: horario_general y configuracion son\n"
            "catálogos de parámetros; auditoria_registro no tiene FK a\n"
            "propósito (debe sobrevivir si se borra el registro).",
            fontsize=7, color="#52606d", va="top")
    ax.set_xlim(-0.5, 25.8)
    ax.set_ylim(-12.0, 1.8)
    ax.set_aspect("equal")
    ax.axis("off")
    base = os.path.join(outdir, f"{notation}")
    fig.savefig(base + ".png", dpi=200, bbox_inches="tight", facecolor="white")
    fig.savefig(base + ".pdf", bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print("ok", base)


if __name__ == "__main__":
    model = json.load(open(sys.argv[1], encoding="utf-8"))
    out = sys.argv[2]
    os.makedirs(out, exist_ok=True)
    for n in ("crows_foot", "connect_to_columns", "uml", "idef1x"):
        render(model, n, out)
