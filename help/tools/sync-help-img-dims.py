#!/usr/bin/env python3
"""Sync help HTML <img width/height> attrs from on-disk PNG sizes.

Walks help/**/*.html, resolves relative img src ending in .png under help/, and
rewrites width/height to match the PNG IHDR. Stdlib only (no Pillow).
"""

from __future__ import annotations

import re
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELP = ROOT / "help"
IMG_TAG = re.compile(
    r"(<img\b[^>]*?\bsrc=\")([^\"]+\.png)(\"[^>]*?>)",
    re.IGNORECASE | re.DOTALL,
)


def _png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as handle:
        signature = handle.read(8)
        if signature != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"not a PNG: {path}")
        length = struct.unpack(">I", handle.read(4))[0]
        chunk = handle.read(4)
        if chunk != b"IHDR" or length < 8:
            raise ValueError(f"missing IHDR: {path}")
        width, height = struct.unpack(">II", handle.read(8))
        return width, height


def _set_dim(tag: str, name: str, value: int) -> str:
    pattern = re.compile(rf'\b{name}="\d+"', re.IGNORECASE)
    replacement = f'{name}="{value}"'
    if pattern.search(tag):
        return pattern.sub(replacement, tag, count=1)
    # Insert before the closing /> or >
    if tag.rstrip().endswith("/>"):
        return tag[:-2] + f" {replacement} />"
    if tag.endswith(">"):
        return tag[:-1] + f" {replacement}>"
    return tag


def _rewrite_file(html_path: Path) -> bool:
    original = html_path.read_text(encoding="utf-8")
    changed = False

    def repl(match: re.Match[str]) -> str:
        nonlocal changed
        prefix, src, suffix = match.group(1), match.group(2), match.group(3)
        png_path = (html_path.parent / src).resolve()
        try:
            png_path.relative_to(HELP.resolve())
        except ValueError:
            return match.group(0)
        if not png_path.is_file():
            return match.group(0)

        width, height = _png_size(png_path)
        tag = prefix + src + suffix
        updated = _set_dim(tag, "width", width)
        updated = _set_dim(updated, "height", height)
        if updated != tag:
            changed = True
        return updated

    rewritten = IMG_TAG.sub(repl, original)
    if not changed:
        return False
    html_path.write_text(rewritten, encoding="utf-8", newline="\n")
    return True


def main() -> None:
    updated = 0
    for html_path in sorted(HELP.rglob("*.html")):
        if _rewrite_file(html_path):
            print("updated", html_path.relative_to(ROOT))
            updated += 1
    print(f"done ({updated} file(s) changed)")


if __name__ == "__main__":
    main()
