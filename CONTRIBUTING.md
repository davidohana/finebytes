# Contributing

## Task runner (`just`)

Install instructions: <https://github.com/casey/just#installation>

If you have `just` installed, you can use shortcuts for the most common workflows:

- `just restore`
- `just build`
- `just test`
- `just format`
- `just run-help`

## C# formatting and linting

This repository uses built-in .NET formatting and analyzers:

- Formatting: `dotnet format`
- Linting: `dotnet build` (with analyzers and code style enforcement enabled)

Run these commands from the repository root before committing:

```powershell
dotnet format .\finebytes.slnx
dotnet build .\finebytes.slnx
```

If you prefer a single command in PowerShell:

```powershell
dotnet format .\finebytes.slnx; if ($LASTEXITCODE -eq 0) { dotnet build .\finebytes.slnx }
```

Linux/macOS usage:

```bash
dotnet format ./finebytes.slnx
dotnet build ./finebytes.slnx
```

Single command in bash:

```bash
dotnet format ./finebytes.slnx && dotnet build ./finebytes.slnx
```

Code style and diagnostic severities are configured in `.editorconfig`. Update that file when project conventions change.
