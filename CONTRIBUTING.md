# Contributing

## Task runner (`just`)

Install instructions: <https://github.com/casey/just#installation>

If you have `just` installed, you can use shortcuts for the most common workflows:

- `just restore`
- `just build`
- `just test`
- `just format`
- `just run-help`
- `just run-ui`

## C# formatting and linting

- Layout: [CSharpier](https://csharpier.com/) (local tool; options in `.csharpierrc.json`, print width 120)
- Formats `.cs`, `.csproj`, `.axaml`, and `.slnx` (see `.csharpierignore`)
- Style / analyzers: `dotnet format style` and `dotnet format analyzers` (no whitespace — that conflicts with CSharpier)
- Linting: `dotnet build` (analyzers and code style enforcement)

Preferred shortcut:

```powershell
just format
dotnet build .\finebytes.slnx
```

Equivalent without `just`:

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet format style .\finebytes.slnx
dotnet format analyzers .\finebytes.slnx
dotnet build .\finebytes.slnx
```

Linux/macOS:

```bash
dotnet tool restore
dotnet csharpier format .
dotnet format style ./finebytes.slnx
dotnet format analyzers ./finebytes.slnx
dotnet build ./finebytes.slnx
```

CSharpier formats `.cs`, `.csproj`, `.axaml`, and `.slnx` (see `.csharpierignore`). Code style and diagnostic severities live in `.editorconfig`.
