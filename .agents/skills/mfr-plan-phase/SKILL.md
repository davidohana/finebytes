---
name: mfr-plan-phase
description: >-
  Orchestrates finebytes/MFR plan phases and backlog items: implement one slice,
  review via mfr-code-review, commit, mark the plan todo, then next. Use when the
  user says implement phase/PR X, each phase in a subagent then review then commit,
  do all pending phases, Open—do / tidy item N, or ONE item only — not for writing
  a new feature plan (use mfr-feature-plan) or implementing a single filter/editor
  without a phase machine.
---

# MFR plan phase orchestration

Drive **one plan slice at a time** with compact subagent briefs. Do not paste skill
bodies or `AGENTS.md` into Task prompts — tell agents to **read** the skill path.

Canonical plans: `docs/plans/*.plan.md` only (never `.cursor/plans/`).

## Modes

| Mode | When | Scope |
|------|------|-------|
| **Phase** | “implement F7 P1”, “phase 3”, “PR B” | That plan section only |
| **Backlog item** | “Open — do #N”, “tidy item”, “ONE item only” | Exactly one numbered item |

Same loop for both; backlog = smaller phase.

## Loop

```text
For each slice (sequential by default):
1. Sync — note HEAD; dirty tree: only touch files in scope (or stash/stop if unsafe)
2. Read — only the named plan section (not the whole plan unless tiny)
3. Implement — parent or implement subagent; exit criteria from the section
4. Review — separate subagent: “read .agents/skills/mfr-code-review/SKILL.md”; scope = this slice
5. Ship — just format (touched); targeted tests; commit if user asked / phase machine implies commits
6. Plan — mark that todo done / strike the backlog item in docs/plans/
7. Next — or stop after one item if backlog mode / user said stop
```

**Do not** implement deeper refactors mid-phase. Collect them; report at end (or hand to a later review pass).

## Concurrency

- **Default:** one implement → one review → commit → next (sequential).
- **Parallel:** only if the user asks. Cap **3–5** concurrent agents/worktrees.
- Isolate owned files; do not let parallel agents edit the same factory/shared plan file without a single merger.

## Compact Task brief (required fields only)

```text
Repo: <path>
Mode: phase | backlog-item
Plan: docs/plans/<file>.plan.md — section/item: <id>
Already shipped: <SHAs or “none”>
File scope: <globs / dirs>
Do not touch: <list>
Exit criteria: <bullets>
Commit: yes | no
Skills to read (do not paste): mfr-code-review | mfr-implement-filter-editor | …
```

Never paste full `SKILL.md` or AGENTS excerpts. Point at paths.

## Ship gate

1. `just format` (or format touched paths if the recipe supports it)
2. Targeted tests for the slice (`dotnet test --filter …` or the plan’s test hint)
3. `just lint` when the slice touches shared surfaces (factories, session, engine)
4. Commit message: why, matching recent `git log` style — only when commits are in scope

Style / persistence / naming: already in `AGENTS.md` — do not restate.

## Parent duties

- Keep Task prompts under ~20 lines when possible.
- After each commit, update the plan checkbox/todo in the same turn.
- At the end of a multi-phase run: short summary + aggregated deeper-refactor list (cost-to-value), not a second full review dump.
