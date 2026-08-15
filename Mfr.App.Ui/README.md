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

The main window is the MFR 7.4 splitter shell. The tall left **File Explorer** lists the current folder (path combo, back/forward/up/refresh, Name grid, Mask, exclude). Other panes are filled in later GUI phases. Keep UI-specific code out of Engine and Filters.
