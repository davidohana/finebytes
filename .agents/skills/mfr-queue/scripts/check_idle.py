#!/usr/bin/env python3
"""Heuristic idle check for finebytes agent checkout serialization."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

LOCK_REL = Path(".tmp") / "agent-lock.json"
DEFAULT_STALE_SEC = 15 * 60
DEFAULT_LOCK_STALE_SEC = 2 * 60 * 60
AGENT_SHELL_BUSY = re.compile(
    r"\b(dotnet\s+(test|build|format|csharpier)|just\s+(format|test|lint|build|restore))\b",
    re.IGNORECASE,
)
SKIP_ACTIVE = re.compile(r"\bjust\s+run-ui\b", re.IGNORECASE)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _iso_now() -> str:
    return _utc_now().strftime("%Y-%m-%dT%H:%M:%SZ")


def _repo_slug(repo: Path) -> str:
    # d:\Devl\finebytes → d-Devl-finebytes (Cursor projects folder naming)
    s = str(repo.resolve())
    s = s.replace(":", "")
    s = s.replace("\\", "-").replace("/", "-")
    while "--" in s:
        s = s.replace("--", "-")
    return s.lstrip("-")


def _discover_transcripts_dir(repo: Path) -> Path | None:
    env = os.environ.get("CURSOR_AGENT_TRANSCRIPTS_DIR")
    if env:
        p = Path(env)
        if p.is_dir():
            return p

    home = Path.home()
    projects = home / ".cursor" / "projects"
    if not projects.is_dir():
        return None

    for slug in _slug_candidates(repo):
        direct = projects / slug / "agent-transcripts"
        if direct.is_dir():
            return direct

    # Fallback: unique match whose slug contains the repo folder name.
    name = repo.resolve().name
    matches = [
        p / "agent-transcripts"
        for p in projects.iterdir()
        if p.is_dir() and name.lower() in p.name.lower() and (p / "agent-transcripts").is_dir()
    ]
    if len(matches) == 1:
        return matches[0]
    return None


def _slug_candidates(repo: Path) -> list[str]:
    slug = _repo_slug(repo)
    out = [slug]
    if slug and slug[0].isalpha():
        flipped = (slug[0].swapcase() + slug[1:])
        if flipped not in out:
            out.append(flipped)
    lower = slug.lower()
    if lower not in out:
        out.append(lower)
    return out


def _discover_terminals_dir(repo: Path) -> Path | None:
    env = os.environ.get("CURSOR_TERMINALS_DIR")
    if env:
        p = Path(env)
        if p.is_dir():
            return p

    home = Path.home()
    projects = home / ".cursor" / "projects"
    if not projects.is_dir():
        return None

    for slug in _slug_candidates(repo):
        direct = projects / slug / "terminals"
        if direct.is_dir():
            return direct

    name = repo.resolve().name
    matches = [
        p / "terminals"
        for p in projects.iterdir()
        if p.is_dir() and name.lower() in p.name.lower() and (p / "terminals").is_dir()
    ]
    if len(matches) == 1:
        return matches[0]
    return None


def _last_nonempty_line(path: Path) -> str:
    try:
        raw = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""
    for line in reversed(raw.splitlines()):
        if line.strip():
            return line.strip()
    return ""


def _transcript_busy(
    transcripts_dir: Path,
    self_id: str | None,
    stale_sec: int,
    now: float,
) -> list[dict]:
    busy: list[dict] = []
    if not transcripts_dir.is_dir():
        return busy

    for session_dir in transcripts_dir.iterdir():
        if not session_dir.is_dir():
            continue
        sid = session_dir.name
        if self_id and sid == self_id:
            continue
        jsonl = session_dir / f"{sid}.jsonl"
        if not jsonl.is_file():
            continue

        age = now - jsonl.stat().st_mtime
        if age > stale_sec:
            continue

        last = _last_nonempty_line(jsonl)
        if '"type":"turn_ended"' in last or '"type": "turn_ended"' in last:
            continue

        busy.append(
            {
                "kind": "transcript",
                "session": sid,
                "age_sec": int(age),
                "last_line_preview": last[:160],
            }
        )
    return busy


def _parse_terminal_header(text: str) -> dict[str, str]:
    meta: dict[str, str] = {}
    if not text.startswith("---"):
        return meta
    end = text.find("\n---", 3)
    if end < 0:
        return meta
    block = text[3:end]
    for line in block.splitlines():
        if ":" not in line:
            continue
        key, val = line.split(":", 1)
        meta[key.strip()] = val.strip()
    return meta


def _terminals_busy(terminals_dir: Path, now: float, stale_sec: int) -> list[dict]:
    busy: list[dict] = []
    if not terminals_dir.is_dir():
        return busy

    done_statuses = {"succeeded", "failed", "cancelled"}

    for path in terminals_dir.glob("*.txt"):
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
            age = now - path.stat().st_mtime
        except OSError:
            continue

        meta = _parse_terminal_header(text)
        status = meta.get("status", "").lower()
        active = meta.get("active_command", "")
        command = meta.get("command", "")
        title = meta.get("title", "")
        probe = " ".join(part for part in (active, command, title) if part)

        if SKIP_ACTIVE.search(probe):
            continue

        # User terminal with an in-flight build/test/format.
        if active and AGENT_SHELL_BUSY.search(active):
            busy.append(
                {
                    "kind": "terminal",
                    "file": path.name,
                    "status": "active_command",
                    "command_preview": active[:160],
                }
            )
            continue

        # Agent shell still running (has command, not finished).
        if command and status not in done_statuses and AGENT_SHELL_BUSY.search(probe):
            if age > stale_sec:
                continue
            busy.append(
                {
                    "kind": "terminal",
                    "file": path.name,
                    "status": status or "running",
                    "command_preview": (command or title)[:160],
                }
            )
    return busy


def _lock_busy(repo: Path, self_id: str | None, lock_stale_sec: int, now: float) -> dict | None:
    path = repo / LOCK_REL
    if not path.is_file():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {
            "kind": "lock",
            "path": str(LOCK_REL).replace("\\", "/"),
            "detail": "unreadable lock; treat as busy",
        }

    holder = data.get("holder")
    if self_id and holder == self_id:
        return None

    started = data.get("started_at")
    age = None
    if isinstance(started, str):
        try:
            started_dt = datetime.fromisoformat(started.replace("Z", "+00:00"))
            age = (_utc_now() - started_dt).total_seconds()
        except ValueError:
            age = None

    if age is not None and age > lock_stale_sec:
        return None

    return {
        "kind": "lock",
        "path": str(LOCK_REL).replace("\\", "/"),
        "holder": holder,
        "task": data.get("task"),
        "age_sec": int(age) if age is not None else None,
    }


def _check(repo: Path, self_id: str | None, stale_sec: int, lock_stale_sec: int) -> dict:
    now = time.time()
    transcripts_dir = _discover_transcripts_dir(repo)
    terminals_dir = _discover_terminals_dir(repo)

    reasons: list[dict] = []
    if transcripts_dir:
        reasons.extend(_transcript_busy(transcripts_dir, self_id, stale_sec, now))
    if terminals_dir:
        reasons.extend(_terminals_busy(terminals_dir, now, stale_sec))

    lock = _lock_busy(repo, self_id, lock_stale_sec, now)
    if lock:
        reasons.append(lock)

    return {
        "idle": len(reasons) == 0,
        "reasons": reasons,
        "self_id": self_id,
        "repo": str(repo),
        "transcripts_dir": str(transcripts_dir) if transcripts_dir else None,
        "terminals_dir": str(terminals_dir) if terminals_dir else None,
        "checked_at": _iso_now(),
    }


def _acquire(repo: Path, self_id: str, task: str, stale_sec: int, lock_stale_sec: int) -> dict:
    status = _check(repo, self_id, stale_sec, lock_stale_sec)
    if not status["idle"]:
        return {"ok": False, "error": "not_idle", **status}

    lock_path = repo / LOCK_REL
    lock_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "holder": self_id,
        "started_at": _iso_now(),
        "task": task,
    }
    lock_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    return {"ok": True, "lock": payload, "path": str(LOCK_REL).replace("\\", "/")}


def _release(repo: Path, self_id: str) -> dict:
    lock_path = repo / LOCK_REL
    if not lock_path.is_file():
        return {"ok": True, "released": False, "detail": "no lock"}

    try:
        data = json.loads(lock_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        lock_path.unlink(missing_ok=True)
        return {"ok": True, "released": True, "detail": "removed unreadable lock"}

    holder = data.get("holder")
    if holder and holder != self_id:
        return {"ok": False, "error": "not_holder", "holder": holder}

    lock_path.unlink(missing_ok=True)
    return {"ok": True, "released": True, "holder": self_id}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "action",
        choices=("check", "acquire", "release"),
        help="check idle, acquire lock, or release lock",
    )
    parser.add_argument(
        "--repo",
        type=Path,
        default=Path.cwd(),
        help="repo root (default: cwd)",
    )
    parser.add_argument(
        "--self-id",
        default=os.environ.get("CURSOR_AGENT_SESSION_ID"),
        help="this agent conversation UUID (exclude from busy check)",
    )
    parser.add_argument("--task", default="", help="short task label when acquiring")
    parser.add_argument(
        "--stale-sec",
        type=int,
        default=DEFAULT_STALE_SEC,
        help="ignore mid-turn transcripts / shells older than this",
    )
    parser.add_argument(
        "--lock-stale-sec",
        type=int,
        default=DEFAULT_LOCK_STALE_SEC,
        help="ignore locks older than this",
    )
    args = parser.parse_args()
    repo = args.repo.resolve()

    if args.action == "check":
        result = _check(repo, args.self_id, args.stale_sec, args.lock_stale_sec)
    elif args.action == "acquire":
        if not args.self_id:
            print(json.dumps({"ok": False, "error": "self_id_required"}))
            return 2
        result = _acquire(repo, args.self_id, args.task, args.stale_sec, args.lock_stale_sec)
    else:
        if not args.self_id:
            print(json.dumps({"ok": False, "error": "self_id_required"}))
            return 2
        result = _release(repo, args.self_id)

    print(json.dumps(result, indent=2))
    if args.action == "check":
        return 0 if result.get("idle") else 1
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main())
