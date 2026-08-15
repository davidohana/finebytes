# App UI

Avalonia 11 desktop entry point (`Mfr.App.Ui`) for Magic File Renamer.

From the repository root:

```powershell
just run-ui
```

Or:

```powershell
dotnet run --project .\Mfr.App.Ui\Mfr.App.Ui.csproj
```

The main window is the MFR 7.4 splitter shell (File Explorer, Available/Applied Filters, Filter Configuration, Rename List). Pane contents are filled in later GUI phases. Keep UI-specific code out of Engine and Filters.
