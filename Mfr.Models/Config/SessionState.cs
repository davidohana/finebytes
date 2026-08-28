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
        /// Schema version for forward-compatible migrations.
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
        public List<SessionStateRenameListSortField>? SortFields { get; set; }

        /// <summary>
        /// Last Rename List visible grid columns in left-to-right order.
        /// <para>Null means unset (first launch uses MFR7 defaults).</para>
        /// </summary>
        [JsonPropertyName("visibleColumns")]
        public List<SessionStateRenameListColumn>? VisibleColumns { get; set; }

        /// <summary>
        /// Converts persisted session fields into sort keys.
        /// </summary>
        /// <param name="fields">Session fields in priority order.</param>
        /// <returns>Sort keys; empty when Auto-Sort is off.</returns>
        public static IReadOnlyList<RenameListSortKey> ToSortKeys(IReadOnlyList<SessionStateRenameListSortField> fields)
        {
            ArgumentNullException.ThrowIfNull(fields);
            if (fields.Count == 0)
            {
                return [];
            }

            return [.. fields.Select(field => new RenameListSortKey(field.Column, field.Descending))];
        }

        /// <summary>
        /// Converts sort keys into persisted session fields.
        /// </summary>
        /// <param name="keys">Sort keys in priority order.</param>
        /// <returns>Session fields; empty when Auto-Sort is off.</returns>
        public static List<SessionStateRenameListSortField> FromSortKeys(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            return [.. keys.Select(key => new SessionStateRenameListSortField(key.Column, key.Descending))];
        }
    }

    /// <summary>
    /// One persisted Rename List Auto-Sort key: column plus sort direction.
    /// </summary>
    /// <param name="Column">Column to compare.</param>
    /// <param name="Descending">When <see langword="true"/>, sort that column descending.</param>
    public sealed record SessionStateRenameListSortField(
        [property: JsonPropertyName("column")] RenameListSortColumn Column,
        [property: JsonPropertyName("descending")] bool Descending = false
    );

    /// <summary>
    /// One persisted Rename List visible grid column: field identity plus optional width override.
    /// </summary>
    /// <param name="Key">Field key (original or preview).</param>
    /// <param name="Width">
    /// Column width in pixels, or <see langword="null"/> to use catalog/header defaults on restore.
    /// </param>
    public sealed record SessionStateRenameListColumn(
        [property: JsonPropertyName("key")] RenameListFieldKey Key,
        [property: JsonPropertyName("width")] int? Width = null
    );

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
