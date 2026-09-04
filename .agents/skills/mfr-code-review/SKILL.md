---
name: mfr-code-review
description: >-
  Reviews finebytes/MFR changes for correctness, KISS, YAGNI, naming, stale
  APIs, layering, leftover flags, fragile heuristics, dedup/reuse (local and
  cross-file), and test coverage; applies high-confidence cleanup and adds
  worthwhile tests; surfaces deeper refactor/dedup options clearly even when not
  auto-applied; may delegate to explore/bugbot/security-review when triggers match. Use when the user asks for a code review, deep review,
  KISS/YAGNI pass, simplify/minimize/cleanup/dedup, to review a plan phase or
  prior transcript, or says auto-correct things you are sure about.
---

# MFR code review

Default posture: **correctness first**, then **delete and collapse**, then **tests that earn
their keep**. Prefer a smaller design over a compatible one.

Actively hunt **dedup and reuse** at every scope — local copy-paste, parallel types/views,
shared policy in two layers, twin APIs, and test fixtures. Apply safe local wins in-pass;
**always call out** stronger cross-file or structural dedups clearly in the report (the user
welcomes deeper refactors — do not bury or skip them because they are not auto-applied).

## Resolve scope

Pick one, in this order:

1. **Plan phase** — that phase’s files and leftovers in the same area.
2. **Prior transcript** — what that session changed; do not re-litigate unrelated work.
3. **Path / type** — that code, callers, tests, and any parallel copy.
4. **Unspecified** — current uncommitted + branch diff.

Read nearby domain docs only when the change touches them. Style and no-legacy-compat
policy in `AGENTS.md` are already always-on — do not restate them.

## Apply vs propose

**Apply in the same pass** when the prompt includes simplify, minimize, cleanup, kiss, yagni,
auto-correct, refactor as needed, add coverage, or “review what’s done” after a phase:

- Bugs and incorrect edge cases
- Dead code, unused parameters/flags, leftover APIs, unused wrappers
- Dual fields/methods that always move together; stale predicates after a behavior change
- Names that no longer match behavior, or two names that are too close
- Second sources of truth
- Local helpers that remove copy-paste in files already being touched
- XML `<summary>` on non-obvious private methods (not a `//` comment)
- High-value tests and consolidation of overlapping tests

**Findings first** (report + suggested order, offer to implement) when the prompt is only
“review / see if / see where / can it be” with no action words.

**Propose only** (do not silently do — but **must suggest clearly** in the report):

- New shared types or layer moves
- Merging parallel implementations (e.g. two views, two payloads, two resolvers)
- Extracting a shared helper/base used by multiple callers
- Behavior/interpretation changes, unless the user said they do not mind
- Abstractions with a single caller (still note if a second caller is imminent)
- Cross-file fixture unifications that are a dedicated pass
- Caching or perf work with no measured cost

For each proposed dedup/refactor: name the **duplicated sites**, the **target shape**
(what to extract, merge, or delete), **payoff** (lines, drift risk, one source of truth),
**scope/cost** (files, tests, risk), and a **suggested order** if several items relate.
Do not treat “deeper refactor” as optional silence — if duplication exists, say so.

If leftover work is real after a named plan phase, list it in the report. Write a follow-up
doc only when the user asks to handover, or when that phase already has one.

## Optional subagents

**Default: one reviewer, one synthesized report.** Most reviews stay in-process — correctness,
KISS, dedup, layering, naming, and tests overlap and the parent often applies fixes in the
same pass.

Delegate only for **discovery** or **specialized audits**. Subagent output is input; the parent
merges, dedupes, prioritizes, and writes the final report. Subagents do **not** apply fixes.

| Subagent | Launch when | Skip when |
|----------|-------------|-----------|
| **`explore`** | Hunting **cross-file dedup/reuse** — parallel views, payloads, DnD handlers, label maps, twin resolvers; scope is path/type and copies may live outside the diff | Diff is small, self-contained, or parallel sites are already obvious from open files |
| **`bugbot`** | User asked; or diff is **large/risky** (rename engine, batch apply, cancel/progress, persistence, concurrency) | Typical UI/VM refactors; small feature-area reviews — overlaps the Correctness lens |
| **`security-review`** | Diff touches **security-sensitive** surfaces: path traversal, deserializing untrusted input, shell/process spawn, network | Routine desktop UI, filters, session layout, internal models |

**Do not** spin up a subagent per report section (Correctness agent, Dedup agent, etc.) — weak
synthesis, heavy overlap.

**`explore` prompt shape** — name the pattern, not just the changed file:

```text
Find parallel implementations of <pattern> across the repo (e.g. drag payloads, palette→list
drop, FilterTargetLabels-style maps). Return: file paths, what each does, and which pairs look
mergeable into one owner.
```

**`bugbot` / `security-review`** — follow `.cursor/skills-cursor/review-bugbot/SKILL.md` and
`review-security/SKILL.md` prompt shapes; default `Diff: branch changes`.

Fold subagent findings into **Dedup / reuse**, **Correctness**, or **Deeper refactors**;
note in the verdict when a subagent was used.

## Review lenses

### Correctness

- Edge cases: duplicates, unknown keys, empty/missing data, no-ops, cancel, second call
- Persist one current schema; unknown values fall back to defaults — no migration shims
- Do not apply the same policy twice across layers
- Side effects (selection, status, hints) must not reset when the primary action is unrelated
- Do not detect errors by comparing user-visible text to a sentinel
- User-cancel of a batch: return/break unless the API is actually throwing-cancel
- Progress counts and phase labels must match the work that is happening

### Dedup / reuse

Hunt in this order:

1. **Same file** — repeated blocks, twin branches, copy-pasted guards; extract a local helper
   when it removes real duplication (apply if already touching the file).
2. **Same feature area** — parallel partials, views, payloads, label maps, DnD handlers;
   compare side by side and note what could be one type or one code path.
3. **Cross-layer** — UI re-resolving what domain already decides; duplicate validation or
   mapping in VM + view + tests.
4. **Tests** — same scenario through VM + headless + integration; near-identical facts;
   fixtures that could be one builder.

When two sites implement the same policy or shape, prefer **one owner** — even if merging
them is a follow-up pass. Flag “same logic, two homes” explicitly.

### KISS / YAGNI / cleanup

- Delete leftover APIs from an earlier step
- One source of truth for names, keys, and labels
- Prefer deleting a wrapper and exposing the real method over keeping both
- Independent flags are fine; if a combination is illegal, reject it early or use an enum
- Split long methods and oversized types by concern, not ceremony
- Do not add speculative cache/prefetch/generic factories, marker attributes inferable from
  type, helper scripts, or a forwarding type that does not earn its name
- Match reference-app **behavior**, not its structure. Changing an interpretation is allowed
  when it removes a special case — call that out
- Routine Information logs for user-driven actions are usually noise
- Guard a no-op before the loop. Question `try/finally` with nothing to clean up. Prefer a
  local in a tight method over repeating a member
- Do not invent a one-off type for a single format when a general mechanism will be needed
  next. Do not duplicate magic numbers

### Layering

Follow `docs/mfr-folder-layering.md`. Domain policy lives below the UI; the UI collects inputs
and binds. If UI code is a second resolver, move or delete it.

Do not pass a whole object into a helper that only needs a few values. Persist UI layout in
session, not process config.

### Naming and docs

- A name must match **current** behavior. Drop hedges (`Maybe`, `Effective`). Avoid jargon.
- Two identifiers that cannot be told apart at a glance are too close — rename them.
- Fields and params should name the type or unit, not a vague role.
- Prefer one general API over a singular/plural or parse/try-parse twin that share a body.
  Prefer one field when two are always written together.
- After a behavior change, hunt stale predicates, comments, and type names.
- Explicit registration when order/defaults/identity are the product; reflection when it is
  an open catalog. Namespaces match folders. Question every warning suppression.
- Non-obvious private methods get XML `<summary>`. Load-bearing concepts belong there, not
  in a `//` comment.
- Store structured errors and present them generally. Do not build fragile per-case
  user-message formatters.

### Tests

**Add** when the path is untested and can fail in production.

**Skip** asserting every label, re-testing the same helper through another layer, trivial
constant/map tests, and legacy-load tests.

**Consolidate** duplicate coverage across layers, near-identical facts into one theory or
loop, and tests whose API was deleted. Drop low-value tests rather than rewriting them.
Reuse existing fixtures when already touching that suite.

View/AXAML sync needs a **gesture** (or construct/layout) headless test — selection, DnD
press-collapse, stolen keys, focus routing, recycle, MinWidth/hit-test, AXAML load, session
visual state. Do not treat `*ViewModelTests` as that coverage. Do not convert existing VM
tests into headless tests; add a gesture fact instead. Play-found UI bug → lock with the
same gesture, not a VM setter.

## After edits

Run the affected tests. Format touched files if layout changed. Do not commit unless asked.

## Report

Lead with a one-paragraph verdict. Then:

```markdown
## Correctness (fixed | found)
## KISS / YAGNI (removed | proposed)
## Dedup / reuse (applied | proposed)
## Naming / docs (if any)
## Tests (added | consolidated | skipped)
## Deeper refactors (not done — always include when duplication exists)
## What to keep / what not to simplify
```

Be specific (type/method names). Separate **applied** from **proposed**.

**Dedup / reuse** and **Deeper refactors** may overlap — use Dedup for concrete duplication
found; use Deeper refactors for structural follow-ups (shared types, layer moves, multi-file
merges). Each proposed item needs: duplicated sites → target shape → payoff → scope/cost →
suggested order. If nothing is worth doing, say that and stop.
