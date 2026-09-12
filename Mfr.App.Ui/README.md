# App UI

Avalonia 12 desktop entry point (`Mfr.App.Ui`) for Magic File Renamer.

From the repository root:

```powershell
just run-ui
```

Or:

```powershell
dotnet run --project .\Mfr.App.Ui\Mfr.App.Ui.csproj
```

The main window is the MFR 7.4 splitter shell. The tall left **File List** lists the current folder (path combo, back/forward/up/refresh, Mask, exclude). **File List → File List Type** switches among Large Icons, Small Icons, Report (default Name / Date modified / Type / Size grid), List, Tiles, and Thumbnails. Other panes are filled in later GUI phases. Keep UI-specific code out of Engine and Filters.

First launch writes `%AppData%/finebytes/mfr/config.json` (filter and log settings) if it is missing, so those values can be edited by hand. UI layout lives in `session.json` in the same folder. Per-filter add defaults live in `filter-defaults.json`. **Tools → Reset Configuration** deletes those three files, skips writing session on close, and restarts the app (named `presets.json` and local logs are kept).
