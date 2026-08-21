using System.Text.Json.Serialization;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Persisted UI session state (window geometry and last File List folder).
    /// <para>Stored in <c>session.json</c> under the same AppData root as <c>mfr.config.json</c>.</para>
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
}
