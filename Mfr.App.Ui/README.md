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

The main window is the MFR 7.4 splitter shell. The tall left **File List** lists the current folder (path combo, back/forward/up/refresh, Mask, exclude). **File List → File List Type** switches among Large Icons, Small Icons, Report (default Name / Date modified / Type / Size grid), List, Tiles, and Thumbnails. Other panes are filled in later GUI phases. Keep UI-specific code out of Engine and Filters.
