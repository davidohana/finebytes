---
name: mfr-code-review
description: >-
  Reviews finebytes/MFR changes for correctness, KISS, YAGNI, naming, stale
  APIs, layering, leftover flags, fragile heuristics, and test coverage;
  applies high-confidence cleanup and adds worthwhile tests. Use when the user
  asks for a code review, deep review, KISS/YAGNI pass, simplify/minimize/
  cleanup/dedup, to review a plan phase or prior transcript, or says
  auto-correct things you are sure about.
---

# MFR code review

Default posture: **correctness first**, then **delete and collapse**, then **tests that earn
their keep**. Prefer a smaller design over a compatible one.

## Resolve scope

Pick one, in this order:

1. **Plan phase** — that phase’s files and leftovers in the same area.
2. **Prior transcript** — what that session changed; do not re-litigate unrelated work.
3. **Path / type** — that code, callers, tests, and any parallel copy.
4. **Unspecified** — current uncommitted + branch diff.

Read nearby domain docs only when the change touches them. Style and no-legacy-compat rules
are already always-on — do not restate them.

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

**Propose only** (do not silently do):

- New shared types or layer moves
- Behavior/interpretation changes, unless the user said they do not mind
- Abstractions with a single caller
- Cross-file fixture unifications that are a dedicated pass
- Caching or perf work with no measured cost

If leftover work is real after a named plan phase, list it in the report. Write a follow-up
doc only when the user asks to handover, or when that phase already has one.

Do not launch Bugbot / security-review subagents unless the user asked for those.

## Review lenses

### Correctness

- Edge cases: duplicates, unknown keys, empty/missing data, no-ops, cancel, second call
- Persist one current schema; unknown values fall back to defaults — no migration shims
- Do not apply the same policy twice across layers
- Side effects (selection, status, hints) must not reset when the primary action is unrelated
- Do not detect errors by comparing user-visible text to a sentinel
- User-cancel of a batch: return/break unless the API is actually throwing-cancel
- Progress counts and phase labels must match the work that is happening

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
## Naming / docs (if any)
## Tests (added | consolidated | skipped)
## Deeper refactors (not done)
## What to keep / what not to simplify
```

Be specific (type/method names). Separate **applied** from **proposed**. For deeper items:
why it pays off and why not now. If nothing is worth doing, say that and stop.
