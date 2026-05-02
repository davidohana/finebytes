---
name: mfr-implement-filter
description: >-
  Implements Magic File Renamer preset filters in this repo: BaseFilter records,
  JSON polymorphism registration, tests, and Mfr.Filters docs. Use when adding or
  changing a filter in Mfr.Filters, preset deserialization, or filter documentation.
---

# MFR: implement a filter

## Code

1. **Location.** Add the filter class under `Mfr.Filters/<Group>/` (e.g. `Space/`, `Case/`). Filters sit in **L2**; they reference `Mfr.Models` only (see `docs/mfr-folder-layering.md`).

2. **Shape.**
   - **String-target filters (prefix/extension/full today):** `public sealed record YourFilter(...) : StringTargetFilter(Target)` (same as most built-ins).
   - **Other targets** (attributes, future ID3, etc.): `public sealed record YourFilter(...) : BaseFilter(Target)` and `protected internal override void ApplyCore(RenameItem item)`.

3. **`Type`.** `public override string Type => "YourFilterType";` — string must match the JSON `type` discriminator and `JsonDerivedType` registration **exactly**.

4. **Transform (`StringTargetFilter`).** Implement `protected override string _TransformValue(string value, RenameItem item)`. Use `RenameItem` / `item.WordSeparator` when behavior depends on pipeline state (e.g. word separator set by an earlier `SpaceCharacter` filter). Override `protected virtual void _Setup()` only for one-time setup before transforms.

5. **Options.** If the filter has settings, add a `YourOptions` `sealed record` in the same file or adjacent file, with camelCase JSON property names via record positional params / `[JsonPropertyName]` as needed.

6. **JSON registration.** In `Mfr.Core/PresetJsonOptions.cs`, add `using Mfr.Filters.<Group>;` if missing and append `new JsonDerivedType(typeof(YourFilter), "YourFilterType")` to `DerivedTypes` (follow the ordering style already used in that list).

## Tests

Add `Mfr.Tests/Models/Filters/<Group>/YourFilterTests.cs`. Use `FilterTestHelpers.ApplyToPrefix` and/or `CreateRenameItem` + `filter.Setup()` + `filter.Apply(item)`. Mirror edge cases and cross-filter ordering when relevant.

## Docs

Add `Mfr.Filters/docs/<Group>/YourFilterType.md` (examples table per `Mfr.Filters/docs/README.md`) and link it from `Mfr.Filters/docs/README.md` in the right group section.

## Deeper context

Architecture and preset shape: `docs/magic-file-renamer-design.md`. Existing filters (`SeparateCapitalizedTextFilter`, `SpaceCharacterFilter`, `TokenMoverFilter`) are good references for “no options”, “mutates `RenameItem` + options”, and “rich options”.
