#!/usr/bin/env python3
"""Marks agent file edits, then runs `just format` when the agent turn ends.

afterFileEdit -> write a per-conversation marker
stop          -> if marker exists, run format and clear the marker
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any


def _marker_path(conversation_id: object) -> Path:
    safe_id = re.sub(r"[^\w.-]+", "_", str(conversation_id or "default"))
    return Path(tempfile.gettempdir()) / f"finebytes-cursor-edited-{safe_id}"


def _workspace_root(payload: dict[str, Any]) -> str:
    roots = payload.get("workspace_roots")
    if isinstance(roots, list) and roots and isinstance(roots[0], str):
        return roots[0]
    return os.getcwd()


def _write_json(obj: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(obj))


def _mark_edited(payload: dict[str, Any]) -> None:
    _marker_path(payload.get("conversation_id")).write_text("1", encoding="utf-8")
    _write_json({})


def _format_on_stop(payload: dict[str, Any]) -> None:
    marker = _marker_path(payload.get("conversation_id"))
    if not marker.exists():
        _write_json({})
        return

    try:
        marker.unlink()
    except OSError:
        # Marker cleanup is best-effort; still try to format.
        pass

    cwd = _workspace_root(payload)
    try:
        result = subprocess.run(
            ["just", "format"],
            cwd=cwd,
            capture_output=True,
            text=True,
            check=False,
        )
    except OSError as error:
        print(f"[format-on-stop] failed to start just format: {error}", file=sys.stderr)
        _write_json({})
        return

    if result.returncode != 0:
        detail = (result.stderr or result.stdout or "").strip()
        suffix = f":\n{detail}" if detail else ""
        print(
            f"[format-on-stop] just format exited with {result.returncode}{suffix}",
            file=sys.stderr,
        )

    _write_json({})


def main() -> None:
    raw = sys.stdin.read()
    try:
        payload: dict[str, Any] = json.loads(raw) if raw.strip() else {}
    except json.JSONDecodeError as error:
        print(f"[format-on-stop] invalid stdin JSON: {error}", file=sys.stderr)
        _write_json({})
        return

    event = payload.get("hook_event_name")
    if event == "afterFileEdit":
        _mark_edited(payload)
    elif event == "stop":
        _format_on_stop(payload)
    else:
        _write_json({})


if __name__ == "__main__":
    main()
