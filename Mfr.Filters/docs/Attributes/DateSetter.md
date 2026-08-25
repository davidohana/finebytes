# DateSetter

Sets the **calendar date** for **creation**, **last write**, or **last access** time on each item. The filter does **not** change the time-of-day portion of that timestamp; use [TimeSetter](TimeSetter.md) for that.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — `File.GetCreationTime` / `File.SetCreationTime`
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies the filesystem APIs when the preview differs from the scan-time original.

## Examples

- `date`: `2020-12-25`; `timestampField`: `lastWrite`
  - Before: Last write `2024-03-15 14:05:30`
  - After: Last write `2020-12-25 14:05:30`
  - Comment: Time of day preserved.
- `date`: `2019-01-01`; `timestampField`: `creation`: Creation `2010-06-01 09:00:00` → Creation `2019-01-01 09:00:00`

## Sample preset (JSON)

```json
{
  "type": "DateSetter",
  "options": {
    "timestampField": "lastWrite",
    "date": "2020-12-25"
  }
}
```

`date` is an ISO **date-only** value (`yyyy-MM-DD`).
