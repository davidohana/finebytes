# Contributing

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
