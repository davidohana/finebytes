# Contributing

## Task runner (`just`)

Install instructions: <https://github.com/casey/just#installation>

If you have `just` installed, you can use shortcuts for the most common workflows:

- `just restore`
- `just build`
- `just test`
- `just format`
- `just lint`
- `just format-md`
- `just lint-md`
- `just run-help`
- `just run-ui`

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (see global.json if present)
- [just](https://github.com/casey/just#installation) (optional task runner)
- [Python 3.12+](https://www.python.org/downloads/) (dev-only, for markdown format/lint)

## C# formatting and linting

- Layout: [CSharpier](https://csharpier.com/) (local tool; options in `.csharpierrc.json`, print width 120)
- Formats `.cs`, `.csproj`, `.axaml`, and `.slnx` (see `.csharpierignore`)
- Style / analyzers: `dotnet format style` and `dotnet format analyzers` (no whitespace — that conflicts with CSharpier)
- Linting: `just lint-cs` (`dotnet build`, `csharpier check`, `dotnet format … --verify-no-changes`)

Preferred shortcuts:

```powershell
just format
just lint
```

Equivalent without `just`:

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet format style .\finebytes.slnx
dotnet format analyzers .\finebytes.slnx

dotnet build .\finebytes.slnx
dotnet csharpier check .
dotnet format style .\finebytes.slnx --verify-no-changes
dotnet format analyzers .\finebytes.slnx --verify-no-changes
```

Linux/macOS format:

```bash
dotnet tool restore
dotnet csharpier format .
dotnet format style ./finebytes.slnx
dotnet format analyzers ./finebytes.slnx
```

Linux/macOS lint:

```bash
dotnet tool restore
dotnet build ./finebytes.slnx
dotnet csharpier check .
dotnet format style ./finebytes.slnx --verify-no-changes
dotnet format analyzers ./finebytes.slnx --verify-no-changes
```

CSharpier formats `.cs`, `.csproj`, `.axaml`, and `.slnx` (see `.csharpierignore`). Code style and diagnostic severities live in `.editorconfig`.

## Markdown formatting and linting

One-time (dev machine):

```powershell
python -m venv .venv
.\.venv\Scripts\pip install -r requirements-md.txt
```

```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements-md.txt
```

Then:

```powershell
just format
just lint
```

- Layout: [mdformat](https://github.com/hukkin/mdformat) with [mdformat-gfm](https://github.com/hukkin/mdformat-gfm) and [mdformat-frontmatter](https://pypi.org/project/mdformat-frontmatter/) (`.mdformat.toml`)
- Lint: [pymarkdownlnt](https://github.com/jackdewinter/pymarkdown) (`pymarkdown.toml`); `mdformat --check` (includes table alignment)
- File set: tracked + untracked `.md` via `git ls-files` in the `justfile`; skips `.cursor/**` and gitignored paths
- Shortcuts: `just format-md` / `just lint-md` (also run from `just format` / `just lint`)
