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
- [Node.js LTS](https://nodejs.org/) (includes npm; dev-only, for markdown format/lint)

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

- Layout: [Prettier](https://prettier.io/) (`.prettierrc.json`, `.prettierignore`); [markdown-table-formatter](https://github.com/nvuillam/markdown-table-formatter) aligns table columns after Prettier
- Lint: [markdownlint-cli2](https://github.com/DavidAnson/markdownlint-cli2) (`.markdownlint-cli2.jsonc`); Prettier `--check`; table alignment check
- Formats project `.md` under `docs/`, `Mfr.Filters/docs/`, and repo root docs; skips `.cursor/**`

Preferred shortcuts:

```powershell
just format-md
just lint-md
```

`just format` runs C# formatters and then `just format-md`. `just lint` runs `just lint-cs` and then `just lint-md`.

Equivalent without `just`:

```powershell
npm ci
npm run format:md

npm ci
npm run lint:md
```
