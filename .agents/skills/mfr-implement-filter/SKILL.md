---
name: mfr-implement-filter
description: >-
  Implements Magic File Renamer preset filters in this repo: BaseFilter records,
  JSON polymorphism registration, tests, and Mfr.Filters docs. Use when adding or
  changing a filter in Mfr.Filters, preset deserialization, or filter documentation.
---

# MFR: implement a filter

For Filter Configuration **option editors** (VM / AXAML / factory / tests), use [mfr-implement-filter-editor](../mfr-implement-filter-editor/SKILL.md) instead.

## Code

1. **Location.** Add the filter class under `Mfr.Filters/<Group>/` (e.g. `Space/`, `Case/`). Filters sit in **L2**; they reference `Mfr.Models` only (see `docs/mfr-folder-layering.md`).

1. **Shape.**

   - **String-target filters (prefix/extension/full today):** `public sealed record YourFilter(...) : StringTargetFilter(Target)` in **`Mfr.Filters`** (same as most built-ins; `StringTargetFilter` is in the **`Mfr.Filters`** namespace).
   - **Other targets** (attributes, future ID3, etc.): `public sealed record YourFilter(...) : BaseFilter(Target)` and `protected internal override void ApplyCore(RenameItem item)`.

1. **`Type`.** `public override string Type => "YourFilterType";` — string must match the JSON `type` discriminator and `JsonDerivedType` registration **exactly**.

1. **Transform (`StringTargetFilter`).** Implement `protected override string _TransformValue(string value, RenameItem item)`. Use `RenameItem` / `item.WordSeparator` when behavior depends on pipeline state (e.g. word separator set by an earlier `SpaceCharacter` filter). Override `protected virtual void _Setup()` only for one-time setup before transforms. If `_Setup` caches instance fields, **always assign every cache** (including clear to null/default) — record `with` copies those fields while resetting setup-complete (see `BaseFilter` remarks).

1. **Options.** If the filter has settings, add a `YourOptions` `sealed record` in the same file or adjacent file, with camelCase JSON property names via record positional params / `[JsonPropertyName]` as needed.

1. **`Target` (`StringTargetFilter` / string-target presets).** `FilterTarget` is polymorphic; JSON discriminator is **`targetType`**:

   | `targetType` value | Fields                                         | Addresses                                                                            |
   | ------------------ | ---------------------------------------------- | ------------------------------------------------------------------------------------ |
   | `FilePrefix`       | (none)                                         | Prefix (name without extension)                                                      |
   | `FileExtension`    | (none)                                         | Extension including leading dot                                                      |
   | `FileFullName`     | (none)                                         | Full file name (prefix + extension); writes parse via `Path.GetFileName` rules       |
   | `AncestorFolder`   | `level`: positive integer                      | Single ancestor segment name vs preview directory                                    |
   | `FullPath`         | (none)                                         | Entire preview full path                                                             |
   | `ParentDirectory`  | (none)                                         | Preview containing directory path                                                    |
   | `AudioField`       | `field`                                        | Generic semantic overlay field (`title`, `album`, …); broadcast / recommended create |
   | `Id3v1Field`       | `field`                                        | One ID3v1 scalar (MPEG only)                                                         |
   | `Id3v2Frame`       | `frameId`, optional `language` / `description` | One modeled ID3v2 frame (MPEG only)                                                  |
   | `XiphField`        | `key`                                          | One Xiph / Vorbis comment key (FLAC/Ogg only)                                        |

1. **JSON registration.** In `Mfr.Engine/Presets/PresetJsonOptions.cs`, add `using Mfr.Filters.<Group>;` if missing and append `new JsonDerivedType(typeof(YourFilter), "YourFilterType")` to `DerivedTypes` (follow the ordering style already used in that list).

1. **Palette catalog.** Add `[FilterPalette(FilterGroup.<Group>, "Display Name")]` on the filter class (same group folder / `FilterGroup` value). Display names use spaces in PascalCase; keep MFR 7 exceptions (`TagRemover` → `Audio Tag Remover`, `FixLeadingZeros` → `Fix Leading 0's`, `Id3v2FieldSetter` → `ID3v2 Field Setter`). `PathMover` display is `Path Mover` (MFR7 name was Mover). `FilterCatalog` discovers entries by reflection; completeness is guarded by tests against `PresetJsonOptions`.

## Tests

Add `Mfr.Tests/Models/Filters/<Group>/YourFilterTests.cs`. Use `FilterTestHelpers.ApplyToPrefix`, `ApplyReturnItem`, and/or `CreateRenameItem` + `filter.Setup()` + `filter.Apply(item)`. Mirror edge cases and cross-filter ordering when relevant.

## Docs

Add `Mfr.Filters/docs/<Group>/YourFilterType.md` (examples table per `Mfr.Filters/docs/README.md`) and link it from `Mfr.Filters/docs/README.md` in the right group section.

## Deeper context

Architecture and preset shape: `docs/magic-file-renamer-design.md`. Existing filters (`SeparateCapitalizedTextFilter`, `SpaceCharacterFilter`, `TokenMoverFilter`) are good references for “no options”, “mutates `RenameItem` + options”, and “rich options”.
