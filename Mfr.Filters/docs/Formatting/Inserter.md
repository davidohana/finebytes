# Inserter

Inserts **resolved text** at a fixed **one-based character position** in the target segment. The insert string is either **literal** or a **formatter template**: a template is used only when `text` contains at least one balanced `<…>` span whose name matches the formatter token-name heuristic (same auto-detection as [AudioTagSetter](../Audio/AudioTagSetter.md) `text`). Otherwise comparisons like `a < b` stay literal.

## Options

- **`text`** (string)
  - Text to insert; formatter tokens apply only when `text` matches the template heuristic (see intro).
- **`position`** (int) — One-based index (see **Origin** below). Values below `1` are treated as `1`.
- **`startFrom`** (string (enum)) — `Beginning` or `End` — see **Origin**.
- **`overwrite`** (bool)
  - If `true`, inserted text **replaces** existing characters at the insert index instead of shifting the rest of the
    segment.

### Origin (`startFrom`)

- **`Beginning`**
  - `position` counts from the **first** character: `1` = insert before the first character. If **`position` exceeds
    the segment length** (i.e. the insert point would be after the last character), the text is inserted at the **end**
    of the segment.
- **`End`**
  - `position` counts from the **last** character: `1` = insert before the **last** character. If **`position` exceeds
    the segment length**, the text is inserted at the **beginning** of the segment.

### Overwrite (`overwrite`)

When `true`, the segment becomes `original[..insertIndex) + inserted + original[insertIndex + inserted.Length..)` (if the inserted string extends past the end of the original segment, the result is `original[..insertIndex) + inserted`).

## Examples

- `text`: `"_-"`; `position`: `3`; `startFrom`: `Beginning`; `overwrite`: `false`
  - Before: `01_Mercury_Rave_-_Holes`
  - After: `01_-_Mercury_Rave_-_Holes`
- `text`: `"X"`; `position`: `99`; `startFrom`: `Beginning`; `overwrite`: `false`
  - Before: `ab`
  - After: `abX`
  - Comment: Position past end → append.
- `text`: `"_"`; `position`: `1`; `startFrom`: `End`; `overwrite`: `false` — `ab` → `a_b` — Before last character.
- `text`: `"**"`; `position`: `2`; `startFrom`: `Beginning`; `overwrite`: `true`
  - Before: `abcd`
  - After: `a**d`
  - Comment: Inserts at index before `b`, overwriting `bc`.

For token reference, see [Formatter](Formatter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Inserter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "text": "_-",
    "position": 3,
    "startFrom": "Beginning",
    "overwrite": false
  }
}
```
