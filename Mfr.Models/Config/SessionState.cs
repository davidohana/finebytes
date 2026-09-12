using System.Text.Json.Serialization;
using Mfr.Models.RenameList;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Persisted UI session state grouped by owning component.
    /// <para>Stored in <c>session.json</c> under the same AppData root as <c>config.json</c>.</para>
    /// </summary>
    public sealed class SessionState
    {
        /// <summary>
        /// Schema version written with the current session shape.
        /// <para>
        /// Not used as a migrate/reject gate: unknown properties are ignored by the serializer, and missing
        /// properties keep CLR defaults (same as first launch). Values <c>&lt;= 0</c> normalize to <c>1</c> on load/save.
        /// </para>
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Last main-window geometry and pane splitters, when remembered.
        /// </summary>
        [JsonPropertyName("mainWindow")]
        public SessionStateMainWindow? MainWindow { get; set; }

        /// <summary>
        /// Last File List folder, masks, and mask suggestions.
        /// </summary>
        [JsonPropertyName("fileList")]
        public SessionStateFileList? FileList { get; set; }

        /// <summary>
        /// Last Rename List Auto-Sort and related session fields.
        /// </summary>
        [JsonPropertyName("renameList")]
        public SessionStateRenameList? RenameList { get; set; }

        /// <summary>
        /// Filter Configuration chrome (e.g. format-token picker collapse).
        /// </summary>
        [JsonPropertyName("filterEditor")]
        public SessionStateFilterEditor? FilterEditor { get; set; }

        /// <summary>
        /// Returns <see cref="MainWindow"/>, creating it when missing.
        /// </summary>
        /// <returns>The main-window session object.</returns>
        public SessionStateMainWindow EnsureMainWindow()
        {
            return MainWindow ??= new SessionStateMainWindow();
        }

        /// <summary>
        /// Returns <see cref="FileList"/>, creating it when missing.
        /// </summary>
        /// <returns>The File List session object.</returns>
        public SessionStateFileList EnsureFileList()
        {
            return FileList ??= new SessionStateFileList();
        }

        /// <summary>
        /// Returns <see cref="RenameList"/>, creating it when missing.
        /// </summary>
        /// <returns>The Rename List session object.</returns>
        public SessionStateRenameList EnsureRenameList()
        {
            return RenameList ??= new SessionStateRenameList();
        }

        /// <summary>
        /// Returns <see cref="FilterEditor"/>, creating it when missing.
        /// </summary>
        /// <returns>The Filter Configuration session object.</returns>
        public SessionStateFilterEditor EnsureFilterEditor()
        {
            return FilterEditor ??= new SessionStateFilterEditor();
        }
    }

    /// <summary>
    /// Saved Filter Configuration chrome shared across filter option editors.
    /// </summary>
    public sealed class SessionStateFilterEditor
    {
        /// <summary>
        /// When true, the format-token picker catalog is visible; when false, only the Edit/collapse rail.
        /// <para>Missing session section defaults to expanded on first launch.</para>
        /// </summary>
        [JsonPropertyName("formatTokenPickerExpanded")]
        public bool FormatTokenPickerExpanded { get; set; } = true;
    }

    /// <summary>
    /// Saved main-window size, position, state, and pane splitter ratios.
    /// </summary>
    public sealed class SessionStateMainWindow
    {
        /// <summary>
        /// Window left edge in screen pixels (used when <see cref="State"/> is <c>Normal</c>).
        /// </summary>
        [JsonPropertyName("x")]
        public int X { get; set; }

        /// <summary>
        /// Window top edge in screen pixels (used when <see cref="State"/> is <c>Normal</c>).
        /// </summary>
        [JsonPropertyName("y")]
        public int Y { get; set; }

        /// <summary>
        /// Window width in device-independent pixels (used when <see cref="State"/> is <c>Normal</c>).
        /// </summary>
        [JsonPropertyName("width")]
        public double Width { get; set; }

        /// <summary>
        /// Window height in device-independent pixels (used when <see cref="State"/> is <c>Normal</c>).
        /// </summary>
        [JsonPropertyName("height")]
        public double Height { get; set; }

        /// <summary>
        /// <c>Normal</c> or <c>Maximized</c>.
        /// <para>
        /// When maximized, restore skips size/position and keeps the current dimensions as restore bounds.
        /// </para>
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = "Normal";

        /// <summary>
        /// Last main-window pane splitter ratios, when remembered.
        /// </summary>
        [JsonPropertyName("splitters")]
        public SessionStateSplitters? Splitters { get; set; }

        /// <summary>
        /// When true, restore and save main-window size, position, maximized state, and pane splitters across launches.
        /// </summary>
        [JsonPropertyName("rememberWindowState")]
        public bool RememberWindowState { get; set; } = true;
    }

    /// <summary>
    /// Saved File List folder and mask fields.
    /// </summary>
    public sealed class SessionStateFileList
    {
        /// <summary>
        /// Last File List directory path, when remembered.
        /// </summary>
        [JsonPropertyName("lastOpenedDirectory")]
        public string? LastOpenedDirectory { get; set; }

        /// <summary>
        /// When true, restore and save the last File List folder across launches.
        /// </summary>
        [JsonPropertyName("rememberLastFolder")]
        public bool RememberLastFolder { get; set; } = true;

        /// <summary>
        /// Last include mask applied to file names.
        /// </summary>
        [JsonPropertyName("fileMask")]
        public string? FileMask { get; set; }

        /// <summary>
        /// Last exclude masks applied to file names.
        /// </summary>
        [JsonPropertyName("excludeMasks")]
        public List<string>? ExcludeMasks { get; set; }

        /// <summary>
        /// Whether exclude masks are applied when listing and adding files.
        /// </summary>
        [JsonPropertyName("excludeMasksEnabled")]
        public bool? ExcludeMasksEnabled { get; set; }

        /// <summary>
        /// Recently used include masks.
        /// </summary>
        [JsonPropertyName("maskSuggestions")]
        public List<string>? MaskSuggestions { get; set; }

        /// <summary>
        /// Last File List layout mode (<c>largeIcons</c>, <c>smallIcons</c>, <c>report</c>, <c>list</c>,
        /// <c>tiles</c>, <c>thumbnails</c>).
        /// <para>Null or unrecognized means unset (first launch uses Report).</para>
        /// </summary>
        [JsonPropertyName("viewMode")]
        public string? ViewMode { get; set; }
    }

    /// <summary>
    /// Saved Rename List session fields.
    /// </summary>
    public sealed class SessionStateRenameList
    {
        /// <summary>
        /// Last Rename List Auto-Sort keys in priority order. Empty disables Auto-Sort (MFR7).
        /// <para>Null means unset (first launch uses <see cref="RenameListSortKey.DefaultKeys"/>).</para>
        /// </summary>
        [JsonPropertyName("sortFields")]
        public List<RenameListSortKey>? SortFields { get; set; }

        /// <summary>
        /// Last Rename List visible grid columns in left-to-right order.
        /// <para>Null means unset (first launch uses MFR7 defaults).</para>
        /// </summary>
        [JsonPropertyName("visibleColumns")]
        public List<RenameListVisibleColumnSpec>? VisibleColumns { get; set; }

        /// <summary>
        /// Which path kinds become Rename List rows when adding from the File List.
        /// </summary>
        [JsonPropertyName("addMode")]
        public RenameListAddMode AddMode { get; set; } = RenameListAddMode.Files;

        /// <summary>
        /// When true, folder sources recurse: matching files in subfolders, and descendant folder rows when
        /// <see cref="AddMode"/> includes folders.
        /// </summary>
        [JsonPropertyName("addFolderContents")]
        public bool AddFolderContents { get; set; } = true;

        /// <summary>
        /// When true, the Rename List grid uses a fixed-width font instead of the proportional File List font.
        /// </summary>
        [JsonPropertyName("useFixedWidthFont")]
        public bool UseFixedWidthFont { get; set; } = true;

        /// <summary>
        /// When true, filter-chain and membership changes re-run Rename List preview (MFR7 <c>PreviewEnabled</c>).
        /// </summary>
        [JsonPropertyName("previewEnabled")]
        public bool PreviewEnabled { get; set; } = true;
    }

    /// <summary>
    /// Saved main-window pane splitter positions as star ratios of the first pane in each pair.
    /// <para>
    /// Each ratio is firstPane / (firstPane + secondPane) in the range (0, 1). Null means leave XAML defaults.
    /// </para>
    /// </summary>
    public sealed class SessionStateSplitters
    {
        /// <summary>
        /// File List column share of File List + filter panes (horizontal).
        /// </summary>
        [JsonPropertyName("fileList")]
        public double? FileList { get; set; }

        /// <summary>
        /// Available Filters column share of Available + Applied (horizontal).
        /// </summary>
        [JsonPropertyName("availableApplied")]
        public double? AvailableApplied { get; set; }

        /// <summary>
        /// Filter lists row share of filter lists + filter editor (vertical).
        /// </summary>
        [JsonPropertyName("filterLists")]
        public double? FilterLists { get; set; }

        /// <summary>
        /// Top panes row share of top panes + Rename List (vertical).
        /// </summary>
        [JsonPropertyName("topPanes")]
        public double? TopPanes { get; set; }
    }
}
