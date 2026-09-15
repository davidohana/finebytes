---
name: mfr-queue
description: >-
  Waits until other local Cursor agents look idle, then runs the user's task on
  the shared finebytes checkout. Use when the user says /queue, mfr-queue, “wait
  until other agents finish”, “when current sessions are done”, or asks to defer
  impl work so parallel agents do not overwrite the same files. Not for
  worktree/cloud isolation (prefer /worktree) and not a hard product queue.
---

# MFR queue (wait until idle)

Serialize **implementation** on the main finebytes checkout by waiting until other
local agents look idle, then doing the work. Order among waiters does not matter.

This is a **heuristic**, not a Cursor scheduler. Prefer `/worktree` when parallel
edits are intentional.

## When this skill applies

- **Yes** — `/queue …`, `@mfr-queue`, “wait until sessions done”, “start when other
  agents finish”, “don’t collide with the other agent”
- **No** — normal implement/review in an already-idle chat; cloud-only runs with no
  local transcripts; user asked for worktrees / best-of-n

## Self id

Take the conversation UUID from user_info / agent store / transcript path
(`…/agent-transcripts/<uuid>/<uuid>.jsonl`). Pass it as `--self-id` so this chat is
not treated as busy.

## Loop

```text
1. Do not edit the repo yet (reads/checks only).
2. Run check_idle.py check — see Script.
3. If idle=false → tell the user briefly what is busy; arm a wake (2–5 min) via
   the loop skill / sleep+notify; on wake, repeat from 2. Cap ~60 min unless the
   user said to wait longer; then stop and report still-busy reasons.
4. If idle=true → acquire lock → do the user's task → always release lock
   (success or failure).
```

Do **not** start Write/StrReplace/editing Shell work before a successful acquire.

## Script

From repo root:

```bash
python .agents/skills/mfr-queue/scripts/check_idle.py check --repo . --self-id <uuid>
python .agents/skills/mfr-queue/scripts/check_idle.py acquire --repo . --self-id <uuid> --task "<short label>"
python .agents/skills/mfr-queue/scripts/check_idle.py release --repo . --self-id <uuid>
```

- Exit `0` + `"idle": true` → safe to acquire.
- Exit `1` + `"reasons"` → wait.
- Lock file: `.tmp/agent-lock.json` (gitignored). Stale locks older than 2h are ignored.

Busy signals (any one):

1. **Transcripts** — other parent `agent-transcripts/<id>/<id>.jsonl` with mtime within
   15m whose last line is **not** `turn_ended`
1. **Terminals** — agent/build shells still running (`dotnet test|build|format`,
   `just format|test|lint|build|restore`). Ignores ambient `just run-ui`
1. **Lock** — `.tmp/agent-lock.json` held by another session

## Wake

Use the Cursor **loop** skill: short confirmation that you are waiting, then sleep
wake every ~3 minutes with prompt like `mfr-queue: recheck idle then continue: <task>`.
Do not busy-poll in a tight loop.

## Limits (say once if relevant)

- Ask-mode / read-only chats that still stream can look busy.
- Crashed mid-turn transcripts look busy until `--stale-sec` expires (default 15m).
- Agents that never write tools yet can look idle briefly — acquire lock ASAP after check.
- Cloud agents on other machines are invisible to this checker.
