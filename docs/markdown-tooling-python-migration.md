# Handover: Switch markdown tooling from npm to Python

**Status: complete.** Markdown format/lint uses Python (`requirements-md.txt`, `.venv`, `just format-md` / `lint-md`). See `CONTRIBUTING.md`.

Plan that replaced **Prettier + markdown-table-formatter + markdownlint-cli2** with **mdformat (+ gfm) + PyMarkdown**, while keeping the same `just format` / `just lint` workflow.

______________________________________________________________________

## Goal

| Today (npm)                            | Target (Python)                                                  |
| -------------------------------------- | ---------------------------------------------------------------- |
| Prettier                               | **mdformat**                                                     |
| markdown-table-formatter               | **mdformat-gfm** (`compact_tables = false`, default)             |
| markdownlint-cli2                      | **pymarkdownlnt** (`pymarkdown scan`)                            |
| `package.json` + `node_modules`        | **`pyproject.toml`** (or `requirements.txt`) + `.venv`           |
| `scripts/*.mjs`, `scripts/run-npm.cmd` | Remove                                                           |
| `.prettierrc.json`, `.prettierignore`  | **`.mdformat.toml`**                                             |
| `.markdownlint-cli2.jsonc`             | **`pymarkdown.toml`** or `[tool.pymarkdown]` in `pyproject.toml` |

C# tooling (CSharpier, `dotnet format`) stays unchanged.

______________________________________________________________________

## Phase 0 — Prerequisites and decision

**Confirm Python is acceptable** as a dev dependency (like Node is today).

- **Recommended:** Python **3.12+** (3.13+ if you want native `exclude` in `.mdformat.toml`; otherwise pass paths/excludes from `just`).
- **Recommended:** **[uv](https://docs.astral.sh/uv/)** for venv + lockfile (`uv.lock`), or plain `pip` + `requirements.txt` if you want minimal tooling.

**Spike first (1–2 hours)** before full migration:

1. Install tools in a throwaway venv.
1. Run on 3 representative files:
   - `Mfr.Filters/docs/Replace/Replacer.md` (aligned table + `<br>` in cells)
   - `docs/audio-tag-model.md` (YAML front matter + mermaid fence)
   - `docs/rename-list-cell-hint-after-delete-bug.md` (many tables)
1. Compare diffs to current npm output. **Go/no-go** on HTML-in-tables and front matter.

If spike fails on `<br>` or front matter, options are:

- `mdformat-frontmatter` plugin
- `<!-- mdformat-off -->` regions for specific tables
- Keep npm **lint-only**, Python **format-only**

______________________________________________________________________

## Phase 1 — Add Python dependencies

### Option A: `pyproject.toml` (preferred)

```toml
[project]
name = "finebytes-dev"
version = "0.0.0"
requires-python = ">=3.12"
dependencies = []

[project.optional-dependencies]
md = [
  "mdformat>=0.7",
  "mdformat-gfm>=2.0",
  "pymarkdownlnt>=0.9",
]

[tool.uv]
dev-dependencies = [
  "mdformat>=0.7",
  "mdformat-gfm>=2.0",
  "pymarkdownlnt>=0.9",
]
```

### Option B: `requirements-md.txt`

```text
mdformat>=0.7
mdformat-gfm>=2.0
pymarkdownlnt>=0.9
```

**Install:**

```powershell
uv venv
uv pip install -e ".[md]"   # or: pip install -r requirements-md.txt
```

**Gitignore:** add `.venv/` (keep or remove `node_modules/` after cutover).

______________________________________________________________________

## Phase 2 — Config files

### `.mdformat.toml` (repo root)

Map current Prettier behavior:

```toml
# Match .editorconfig / .prettierrc.json
wrap = "keep"              # proseWrap: preserve
end_of_line = "crlf"

# Column-aligned tables (default; do NOT set compact_tables = true)
[plugin.tables]
compact_tables = false

# If Python 3.13+:
# exclude = [".cursor/**", "**/bin/**", "**/obj/**", "node_modules/**"]
```

**Note:** On Python < 3.13, exclusions must be done in `just` (explicit paths or `--exclude` flags), not in TOML.

### PyMarkdown config

Port `.markdownlint-cli2.jsonc` rules:

| markdownlint                             | PyMarkdown action                                                                |
| ---------------------------------------- | -------------------------------------------------------------------------------- |
| `MD013: false`                           | disable `md013`                                                                  |
| `MD025: false`                           | disable `md025` (YAML front matter + H1)                                         |
| `MD033: allowed ["br"]`                  | `[plugins.md033]` → `allowed_elements = "br,!--,![CDATA["` (verify exact syntax) |
| `MD036: false`                           | disable `md036`                                                                  |
| `MD038: false`                           | disable `md038`                                                                  |
| `MD041: false`                           | disable `md041`                                                                  |
| ignores: `.cursor/**`, `**/bin/**`, etc. | CLI globs or `plugins.per-file-ignores`                                          |

Example `pymarkdown.toml`:

```toml
[plugins.md013]
enabled = false

[plugins.md025]
enabled = false

[plugins.md033]
allowed_elements = "br,!--,![CDATA["

[plugins.md036]
enabled = false

[plugins.md038]
enabled = false

[plugins.md041]
enabled = false
```

Validate rule IDs against [PyMarkdown rules reference](https://pymarkdown.readthedocs.io/en/latest/rules/) during spike.

______________________________________________________________________

## Phase 3 — Helper script (optional but recommended)

Small cross-platform wrapper avoids Windows/Git Bash PATH pain (same issue as `npm.cmd`).

**`scripts/md-tools.ps1`** or **`scripts/md-tools.sh`** + **`scripts/run-md.cmd`**:

- Activates `.venv` (or calls `.venv\Scripts\mdformat.exe` directly on Windows)
- Runs format or lint with consistent file list

**File list** (mirror current `scripts/markdown-table-paths.mjs`):

- Include: `docs/`, `Mfr.Filters/docs/`, `AGENTS.md`, `CONTRIBUTING.md`, root `README.md`, etc.
- Exclude: `.cursor/**`, `**/bin/**`, `**/obj/**`

Example commands:

```bash
# Format
mdformat docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md

# Lint (format check + pymarkdown)
mdformat --check docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md
pymarkdown scan docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md
```

With mdformat, **format check replaces** Prettier `--check` **and** the separate table alignment check — aligned tables are enforced by `mdformat-gfm` in one step.

______________________________________________________________________

## Phase 4 — Update `justfile`

Replace npm recipes:

```just
# Shared: ensure venv exists (pick one approach)
_ensure-py-md:
    uv sync --group md    # or: python -m venv .venv && .venv/Scripts/pip install ...

format-md:
    just _ensure-py-md
    {{py}} -m mdformat docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md

lint-md:
    just _ensure-py-md
    {{py}} -m mdformat --check docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md
    {{py}} -m pymarkdown scan docs Mfr.Filters/docs AGENTS.md CONTRIBUTING.md
```

**Windows:** invoke `.venv\Scripts\python.exe` explicitly (same pattern as `run-npm.cmd`), or use `uv run mdformat …` which handles venv.

**Remove:** `[unix]`/`[windows]` npm split, `run-npm.cmd`.

______________________________________________________________________

## Phase 5 — One-time doc reformat

1. Run `just format-md` on a branch.
1. Review diff focus areas:
   - `Mfr.Filters/docs/**` Examples tables
   - `docs/audio-tag-model.md`, `docs/image-metadata-model.md` (front matter + mermaid)
   - Link reference shuffling (mdformat moves link refs to bottom by default — **behavior change** vs Prettier)
1. Run `just lint-md`, fix config until clean.
1. Run full `just lint` + `just test`.

**Expect a large one-time diff** — same as when npm tooling was first added.

______________________________________________________________________

## Phase 6 — Remove npm artifacts

Delete after green CI/local lint:

| Remove                                | Reason                         |
| ------------------------------------- | ------------------------------ |
| `package.json`, `package-lock.json`   | Replaced by Python deps        |
| `node_modules/`                       | No longer needed               |
| `.prettierrc.json`, `.prettierignore` | Replaced by `.mdformat.toml`   |
| `.markdownlint-cli2.jsonc`            | Replaced by pymarkdown config  |
| `scripts/format-markdown-tables.mjs`  | Tables in mdformat-gfm         |
| `scripts/check-markdown-tables.mjs`   | `mdformat --check` covers this |
| `scripts/markdown-table-paths.mjs`    | Moved to just/script           |
| `scripts/run-npm.cmd`                 | No npm                         |

Update:

- `CONTRIBUTING.md` — Python/uv prerequisite instead of Node.js
- `AGENTS.md` — mdformat + pymarkdown in format/lint bullets
- `.editorconfig` — comment referencing npm → Python tools
- `.gitignore` — `.venv/`, drop `node_modules/` if desired

______________________________________________________________________

## Phase 7 — Docs and contributor onboarding

**CONTRIBUTING.md** should say:

```powershell
# One-time
uv sync          # or: pip install -r requirements-md.txt

just format
just lint
```

Remove Node.js LTS from prerequisites unless something else needs it.

______________________________________________________________________

## Risk register

| Risk                                     | Mitigation                                                           |
| ---------------------------------------- | -------------------------------------------------------------------- |
| `<br>` in table cells mangled            | Spike on `Replacer.md`; tune md033 / mdformat-off comments           |
| YAML front matter altered                | Try `mdformat-frontmatter`; or disable formatting above front matter |
| mdformat moves link refs to bottom       | Accept as new convention, or configure if plugin allows              |
| mermaid fences touched                   | Usually fine as ```` ```mermaid ```` code blocks; verify in spike    |
| PyMarkdown rule parity ≠ markdownlint    | Map rules explicitly; expect minor lint diff                         |
| Windows PATH / Git Bash                  | Use `uv run` or `.venv\Scripts\*.exe` explicitly                     |
| Python not installed on all dev machines | Document uv install; optional CI check                               |

______________________________________________________________________

## Suggested task order (checklist)

- [ ] **Spike** mdformat + pymarkdown on 3 files; sign off on diffs
- [ ] Add `pyproject.toml` / `requirements-md.txt` + `.venv` + gitignore
- [ ] Add `.mdformat.toml` + pymarkdown config
- [ ] Add `scripts/run-md.cmd` (Windows) or use `uv run`
- [ ] Wire `just format-md` / `just lint-md`
- [ ] Run format + lint; tune config
- [ ] Update `CONTRIBUTING.md`, `AGENTS.md`
- [ ] Remove npm files and scripts
- [ ] Final `just format && just lint && just test`

______________________________________________________________________

## Effort estimate

| Phase                           | Time           |
| ------------------------------- | -------------- |
| Spike + config tuning           | 2–4 h          |
| justfile + scripts + docs       | 1–2 h          |
| Review/format diff + lint fixes | 1–2 h          |
| **Total**                       | **~ half day** |

______________________________________________________________________

## Recommendation

Do **Phase 0 spike** before deleting npm. If `Replacer.md` and front-matter docs format cleanly, the Python stack is a good fit and **simpler** than today (one formatter instead of Prettier + table formatter + table check script).

If the spike is messy, **keep npm for now** or use a hybrid: **dprint** for format (single binary, built-in table align) + **pymarkdownlnt** for lint (Python only for lint).
