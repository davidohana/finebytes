# ReplaceList

Loads **pairs** from a file (search line / replace line format used by the app). Applies each pair in order, like chaining several [Replacer](Replacer.md) steps. Replacement text can include formatter tokens where supported.

**Example**

- First pair turns `.` into `_`, second pair runs a counter token—behavior matches your list file.

Use **ReplaceList** for long, editable tables; use [Replacer](Replacer.md) for single rules.
