---
title: Audio tag frame model (implementation plan)
description: SUPERSEDED — see audio_tag_simplification plan (parsed fields + field-patch Apply).
---

# SUPERSEDED

This document is **superseded** by the handoff plan:

**Audio tag simplification** — `~/.cursor/plans/audio_tag_simplification_15285120.plan.md`

That plan replaces the blob-based overlay (`SerializedTagBlob` / Id3v2 `CanonicalTagBytes`) with **parsed per-TagTypes fields**, in-memory filter mutation, and **Original→Preview field-patch Apply**.

Do not implement new work from the phases below. Historical checklist kept only for archaeology.

---

# Audio tags: per-type overlays and frame-aware persistence (archive)

## Overview

Phased work (historical): (1) structured overlay + MP3 ID3v1/v2 round-trip and semantic mapping, (2) extend to other TagLib tag types, (3) harden parity with existing filters/commit tests, **(4) derived semantic fields from blocks only**, **(5) selective per-type tag deletion**, **(6) frame-level filter targets**.

## Checklist (phases) — do not execute

- [x] **Phase 1–3 (partial):** Structured overlay existed with blob-backed Xiph/Ape/Riff and Id3v2 canonical bytes (replaced in simplification Phase 0).
- [ ] **Phase 4–6:** Superseded by simplification Phases 0–E (parsed fields, policy, selective remove, recommended create, specific targets, field-patch Apply).

See the simplification plan for locked decisions, data model, and verification commands.
