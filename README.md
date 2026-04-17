# finebytes

## Quick Start

### Prerequisites

- .NET 10 SDK installed (`dotnet --info`)

### Task Runner (`just`)

Install `just`:

- Follow the official install options: <https://github.com/casey/just#installation>
- Quick cross-platform option (if Rust/Cargo is installed): `cargo install just`

After install, you can run common tasks with short commands from the repository root:

```powershell
just restore
just build
just test
just format
just run-help
```

Run `just` by itself to list available targets.

For contributor workflow, formatting/linting expectations, and pre-commit checks, see `CONTRIBUTING.md`.

### Run the CLI App

Show usage/help (Windows PowerShell):

```powershell
dotnet run --project .\Mfr\Mfr.csproj -- --help
```

Show usage/help (Linux bash):

```bash
dotnet run --project ./Mfr/Mfr.csproj -- --help
```

Example run (Windows PowerShell):

```powershell
dotnet run --project .\Mfr\Mfr.csproj -- "C:\Music\*.mp3" -p "my-preset"
```

Example run (Linux bash):

```bash
dotnet run --project ./Mfr/Mfr.csproj -- "/home/user/music/*.mp3" -p "my-preset"
```

### Presets File

- Presets are loaded from a single JSON file.
- Default path on Windows: `%APPDATA%\MagicFileRenamer\presets.json`
- Override path with: `--presets-file` (long option only)
- Preset names must be unique (case-insensitive).

Minimal shape:

```json
{
  "presets": [
    {
      "id": "3f40e2ab-2d9f-4ea4-b597-4ef8c715d2c1",
      "name": "my-preset",
      "description": "optional",
      "filters": []
    }
  ]
}
```
