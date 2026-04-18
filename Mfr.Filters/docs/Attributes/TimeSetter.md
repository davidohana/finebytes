# TimeSetter

Sets the **time-of-day** for **creation**, **last write**, or **last access** time on each item. The filter does **not** change the calendar date portion of that timestamp; use [DateSetter](DateSetter.md) for that.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — creation time
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies `File.Set*` time APIs when the preview differs from the scan-time original.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `time`: `09:00:15`<br>`timestampField`: `creation` | Creation `2024-03-15 14:05:30` | Creation `2024-03-15 09:00:15` | Date preserved. |
| `time`: `23:59:01`<br>`timestampField`: `lastAccess` | Last access `2019-01-01 12:00:00` | Last access `2019-01-01 23:59:01` | |

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
