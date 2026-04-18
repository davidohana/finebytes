# DateSetter

Sets the **calendar date** for **creation**, **last write**, or **last access** time on each item. The filter does **not** change the time-of-day portion of that timestamp; use [TimeSetter](TimeSetter.md) for that.

Choose which timestamp field to edit with `target.family`:

- **`CreationDate`** — `File.GetCreationTime` / `File.SetCreationTime`
- **`LastWriteDate`** — last modified
- **`LastAccessDate`** — last accessed

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies the filesystem APIs when the preview differs from the scan-time original.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `date`: `2020-12-25`<br>target: `LastWriteDate` | Last write `2024-03-15 14:05:30` | Last write `2020-12-25 14:05:30` | Time of day preserved. |
| `date`: `2019-01-01`<br>target: `CreationDate` | Creation `2010-06-01 09:00:00` | Creation `2019-01-01 09:00:00` | |

## Sample preset (JSON)

```json
{
  "type": "DateSetter",
  "target": { "family": "LastWriteDate" },
  "options": {
    "date": "2020-12-25"
  }
}
```

`date` is an ISO **date-only** value (`yyyy-MM-DD`).
