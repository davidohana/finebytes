# Replace filters

Search-and-replace and cleanup.

---

## Replacer

Finds a pattern in the target segment and replaces it. Modes typically include:

- **Literal** — exact text  
- **Wildcard** — `*` and `?`  
- **Regex** — regular expression  

Options also cover case sensitivity, replace all occurrences, and whole-word matching.

**Example**

- Literal find ` `, replace `_`: `my song` → `my_song`

---

## ReplaceList

Loads **pairs** from a file (search line / replace line format used by the app). Applies each pair in order, like chaining several **Replacer** steps. Replacement text can include formatter tokens where supported.

**Example**

- First pair turns `.` into `_`, second pair runs a counter token—behavior matches your list file.

---

## Cleaner

Removes or substitutes **invalid file-name characters** and any **extra characters** you list, using one replacement string (for example `_` or empty).

**Example**

- Strip `:` and `*` from a segment by replacing them with `-`.

---

### Tips

- Use **Replacer** for one-off rules; **ReplaceList** for long, editable tables.
- Run **Cleaner** early if names must be safe for the filesystem before other filters.
