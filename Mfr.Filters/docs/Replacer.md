# Replacer

Finds a pattern in the target segment and replaces it. Modes typically include:

- **Literal** — exact text  
- **Wildcard** — `*` and `?`  
- **Regex** — regular expression  

Options also cover case sensitivity, replace all occurrences, and whole-word matching.

**Example**

- Literal find ` `, replace `_`: `my song` → `my_song`

Use **Replacer** for one-off rules; for long, editable tables see [ReplaceList](ReplaceList.md).
