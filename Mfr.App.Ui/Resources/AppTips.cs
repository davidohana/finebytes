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

        /// <summary>Toolbar tip for the log window.</summary>
        public const string ShowLog = "Log (Ctrl+Shift+L)";

        /// <summary>Toolbar tip for Options.</summary>
        public const string ShowOptions = "Options (Ctrl+,)";

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
            "Override the focused field in the Rename List (F2). Files update on Go.";

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

        /// <summary>Rename List field shuttle: visible columns (toolbar, context menu, main menu).</summary>
        public const string SelectRenameListFields = "Choose visible Rename List columns";

        /// <summary>Rename List field shuttle: Auto-Sort fields (context and main menus).</summary>
        public const string EditRenameListSortFields = "Choose Auto-Sort fields and order";

        /// <summary>Rename List Auto-Preview toolbar and menu tip (MFR7 AutoPreview).</summary>
        public const string AutoPreview = "Auto-Preview. Push or unpush to change status.";

        /// <summary>Rename List row-error badge in the status column.</summary>
        public const string RenameListRowErrorGlyph =
            "This row has an error. Right-click and choose Show Error Details.";

        /// <summary>File List refresh button tip.</summary>
        public const string Refresh = "Refresh (F5)";

        /// <summary>Address bar tip for focusing the typed path.</summary>
        public const string GoToAddress = "Go to folder (Ctrl+L)";

        /// <summary>Up button tip. Backspace is File List–focused, not a window hotkey.</summary>
        public const string GoUp = "Up (Backspace)";

        /// <summary>Typed-path box tip.</summary>
        public const string AddressEdit = "Enter to go, Esc to cancel";

        /// <summary>Applied Filters add-from-palette shuttle tip.</summary>
        public const string AddSelectedFilter = "Add selected filter to the Filter Apply List";

        /// <summary>Applied Filters remove-selected shuttle tip.</summary>
        public const string RemoveSelectedFilter = "Remove selected filter from the Filter Apply List";

        /// <summary>Applied Filters clear-all shuttle tip.</summary>
        public const string RemoveAllFilters = "Clear Filter Apply List";

        /// <summary>Applied Filters move-up shuttle tip.</summary>
        public const string MoveFilterUp = "Move selected filter up in the apply list (to apply it sooner)";

        /// <summary>Applied Filters move-down shuttle tip.</summary>
        public const string MoveFilterDown = "Move selected filter down in the apply list (to apply it later)";

        /// <summary>Applied Filters Filter Options shuttle tip.</summary>
        public const string FilterOptions = "Filter Options — Edit filter name and Apply To options";

        /// <summary>Applied Filters / Filters menu: open Preset Manager (MFR7 PresetManager).</summary>
        public const string Presets = "Show Preset Manager window";

        /// <summary>Applied Filters / Filters menu: Save Preset dialog (upsert by name).</summary>
        public const string SavePreset = "Save applied filters as a preset";

        /// <summary>Applied Filters toolbar ▾ quick-pick tip.</summary>
        public const string PresetsQuickPick = "Load a preset";

        /// <summary>Filter Configuration title-bar reset tip (MFR7 Reset settings for this filter).</summary>
        public const string ResetFilterDefaults = "Reset settings for this filter";

        /// <summary>Filter Configuration title-bar save-as-default tip (MFR7 save current settings as default).</summary>
        public const string SaveFilterAsDefault = "Save current settings for this filter as default";

        /// <summary>Filter Configuration title-bar help tip (MFR7 Show help for this filter).</summary>
        public const string ShowFilterHelp = "Show help for this filter";
    }
}
