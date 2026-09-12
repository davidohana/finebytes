---
name: mfr-plan-phase
description: >-
  Orchestrates the finebytes/MFR phase machine (implement → optional review →
  commit → next) for plan slices. Use ONLY when the user explicitly asks for that
  machine: @mfr-plan-phase, “phase machine”, “each phase then review then commit”,
  “do all pending phases”, or “review and commit after each”. Do NOT use for a
  plain “implement phase X / do item N / do it” request — that is implement-only
  (no auto-review, no auto-commit). Not for writing a new feature plan
  (mfr-feature-plan) or a single filter/editor without orchestration
  (mfr-implement-filter / mfr-implement-filter-editor).
---

# MFR plan phase orchestration

Drive **one plan slice at a time** with compact subagent briefs. Do not paste skill
bodies or `AGENTS.md` into Task prompts — tell agents to **read** the skill path.

Canonical plans: `docs/plans/*.plan.md` only (never `.cursor/plans/`).

## When this skill applies

- **Yes (full machine)** — `@mfr-plan-phase`, “phase machine”, “each phase then review then commit”, “do all pending”, “review and commit after each” → implement → (review if needed) → commit → mark → next; deferred reviews batch at end
- **No (implement-only)** — “implement phase X”, “do P3”, “Open — do #N”, “do it”, paste a plan section → implement that slice only; stop. No review subagent, no commit unless the user separately asked to commit

Plain implement work still follows the plan section’s exit criteria and may mark the plan todo done; it is **not** the phase machine.

## Modes (only inside the phase machine)

- **Phase** — named plan section (F7 P1, phase 3, PR B); scope = that section only
- **Backlog item** — numbered tidy / Open item under a plan; scope = exactly one numbered item

Same loop for both; backlog = smaller phase.

## Review cadence (parent judgment)

After reading each slice, decide **per-phase review** vs **defer**:

**Per-phase review** when any of these are true:

- Non-trivial logic, new abstraction, or cross-layer wiring (engine/session/UI)
- Shared surfaces (factories, persistence, shortcuts, drag/drop, focus)
- High regression risk or hard-to-reverse API/shape changes
- User asked for review after each / “review and commit after each”

**Defer** (skip the review subagent for this slice) when the slice is small/mechanical:

- Docs-only, checkbox/plan markup, rename/move with no behavior change
- Thin glue, one-liner fixes, obvious test-only additions
- Narrow backlog tidy with clear exit criteria and low blast radius

Track deferred slices (plan id + SHAs/files). Before ending a multi-slice run (or when stopping after “one only” if that one was deferred), launch **one** review subagent covering **all deferred slices together** (`mfr-code-review`, scope = combined). If every slice already got a per-phase review, skip the batch step.

If the user forces “review after each”, always per-phase; still no duplicate batch dump at the end (parent summary only).

## Loop (full machine only)

```text
For each slice (sequential by default):
1. Sync — note HEAD; dirty tree: only touch files in scope (or stash/stop if unsafe)
2. Read — only the named plan section (not the whole plan unless tiny)
3. Decide review — per-phase vs defer (see Review cadence)
4. Implement — parent or implement subagent; exit criteria from the section
5. Review (only if per-phase) — separate subagent: “read .agents/skills/mfr-code-review/SKILL.md”; scope = this slice
6. Ship — just format (touched); targeted tests; commit (machine implies Commit: yes)
7. Plan — mark that todo done / strike the backlog item in docs/plans/
8. Next — or stop after one item if user said stop / “one only”

After the last slice (or when stopping):
9. Batch review — if any slices were deferred, one subagent reviews all of them together
```

**Do not** implement deeper refactors mid-phase. Collect them; report at end (or hand to a later review pass).

### End-of-run deeper-refactor report

When listing deferred deeper refactors, do **not** use one-line titles alone. For each item, elaborate enough that a later agent (or you) can act without re-discovering context:

- **What** — concrete smell / duplication / layering issue (files, types, or call sites)
- **Why it hurts** — maintenance, correctness risk, or friction if left as-is
- **Proposed direction** — preferred approach in a few sentences (not a full design dump)
- **Cost-to-value** — rough risk / LOC / churn vs payoff; rank higher-value items first
- **Suggested timing** — e.g. next tidy pass, before a related feature, or safe to defer indefinitely

Skip trivial nits; prefer 3–8 well-explained items over a long bullet dump.

## Concurrency

- **Default:** one implement → (optional review) → commit → next (sequential).
- **Parallel:** only if the user asks. Cap **3–5** concurrent agents/worktrees.
- Isolate owned files; do not let parallel agents edit the same factory/shared plan file without a single merger.
- Batch review at end is always a single agent (not parallelized per deferred slice).

## Compact Task brief (required fields only)

```text
Repo: <path>
Mode: phase | backlog-item | batch-review
Plan: docs/plans/<file>.plan.md — section/item: <id or list>
Already shipped: <SHAs or “none”>
File scope: <globs / dirs>
Do not touch: <list>
Exit criteria: <bullets>
Commit: yes | no (batch-review: no unless review found must-fix and user/machine already committing fixes)
Skills to read (do not paste): mfr-code-review | mfr-implement-filter-editor | …
```

For **batch-review**, list every deferred slice id + shipped SHAs/paths in `Plan` / `Already shipped` / `File scope`.

Never paste full `SKILL.md` or AGENTS excerpts. Point at paths.

Plain implement (skill not in play): omit review/commit; do not set `Commit: yes` unless the user asked to commit.

## Ship gate

1. `just format` (or format touched paths if the recipe supports it)
1. Targeted tests for the slice (`dotnet test --filter …` or the plan’s test hint)
1. `just lint` when the slice touches shared surfaces (factories, session, engine)
1. Commit message: why, matching recent `git log` style — only in the phase machine (or when the user explicitly asked to commit)

Style / persistence / naming: already in `AGENTS.md` — do not restate.

## Subagent models

When launching `Task` for implement or review:

- **Default:** omit `model` or set `inherit` (Auto).
- **Allowed alternative:** Grok only (`cursor-grok-4.6-high`) when the parent picks a stronger model.
- **Do not** set GPT Sol, Claude, Composer, or any other slug unless the user explicitly named that model in the request.

## Parent duties

- Keep Task prompts under ~20 lines when possible.
- After each commit, update the plan checkbox/todo in the same turn.
- Note in the run (briefly) whether each slice was **reviewed** or **deferred**.
- At the end of a multi-phase run: batch-review any deferred slices (one subagent), then short summary + aggregated deeper-refactor list (cost-to-value) — not a second full review dump beyond that batch.
