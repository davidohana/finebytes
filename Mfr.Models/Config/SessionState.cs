using System.Text.Json.Serialization;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Persisted UI session state (window geometry, pane splitters, and last File List folder).
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
        /// Last File List directory path, when remembered.
        /// </summary>
        [JsonPropertyName("lastOpenedDirectory")]
        public string? LastOpenedDirectory { get; set; }

        /// <summary>
        /// Last main-window geometry, when remembered.
        /// </summary>
        [JsonPropertyName("window")]
        public SessionWindowState? Window { get; set; }

        /// <summary>
        /// Last main-window pane splitter ratios, when remembered.
        /// </summary>
        [JsonPropertyName("splitters")]
        public SessionSplitterState? Splitters { get; set; }

        /// <summary>
        /// Last include mask applied to file names.
        /// </summary>
        [JsonPropertyName("fileMask")]
        public string? FileMask { get; set; }

        /// <summary>
        /// Last exclude mask applied to file names.
        /// </summary>
        [JsonPropertyName("excludeMasks")]
        public string? ExcludeMasks { get; set; }

        /// <summary>
        /// Recently used include masks.
        /// </summary>
        [JsonPropertyName("maskSuggestions")]
        public List<string>? MaskSuggestions { get; set; }
    }

    /// <summary>
    /// Saved main-window size, position, and state.
    /// </summary>
    public sealed class SessionWindowState
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
        /// When maximized, restore skips size/position and keeps the window's default dimensions.
        /// </para>
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = "Normal";
    }

    /// <summary>
    /// Saved main-window pane splitter positions as star ratios of the first pane in each pair.
    /// <para>
    /// Each ratio is firstPane / (firstPane + secondPane) in the range (0, 1). Null means leave XAML defaults.
    /// </para>
    /// </summary>
    public sealed class SessionSplitterState
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
