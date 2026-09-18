#!/usr/bin/env python3
"""Annotate help/images/ui/main-window.png → help/images/ui/hotspots.png (MFR7-style).

Also refreshes imagemap coords in help/ui/parts.html.

Requires Pillow (`pip install -r help/tools/requirements.txt` into the repo `.venv`).
"""

from __future__ import annotations

import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "help" / "images" / "ui" / "main-window.png"
DST = ROOT / "help" / "images" / "ui" / "hotspots.png"
PARTS_HTML = ROOT / "help" / "ui" / "parts.html"


def _load_font(size: int) -> ImageFont.ImageFont:
    for path in (
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/freefont/FreeSansBold.ttf",
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arialbd.ttf",
    ):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


def _layout(w: int, h: int) -> dict:
    """Pane rects from MainWindow.axaml star ratios + measured chrome."""
    menu_toolbar = 68
    status_h = 29
    panes_top = menu_toolbar
    panes_bottom = h - status_h
    panes_h = panes_bottom - panes_top
    split = 6
    star_h = panes_h - split
    top_h = int(round(0.61 * star_h))
    rename_top = panes_top + top_h + split

    star_w = w - split
    file_w = int(round(0.4 * star_w))
    filters_left = file_w + split
    filters_w = star_w - file_w

    filter_star_h = top_h - split
    lists_h = int(round(0.3 * filter_star_h))
    config_top = panes_top + lists_h + split

    lists_star_w = filters_w - split
    avail_w = int(round(0.42 * lists_star_w))
    chain_left = filters_left + avail_w + split

    return {
        "file": (2, panes_top + 2, file_w - 2, panes_top + top_h - 2),
        "avail": (filters_left + 2, panes_top + 2, filters_left + avail_w - 2, panes_top + lists_h - 2),
        "chain": (chain_left + 2, panes_top + 2, w - 4, panes_top + lists_h - 2),
        "config": (filters_left + 2, config_top + 2, w - 4, panes_top + top_h - 2),
        "rename": (2, rename_top + 2, w - 4, panes_bottom - 2),
        "status": (2, panes_bottom + 1, w - 4, h - 3),
        "log": (8 + 2 * 36 + 16, (30 + 62) / 2, 20),
        "options": (8 + 3 * 36 + 16, (30 + 62) / 2, 20),
    }


def _draw_label(
    draw: ImageDraw.ImageDraw,
    font: ImageFont.ImageFont,
    text: str,
    color: tuple[int, int, int],
    cx: float,
    cy: float,
) -> None:
    pad_x, pad_y = 10, 5
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x0 = int(cx - tw / 2 - pad_x)
    y0 = int(cy - th / 2 - pad_y)
    x1 = int(cx + tw / 2 + pad_x)
    y1 = int(cy + th / 2 + pad_y)
    draw.rounded_rectangle(
        [x0, y0, x1, y1],
        radius=10,
        fill=color + (230,),
        outline=(255, 255, 255, 240),
        width=2,
    )
    draw.text((x0 + pad_x - bbox[0], y0 + pad_y - bbox[1]), text, fill=(255, 255, 255, 255), font=font)


def _replace_area_coords(html: str, href: str, coords: str) -> str:
    pattern = re.compile(
        rf'(<area\b(?=[^>]*\bhref="{re.escape(href)}")[^>]*?\bcoords=")[^"]+(")',
        re.DOTALL,
    )
    updated, count = pattern.subn(rf"\g<1>{coords}\2", html, count=1)
    if count != 1:
        raise SystemExit(f"failed to patch parts.html area for href={href!r} (matches={count})")
    return updated


def _patch_parts_html(layout: dict) -> None:
    html = PARTS_HTML.read_text(encoding="utf-8")
    mapping = {
        "fileexp.html": f'{layout["file"][0]},{layout["file"][1]},{layout["file"][2]},{layout["file"][3]}',
        "availfilterlist.html": (
            f'{layout["avail"][0]},{layout["avail"][1]},{layout["avail"][2]},{layout["avail"][3]}'
        ),
        "appliedfilterlist.html": (
            f'{layout["chain"][0]},{layout["chain"][1]},{layout["chain"][2]},{layout["chain"][3]}'
        ),
        "filterconfigpanel.html": (
            f'{layout["config"][0]},{layout["config"][1]},{layout["config"][2]},{layout["config"][3]}'
        ),
        "renamelist.html": (
            f'{layout["rename"][0]},{layout["rename"][1]},{layout["rename"][2]},{layout["rename"][3]}'
        ),
        "statusbar.html": (
            f'{layout["status"][0]},{layout["status"][1]},{layout["status"][2]},{layout["status"][3]}'
        ),
        "log.html": f'{int(layout["log"][0])},{int(layout["log"][1])},{int(layout["log"][2])}',
        "optionswin.html": (
            f'{int(layout["options"][0])},{int(layout["options"][1])},{int(layout["options"][2])}'
        ),
    }
    updated = html
    for href, coords in mapping.items():
        updated = _replace_area_coords(updated, href, coords)

    if updated != html:
        PARTS_HTML.write_text(updated, encoding="utf-8", newline="\n")
        print("updated", PARTS_HTML.relative_to(ROOT))
    else:
        print("parts.html imagemap already up to date")


def main() -> None:
    im = Image.open(SRC).convert("RGBA")
    w, h = im.size
    if (w, h) != (1280, 860):
        raise SystemExit(f"expected 1280x860 main-window.png, got {w}x{h}")

    layout = _layout(w, h)
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    font = _load_font(15)

    regions = [
        ("File List", layout["file"], (220, 60, 40), "tl"),
        ("Available Filters", layout["avail"], (230, 140, 30), "bl"),
        ("Filter Chain", layout["chain"], (40, 170, 200), "br"),
        ("Filter Configuration", layout["config"], (200, 60, 140), "bl"),
        ("Rename List", layout["rename"], (40, 160, 60), "br"),
        ("Status Bar", layout["status"], (220, 180, 30), "bc"),
    ]

    for name, (x0, y0, x1, y1), color, anchor in regions:
        for t in range(4):
            draw.rectangle([x0 + t, y0 + t, x1 - t, y1 - t], outline=color + (255,))
        if anchor == "tl":
            lx, ly = x0 + 90, y0 + 22
        elif anchor == "bl":
            lx, ly = x0 + 110, y1 - 22
        elif anchor == "br":
            lx, ly = x1 - 110, y1 - 22
        else:
            lx, ly = (x0 + x1) / 2, (y0 + y1) / 2
        _draw_label(draw, font, name, color, lx, ly)

    for key, color in (("log", (80, 200, 100)), ("options", (220, 80, 160))):
        cx, cy, r = layout[key]
        for t in range(3):
            draw.ellipse([cx - r + t, cy - r + t, cx + r - t, cy + r - t], outline=color + (255,))

    out = Image.alpha_composite(im, overlay).convert("RGB")
    out.save(DST, "PNG", optimize=True)
    print("wrote", DST.relative_to(ROOT))
    _patch_parts_html(layout)


if __name__ == "__main__":
    main()
