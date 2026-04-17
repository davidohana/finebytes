# Inserter

Inserts **resolved text** at a fixed **one-based character position** in the target segment. The insert string is expanded with the same **formatter tokens** as [Formatter](Formatter.md) (e.g. `<file-name>`, `<counter:…>`).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `text` | string | Text to insert; may contain `<…>` formatter tokens. |
| `position` | int | One-based index (see **Origin** below). Values below `1` are treated as `1`. |
| `startFrom` | string (enum) | `Beginning` or `End` — see **Origin**. |
| `overwrite` | bool | If `true`, inserted text **replaces** existing characters at the insert index instead of shifting the rest of the segment. |

### Origin (`startFrom`)

| Value | Meaning |
|--------|---------|
| `Beginning` | `position` counts from the **first** character: `1` = insert before the first character. If **`position` exceeds the segment length** (i.e. the insert point would be after the last character), the text is inserted at the **end** of the segment. |
| `End` | `position` counts from the **last** character: `1` = insert before the **last** character. If **`position` exceeds the segment length**, the text is inserted at the **beginning** of the segment. |

### Overwrite (`overwrite`)

When `true`, the segment becomes `original[..insertIndex) + inserted + original[insertIndex + inserted.Length..)` (if the inserted string extends past the end of the original segment, the result is `original[..insertIndex) + inserted`).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `text`: `"_-"`<br>`position`: `3`<br>`startFrom`: `Beginning`<br>`overwrite`: `false` | `01_Mercury_Rave_-_Holes` | `01_-_Mercury_Rave_-_Holes` | |
| `text`: `"X"`<br>`position`: `99`<br>`startFrom`: `Beginning`<br>`overwrite`: `false` | `ab` | `abX` | Position past end → append. |
| `text`: `"_"`<br>`position`: `1`<br>`startFrom`: `End`<br>`overwrite`: `false` | `ab` | `a_b` | Before last character. |
| `text`: `"**"`<br>`position`: `2`<br>`startFrom`: `Beginning`<br>`overwrite`: `true` | `abcd` | `a**d` | Inserts at index before `b`, overwriting `bc`. |

For token reference, see [Formatter](Formatter.md).
