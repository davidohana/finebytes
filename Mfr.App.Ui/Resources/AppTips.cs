using Mfr.App.Ui.Input;

namespace Mfr.App.Ui.Resources
{
    /// <summary>
    /// Toolbar, menu, and control tooltip text.
    /// <para>
    /// Shortcut gestures live in <see cref="AppShortcuts"/>; keep gesture text in these tips
    /// aligned with that type and <c>docs/keyboard-shortcuts.md</c>.
    /// </para>
    /// </summary>
    public static class AppTips
    {
        /// <summary>Toolbar and menu tip for GO.</summary>
        public const string Go = "GO (Ctrl+G)";

        /// <summary>Toolbar tip for Undo Last.</summary>
        public const string UndoLast = "Undo last (Ctrl+Z)";

        /// <summary>Toolbar tip for the Rename Log window.</summary>
        public const string ShowLog = "Rename Log (Ctrl+Shift+L)";

        /// <summary>Rename Log dialog: Prepare Undo for the selected log.</summary>
        public const string RenameLogPrepareUndo =
            "Prepare an undo session from this log — replaces the Rename List, clears Filter Chain; press GO to apply";

        /// <summary>Rename Log dialog: Erase the selected log.</summary>
        public const string RenameLogErase = "Erase this log from history (deletes the on-disk file when present)";

        /// <summary>Toolbar tip for Options.</summary>
        public const string ShowOptions = "Options (Ctrl+,)";

        /// <summary>Options: restore File List last folder across launches.</summary>
        public const string OptionsRememberLastFolder =
            "Open the last File List folder again the next time you start the app";

        /// <summary>Options: remember main-window and dialog layout across launches.</summary>
        public const string OptionsRememberWindowState =
            "Restore main window and dialog sizes, positions, and pane splitters next launch";

        /// <summary>Options: Confirmations section — how suppressions work.</summary>
        public const string OptionsConfirmations =
            "Uncheck Keep showing… on a confirmation to hide it next time; reset below restores all";

        /// <summary>Options: Reset confirmations button.</summary>
        public const string OptionsResetConfirmations =
            "Show all confirmation dialogs again (applies when you OK Options)";

        /// <summary>Options: double-click opens in File List.</summary>
        public const string OptionsDoubleClickOpen =
            "Double-click opens folders or launches files with the default application";

        /// <summary>Options: double-click adds to Rename List.</summary>
        public const string OptionsDoubleClickAdd =
            "Double-click adds the File List selection to the Rename List (same as Add Selected)";

        /// <summary>Options: Add to Rename List — Files.</summary>
        public const string OptionsAddModeFiles =
            "Add file rows only (selected files, and files found under selected folders)";

        /// <summary>Options: Add to Rename List — Folders.</summary>
        public const string OptionsAddModeFolders = "Add folder rows only";

        /// <summary>Options: Add to Rename List — Files and folders.</summary>
        public const string OptionsAddModeFilesAndFolders = "Add both file and folder rows";

        /// <summary>Options: recurse into folder sources when adding.</summary>
        public const string OptionsAddFolderContents =
            "When adding a folder, also include matching items from its subfolders recursively";

        /// <summary>Options: remember Rename List column widths across hide/re-add and launches.</summary>
        public const string OptionsRememberColumnWidths =
            "Reuse the last resized width when a Rename List column is shown again";

        /// <summary>Options: rename-log retention — Disabled.</summary>
        public const string OptionsRenameLogDisabled =
            "Do not save rename logs to disk; Undo Last still works for the in-memory last operation";

        /// <summary>Options: rename-log retention — Limited.</summary>
        public const string OptionsRenameLogLimited =
            "Keep only the newest N on-disk rename logs (default 10); older files are deleted";

        /// <summary>Options: rename-log retention — Unlimited.</summary>
        public const string OptionsRenameLogUnlimited =
            "Keep every on-disk rename log until you erase them in the Rename Log window";

        /// <summary>Tools → Reset Configuration menu tip (MFR7 ResetConfiguration).</summary>
        public const string ResetConfiguration = "Reset configuration to default values";

        /// <summary>Rename List Add Selected button and menu tip.</summary>
        public const string AddSelected = "Add selected (Ctrl+Shift+S)";

        /// <summary>Rename List Add All button and menu tip.</summary>
        public const string AddAll = "Add all (Ctrl+Shift+A)";

        /// <summary>Rename List Remove Selected button and menu tip.</summary>
        public const string RemoveSelected = "Remove selected (Ctrl+Shift+R)";

        /// <summary>Rename List Remove All But Selected menu tip.</summary>
        public const string RemoveAllButSelected = "Remove all but selected (Ctrl+Shift+B)";

        /// <summary>Rename List Clear button and menu tip.</summary>
        public const string ClearRenameList = "Clear Rename List (Ctrl+Shift+C)";

        /// <summary>Rename List Locate in File List menu tip.</summary>
        public const string LocateInFileList = "Locate in File List (F4)";

        /// <summary>Rename List Manual Override Field menu tip.</summary>
        public const string ManualOverrideField =
            "Override the focused field in the Rename List (F2). Files update on GO.";

        /// <summary>Rename List column header: clear overrides for this column on every row.</summary>
        public const string CancelManualOverrideColumn =
            "Clear manual overrides for this column on all Rename List rows";

        /// <summary>Rename List Move Selected Up button and menu tip.</summary>
        public const string MoveSelectedUp = "Move selected up (Ctrl+Up)";

        /// <summary>Rename List Move Selected Down button and menu tip.</summary>
        public const string MoveSelectedDown = "Move selected down (Ctrl+Down)";

        /// <summary>Rename List header: drop rows whose preview for this column is unchanged.</summary>
        public const string RemoveUnchangedItems = "Remove items whose preview for this field is unchanged";

        /// <summary>Rename List header: Edit as Name List (writable columns only).</summary>
        public const string EditAsNameList = "Create a Name List filter to edit the names in this column";

        /// <summary>Rename List header: Export This Column (text file).</summary>
        public const string ExportThisColumn = "Export this column's values to a text file";

        /// <summary>Rename List header / main menu: Export Visible Columns (CSV).</summary>
        public const string ExportVisibleColumns = "Export all visible columns to a CSV file";

        /// <summary>Rename List field shuttle (toolbar, context menu, main menu).</summary>
        public const string SelectRenameListFields = "Select Rename List columns and Auto-Sort fields";

        /// <summary>Rename List Add columns from filters (toolbar / shuttle / menu).</summary>
        public const string AddColumnsFromFilters = "Add Rename List columns for fields used by the Filter Chain";

        /// <summary>Rename List Set columns from filters (toolbar / shuttle / menu).</summary>
        public const string SetColumnsFromFilters =
            "Set Rename List columns to defaults plus fields used by the Filter Chain";

        /// <summary>Rename List field shuttle: Auto-Sort fields (main menu).</summary>
        public const string EditRenameListSortFields = "Choose Auto-Sort fields and order";

        /// <summary>Rename List Auto-Preview toolbar and menu tip (MFR7 AutoPreview).</summary>
        public const string AutoPreview = "Auto-Preview — click to turn on or off";

        /// <summary>Rename List Before/After Mode tip (shuttle checkbox and Rename List menu).</summary>
        public const string BeforeAfterMode =
            "Use a Before/After toggle for original vs preview values instead of side-by-side columns";

        /// <summary>Rename List Before/After side tip (toolbar toggle).</summary>
        public const string BeforeAfterSide =
            "Show original (Before) or preview (After) values for the same fields (Ctrl+[ / Ctrl+])";

        /// <summary>Rename List menu: switch Before/After Mode to original values.</summary>
        public const string ToggleBefore = "Toggle Before — show original values (Ctrl+[)";

        /// <summary>Rename List menu: switch Before/After Mode to preview values.</summary>
        public const string ToggleAfter = "Toggle After — show preview values (Ctrl+])";

        /// <summary>Rename List color legend toolbar tip (MFR7 btnLegendEnabled).</summary>
        public const string ColorLegend = "Show or hide the Rename List color legend";

        /// <summary>Rename List color legend panel tip (MFR7 ColorLegend).</summary>
        public const string ColorLegendPanel = "Explains Rename List cell and row colors";

        /// <summary>Color legend: Original Value swatch.</summary>
        public const string LegendOriginalValue = "Unchanged original or preview value";

        /// <summary>Color legend: Value Changed swatch (red text).</summary>
        public const string LegendValueChanged = "Preview value differs from original — will change when you GO";

        /// <summary>Color legend: Manual Override swatch (blue text).</summary>
        public const string LegendManualOverride = "Value was set with Manual Override (F2) — used on GO";

        /// <summary>Color legend: Value Error swatch (gray italic em dash).</summary>
        public const string LegendValueError = "Gray italic — means load failed or the item is missing on disk";

        /// <summary>Color legend: Preview Error swatch (lavender row).</summary>
        public const string LegendPreviewError = "Preview failed for this row — ignored on GO; right-click for details";

        /// <summary>Color legend: Rename Error swatch (plum row).</summary>
        public const string LegendRenameError =
            "Last GO failed for this row — right-click Show rename error; F5 clears";

        /// <summary>Rename List status glyph when the highest-priority issue is load/missing.</summary>
        public const string RenameListLoadErrorGlyph =
            "This row has a load error or is missing on disk. Right-click and choose Show Error Details.";

        /// <summary>Rename List status glyph when the highest-priority issue is a preview failure.</summary>
        public const string RenameListPreviewErrorGlyph =
            "This row has a preview error. Right-click and choose Show preview error.";

        /// <summary>Rename List status glyph when the highest-priority issue is a rename/commit failure.</summary>
        public const string RenameListCommitErrorGlyph =
            "This row has a rename error. Right-click and choose Show rename error.";

        /// <summary>Rename List status column header tip.</summary>
        public const string RenameListRowErrorColumn = "Error column — marks rows with load, preview, or rename errors";

        /// <summary>File List refresh button tip.</summary>
        public const string Refresh = "Refresh (F5)";

        /// <summary>Address bar tip for focusing the typed path.</summary>
        public const string GoToAddress = "Go to folder (Ctrl+L or Alt+D)";

        /// <summary>Up button tip. Backspace is File List–focused, not a window hotkey.</summary>
        public const string GoUp = "Up (Backspace)";

        /// <summary>Typed-path box tip.</summary>
        public const string AddressEdit = "Enter to go, Esc to cancel";

        /// <summary>Filter Chain add-from-palette shuttle tip.</summary>
        public const string AddSelectedFilter = "Add selected filter to Filter Chain";

        /// <summary>Filter Chain remove-selected shuttle tip.</summary>
        public const string RemoveSelectedFilter = "Remove selected filter from Filter Chain (Del)";

        /// <summary>Filter Chain clear-all shuttle tip.</summary>
        public const string RemoveAllFilters = "Clear Filter Chain";

        /// <summary>Filter Chain move-up shuttle tip.</summary>
        public const string MoveFilterUp = "Move selected filter up in Filter Chain (Ctrl+Up) — applies sooner";

        /// <summary>Filter Chain move-down shuttle tip.</summary>
        public const string MoveFilterDown = "Move selected filter down in Filter Chain (Ctrl+Down) — applies later";

        /// <summary>Filter Chain Filter Options shuttle tip.</summary>
        public const string FilterOptions =
            "Filter Options — name, Apply To target, and where the filter runs (whole / substring / token)";

        /// <summary>Filter Chain / Filters menu: open Preset Manager (MFR7 PresetManager).</summary>
        public const string Presets = "Show Preset Manager window";

        /// <summary>Filter Chain / Filters menu: Save Preset dialog (upsert by name).</summary>
        public const string SavePreset = "Save Filter Chain as a preset";

        /// <summary>Filter Chain toolbar ▾ quick-pick tip.</summary>
        public const string PresetsQuickPick = "Load a preset";

        /// <summary>Filter Configuration title-bar reset tip (MFR7 Reset settings for this filter).</summary>
        public const string ResetFilterDefaults = "Reset settings for this filter";

        /// <summary>Filter Configuration title-bar save-as-default tip (MFR7 save current settings as default).</summary>
        public const string SaveFilterAsDefault = "Save current settings for this filter as default";

        /// <summary>Filter Configuration title-bar help tip (MFR7 Show help for this filter).</summary>
        public const string ShowFilterHelp = "Show help for this filter";
    }
}
