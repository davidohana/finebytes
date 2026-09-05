# TimeShifter

Shifts **creation**, **last write**, or **last access** time on each item by an integer **amount** in the chosen **unit**. A **negative** `amount` moves the timestamp backward; a **positive** `amount` moves it forward.

Choose which timestamp field to edit with the `timestampField` property inside `options`:

- **`creation`** — creation time
- **`lastWrite`** — last modified
- **`lastAccess`** — last accessed

`unit` values: `seconds`, `minutes`, `hours`, `days`, `months`, `years` (calendar months and years use `DateTime.AddMonths` / `AddYears`).

Shifted calendar dates stay inside the shared product range (`1601-01-01`..`2100-12-31`, same as Date/Time Setter). If the shift would leave that range — or `DateTime.Add*` throws because the amount/result is out of range — the field is clamped to the nearer endpoint and the original time-of-day is kept.

Preview updates the corresponding `DateTime` on `FileMeta`; commit applies the filesystem APIs when the preview differs from the scan-time original.

## Examples

- `amount`: `1`; `unit`: `days`; `timestampField`: `lastWrite`
  - Before: Last write `2024-03-15 14:05:30`
  - After: Last write `2024-03-16 14:05:30`
- `amount`: `-2`; `unit`: `hours`; `timestampField`: `creation`
  - Before: Creation `2024-03-15 14:05:30`
  - After: Creation `2024-03-15 12:05:30`

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
