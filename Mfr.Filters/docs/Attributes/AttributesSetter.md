# AttributesSetter

Sets or clears **read-only**, **hidden**, **archive**, and **system** filesystem attributes on every item in the rename list. The filter targets metadata, not the file name. Preview updates `Attributes` on each item; commit applies `File.SetAttributes` when the preview differs from the scan-time original.

Each option uses a tri-state:

- **`Set`** — turn the flag on.
- **`Clear`** — turn the flag off.
- **`Keep`** — leave the flag as it is on the current preview (default in the UI as the Keep radio).

Other attribute bits (for example `Directory`) are preserved; only the four flags above are modified according to options.

## Examples

- `readOnly`: `Keep`; `hidden`: `Set`; `archive`: `Keep`; `system`: `Keep`
  - Before: `(file on disk without H)`
  - After: `Preview/commit adds **Hidden**`
  - Comment: Path unchanged; commit still runs.
- `readOnly`: `Keep`; `hidden`: `Keep`; `archive`: `Clear`; `system`: `Keep` — `File had **Archive**` → `Archive cleared`
- All `Keep` — `any` → `unchanged` — No preview delta from this filter.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "AttributesSetter",
  "options": {
    "readOnly": "Keep",
    "hidden": "Set",
    "archive": "Keep",
    "system": "Keep"
  }
}
```

Valid tri-state values: `Set`, `Clear`, `Keep` (case-insensitive with default preset options).
