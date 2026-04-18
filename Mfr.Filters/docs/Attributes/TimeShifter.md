# TimeShifter

Shifts **creation**, **last write**, or **last access** time on each item by an integer **amount** in the chosen **unit**. A **negative** `amount` moves the timestamp backward; a **positive** `amount` moves it forward.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — creation time
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

`unit` values: `seconds`, `minutes`, `hours`, `days`, `months`, `years` (calendar months and years use `DateTime.AddMonths` / `AddYears`).

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies the filesystem APIs when the preview differs from the scan-time original.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `amount`: `1`<br>`unit`: `days`<br>`timestampField`: `lastWrite` | Last write `2024-03-15 14:05:30` | Last write `2024-03-16 14:05:30` | |
| `amount`: `-2`<br>`unit`: `hours`<br>`timestampField`: `creation` | Creation `2024-03-15 14:05:30` | Creation `2024-03-15 12:05:30` | |

## Sample preset (JSON)

```json
{
  "type": "TimeShifter",
  "options": {
    "timestampField": "lastWrite",
    "amount": 1,
    "unit": "days"
  }
}
```
