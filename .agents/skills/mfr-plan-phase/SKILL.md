---
name: mfr-plan-phase
description: >-
  Orchestrates the finebytes/MFR phase machine (implement → review → commit →
  next) for plan slices. Use ONLY when the user explicitly asks for that machine:
  @mfr-plan-phase, “phase machine”, “each phase then review then commit”, “do all
  pending phases”, or “review and commit after each”. Do NOT use for a plain
  “implement phase X / do item N / do it” request — that is implement-only (no
  auto-review, no auto-commit). Not for writing a new feature plan
  (mfr-feature-plan) or a single filter/editor without orchestration
  (mfr-implement-filter / mfr-implement-filter-editor).
---

# MFR plan phase orchestration

Drive **one plan slice at a time** with compact subagent briefs. Do not paste skill
bodies or `AGENTS.md` into Task prompts — tell agents to **read** the skill path.

Canonical plans: `docs/plans/*.plan.md` only (never `.cursor/plans/`).

## When this skill applies

- **Yes (full machine)** — `@mfr-plan-phase`, “phase machine”, “each phase then review then commit”, “do all pending”, “review and commit after each” → implement → review → commit → mark → next
- **No (implement-only)** — “implement phase X”, “do P3”, “Open — do #N”, “do it”, paste a plan section → implement that slice only; stop. No review subagent, no commit unless the user separately asked to commit

Plain implement work still follows the plan section’s exit criteria and may mark the plan todo done; it is **not** the phase machine.

## Modes (only inside the phase machine)

- **Phase** — named plan section (F7 P1, phase 3, PR B); scope = that section only
- **Backlog item** — numbered tidy / Open item under a plan; scope = exactly one numbered item

Same loop for both; backlog = smaller phase.

## Loop (full machine only)

```text
For each slice (sequential by default):
1. Sync — note HEAD; dirty tree: only touch files in scope (or stash/stop if unsafe)
2. Read — only the named plan section (not the whole plan unless tiny)
3. Implement — parent or implement subagent; exit criteria from the section
4. Review — separate subagent: “read .agents/skills/mfr-code-review/SKILL.md”; scope = this slice
5. Ship — just format (touched); targeted tests; commit (machine implies Commit: yes)
6. Plan — mark that todo done / strike the backlog item in docs/plans/
7. Next — or stop after one item if user said stop / “one only”
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
Commit: yes
Skills to read (do not paste): mfr-code-review | mfr-implement-filter-editor | …
```

Never paste full `SKILL.md` or AGENTS excerpts. Point at paths.

Plain implement (skill not in play): omit review/commit; do not set `Commit: yes` unless the user asked to commit.

## Ship gate

1. `just format` (or format touched paths if the recipe supports it)
1. Targeted tests for the slice (`dotnet test --filter …` or the plan’s test hint)
1. `just lint` when the slice touches shared surfaces (factories, session, engine)
1. Commit message: why, matching recent `git log` style — only in the phase machine (or when the user explicitly asked to commit)

Style / persistence / naming: already in `AGENTS.md` — do not restate.

## Parent duties

- Keep Task prompts under ~20 lines when possible.
- After each commit, update the plan checkbox/todo in the same turn.
- At the end of a multi-phase run: short summary + aggregated deeper-refactor list (cost-to-value), not a second full review dump.
