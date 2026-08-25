# TimeSetter

Sets the **time-of-day** for **creation**, **last write**, or **last access** time on each item. The filter does **not** change the calendar date portion of that timestamp; use [DateSetter](DateSetter.md) for that.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — creation time
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies `File.Set*` time APIs when the preview differs from the scan-time original.

## Examples

- `time`: `09:00:15`; `timestampField`: `creation`
  - Before: Creation `2024-03-15 14:05:30`
  - After: Creation `2024-03-15 09:00:15`
  - Comment: Date preserved.
- `time`: `23:59:01`; `timestampField`: `lastAccess`
  - Before: Last access `2019-01-01 12:00:00`
  - After: Last access `2019-01-01 23:59:01`

## Sample preset (JSON)

```json
{
  "type": "TimeSetter",
  "options": {
    "timestampField": "creation",
    "time": "09:00:15"
  }
}
```

`time` uses the **time-only** form (see `TimeOnly` JSON representation, typically `HH:mm:ss`).
