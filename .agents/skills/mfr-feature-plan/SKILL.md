---
name: mfr-feature-plan
description: >-
  Writes a phased finebytes/MFR feature plan under docs/plans/: explore stubs,
  MFR7 once via mfr7-reference, lock blocking decisions, split phases. Use when
  the user says plan in details, handover a plan, or designs a new Rename List /
  Applied Filters / FormatEditor / presets / export feature before coding — not
  for implementing phases (use mfr-plan-phase) or a single filter/editor
  (use mfr-implement-filter / mfr-implement-filter-editor).
---

# MFR feature planning

Produce a **handover-ready** plan. Do **not** implement unless the user explicitly
asks after the plan is locked.

Output path: `docs/plans/<kebab-case>.plan.md` only (never `.cursor/plans/`).

## Checklist

```text
Feature plan:
- [ ] 1. Parent plan — find related section (rename-list-ui, applied-filter-editors, …); note status
- [ ] 2. finebytes stubs — existing types, TODOs, nearest sibling UI
- [ ] 3. MFR7 once — read mfr7-reference; paste capability brief into this plan (do not re-crawl later)
- [ ] 4. Blocking decisions — ask at most 1–2 critical questions; lock answers in the plan
- [ ] 5. Phase split — small shippable slices with file scope + exit criteria
- [ ] 6. Write plan — todos, locked decisions, MFR7 brief, non-goals
- [ ] 7. Stop — wait for implement / mfr-plan-phase unless user said implement now
```

## Blocking decisions only

Ask before writing phases when the answer changes architecture or UX meaningfully
(e.g. preview-only vs commit path, one dialog vs two, persist where).

Do **not** ask about style already covered by `AGENTS.md`, naming nits, or
optional polish. Prefer a stated default in the plan over an open option list.

After answers: **lock** them in a “Decisions” section; stop rewriting the whole
plan for non-blocking tweaks — amend that section and affected phases only.

## Plan shape

```markdown
# <Feature> plan

## Decisions (locked)
- …

## MFR7 reference brief
(paste from mfr7-reference Step 7 — sources, behavior, UX, parity gaps)

## Non-goals
- …

## Phases
### P1 — …
- Scope / files:
- Exit criteria:
- Tests:
### P2 — …
…
```

Link the parent plan section when this is a child (e.g. rename-list 14d).

## Explore budget

- Prefer one focused explore (finebytes) + `mfr7-reference` over dual unbounded crawls.
- Reuse existing docs/tests that already cite MFR7; do not re-derive.
- If a parent plan already has a brief for this pane, copy forward — do not re-crawl Help/GIFs.

## Exit

Hand the user: plan path, locked decisions, phase list. Suggest `mfr-plan-phase`
for execution. Do not start coding in the same turn unless asked.
