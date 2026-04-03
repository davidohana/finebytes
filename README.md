# finebytes

## Common Developer Tasks

### Prerequisites

- .NET 10 SDK installed (`dotnet --info`)

### Restore Dependencies

Windows (PowerShell):

```powershell
dotnet restore .\mfr8\mfr8.csproj
dotnet restore .\mfr8.Tests\mfr8.Tests.csproj
```

Linux (bash):

```bash
dotnet restore ./mfr8/mfr8.csproj
dotnet restore ./mfr8.Tests/mfr8.Tests.csproj
```

### Compile (Build)

Windows (PowerShell):

```powershell
dotnet build .\mfr8\mfr8.csproj -c Debug
dotnet build .\mfr8.Tests\mfr8.Tests.csproj -c Debug
```

Linux (bash):

```bash
dotnet build ./mfr8/mfr8.csproj -c Debug
dotnet build ./mfr8.Tests/mfr8.Tests.csproj -c Debug
```

### Run Unit Tests

Windows (PowerShell):

```powershell
dotnet test .\mfr8.Tests\mfr8.Tests.csproj -c Debug
```

Linux (bash):

```bash
dotnet test ./mfr8.Tests/mfr8.Tests.csproj -c Debug
```

### Run the CLI App

Show usage/help (Windows PowerShell):

```powershell
dotnet run --project .\mfr8\mfr8.csproj -- --help
```

Show usage/help (Linux bash):

```bash
dotnet run --project ./mfr8/mfr8.csproj -- --help
```

Example run (Windows PowerShell):

```powershell
dotnet run --project .\mfr8\mfr8.csproj -- "C:\Music\*.mp3" -p "my-preset"
```

Example run (Linux bash):

```bash
dotnet run --project ./mfr8/mfr8.csproj -- "/home/user/music/*.mp3" -p "my-preset"
```

### Presets File

- Presets are loaded from a single JSON file.
- Default path on Windows: `%APPDATA%\MagicFileRenamer\presets.json`
- Override path with: `-d` / `--presets-file`
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
