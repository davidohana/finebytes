# DateTimeSetter

Sets the **calendar date** and/or **time-of-day** for **creation**, **last write**, or **last access** time on each item. Use `setDate` / `setTime` to choose which parts to replace; unchecked parts keep the existing preview values.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — `File.GetCreationTime` / `File.SetCreationTime`
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies the filesystem APIs when the preview differs from the scan-time original.

## Examples

- `setDate`: true; `date`: `2020-12-25`; `setTime`: false; `timestampField`: `lastWrite`
  - Before: Last write `2024-03-15 14:05:30`
  - After: Last write `2020-12-25 14:05:30`
  - Comment: Time of day preserved.
- `setDate`: false; `setTime`: true; `time`: `09:00:15`; `timestampField`: `creation`
  - Before: Creation `2024-03-15 14:05:30`
  - After: Creation `2024-03-15 09:00:15`
  - Comment: Date preserved.
- `setDate`: true; `date`: `2019-01-01`; `setTime`: true; `time`: `23:59:01`; `timestampField`: `lastAccess`
  - Before: Last access `2024-03-15 14:05:30`
  - After: Last access `2019-01-01 23:59:01`

## Sample preset (JSON)

```json
{
  "type": "DateTimeSetter",
  "options": {
    "timestampField": "lastWrite",
    "setDate": true,
    "date": "2020-12-25",
    "setTime": true,
    "time": "14:05:30"
  }
}
```

`date` is an ISO **date-only** value (`yyyy-MM-DD`). `time` uses the **time-only** form (typically `HH:mm:ss`).
