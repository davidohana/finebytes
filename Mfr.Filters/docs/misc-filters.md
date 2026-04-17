# Misc filters

Small utilities that do not fit the other groups.

---

## StripParentheses

Removes one kind of bracket pair: round `()`, square `[]`, curly `{}`, or angle `<>`. You can remove **only the brackets** or **the brackets and everything inside** (depending on options).

**Examples** (round, remove contents)

- `Song (live)` → `Song `
- `Song (live)` with delimiters only removed → `Song live`

---

## FixLeadingZeros

Finds digit sequences in the text and normalizes **leading zeros** to a target width (for example track numbers). Options can limit how many numbers are changed, require **whole-word** numbers only, and optionally strip extra zeros before padding.

**Example**

- Width 2, many files: `09` stays two digits; `9` may become `09` depending on settings.

---

### Tips

- Use **StripParentheses** to drop edition tags like `(Remaster)` or `[EP]` from names.
- Use **FixLeadingZeros** so `1`, `01`, and `001` line up for sorting.
