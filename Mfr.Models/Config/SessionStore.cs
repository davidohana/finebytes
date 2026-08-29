using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Utils;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Loads and saves <see cref="SessionState"/> from <c>session.json</c>.
    /// <para>Missing or corrupt files yield an empty session; load never throws to the caller.</para>
    /// </summary>
    public static class SessionStore
    {
        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        /// <summary>
        /// Gets the in-memory session for this process, set by <see cref="Load"/> when using the default path.
        /// </summary>
        public static SessionState Current { get; private set; } = new();

        /// <summary>
        /// Default session file path (<see cref="AppDataPaths.RoamingRoot"/> + <c>session.json</c>).
        /// </summary>
        /// <returns>Absolute path to the default session JSON file.</returns>
        public static string DefaultSessionFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("session.json");
        }

        /// <summary>
        /// Loads session state from <paramref name="sessionFilePath"/> when present and valid.
        /// <para>When <paramref name="sessionFilePath"/> is omitted, also assigns <see cref="Current"/>.</para>
        /// </summary>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultSessionFilePath"/> is used.
        /// </param>
        /// <returns>Deserialized state, or a new empty <see cref="SessionState"/> when missing or unreadable.</returns>
        public static SessionState Load(string? sessionFilePath = null)
        {
            var useDefaultPath = sessionFilePath.IsBlank();
            var state = _Read(_ResolvePath(sessionFilePath));
            if (useDefaultPath)
            {
                Current = state;
            }

            return state;
        }

        /// <summary>
        /// Writes <paramref name="state"/> to <paramref name="sessionFilePath"/>, creating the directory when needed.
        /// </summary>
        /// <param name="state">Session values to persist.</param>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultSessionFilePath"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public static void Save(SessionState state, string? sessionFilePath = null)
        {
            ArgumentNullException.ThrowIfNull(state);

            var path = _ResolvePath(sessionFilePath);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (state.Version <= 0)
            {
                state.Version = 1;
            }

            state.Ui ??= new SessionStateUi();

            var json = JsonSerializer.Serialize(state, s_JsonOptions);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Writes <see cref="Current"/>.Ui into the session file, preserving other sections already on disk.
        /// <para>Failures are swallowed so UI preference saves do not crash the app.</para>
        /// </summary>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultSessionFilePath"/> is used.
        /// </param>
        public static void SaveCurrentUi(string? sessionFilePath = null)
        {
            try
            {
                var path = _ResolvePath(sessionFilePath);
                var session = File.Exists(path) ? _Read(path) : new SessionState();
                session.Ui = Current.Ui;
                Save(session, path);
            }
            catch
            {
                // Preference save must not block the UI or surface to the user.
            }
        }

        /// <summary>
        /// Resolves <paramref name="sessionFilePath"/> to the default session path when omitted.
        /// </summary>
        /// <param name="sessionFilePath">Explicit path, or blank for the default AppData file.</param>
        /// <returns>Absolute path to the session JSON file.</returns>
        private static string _ResolvePath(string? sessionFilePath)
        {
            return sessionFilePath.IsBlank() ? DefaultSessionFilePath() : sessionFilePath.Trim();
        }

        /// <summary>
        /// Reads session JSON, or an empty session when missing or unreadable.
        /// </summary>
        /// <param name="path">Absolute path to the session file.</param>
        /// <returns>Deserialized state, or a new empty session.</returns>
        private static SessionState _Read(string path)
        {
            if (!File.Exists(path))
            {
                return new SessionState();
            }

            try
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<SessionState>(json, s_JsonOptions) ?? new SessionState();
                state.Ui ??= new SessionStateUi();
                return state;
            }
            catch
            {
                return new SessionState();
            }
        }
    }
}
