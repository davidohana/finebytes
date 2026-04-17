# Cleaner

Replaces **invalid file-name characters** (optional) and any **custom** characters you list with a single **replacement** string (often one safe character or empty).

Examples match [`CleanerFilterTests`](../../Mfr.Tests/Models/Filters/Replace/CleanerFilterTests.cs) (path separators, `@`/`#`, mixed illegal + custom).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `removeIllegalChars` | bool | If `true`, characters invalid in file names on the current OS are replaced. |
| `customCharsToRemove` | string | Extra characters to replace (can be empty). |
| `replacement` | string | Substitute for each removed character (can be empty to delete). |

If there is nothing to remove, the segment is unchanged.

## Examples

| Options | Before | After |
|---------|--------|-------|
| `removeIllegalChars`: `true`, `customCharsToRemove`: `""`, `replacement`: `"_"` | `a/b` | `a_b` |
| `removeIllegalChars`: `false`, `customCharsToRemove`: `"@#"`, `replacement`: `"-"` | `a@b#c` | `a-b-c` |
| `removeIllegalChars`: `true`, `customCharsToRemove`: `"@#"`, `replacement`: `"X"` | `a/b@c#d&#124;e` | `aXbXcXdXe` |

Run **Cleaner** early if later filters assume a safe file name.
