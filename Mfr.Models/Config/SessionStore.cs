using System.Text.Json;
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
        };

        /// <summary>
        /// Default session file path (<c>%ApplicationData%/finebytes/mfr/session.json</c>).
        /// </summary>
        /// <returns>Absolute path to the default session JSON file.</returns>
        public static string DefaultSessionFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appData.CombinePath("finebytes", "mfr", "session.json");
        }

        /// <summary>
        /// Loads session state from <paramref name="sessionFilePath"/> when present and valid.
        /// </summary>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultSessionFilePath"/> is used.
        /// </param>
        /// <returns>Deserialized state, or a new empty <see cref="SessionState"/> when missing or unreadable.</returns>
        public static SessionState Load(string? sessionFilePath = null)
        {
            var path = sessionFilePath.IsBlank() ? DefaultSessionFilePath() : sessionFilePath.Trim();
            if (!File.Exists(path))
                return new SessionState();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SessionState>(json, s_JsonOptions) ?? new SessionState();
            }
            catch
            {
                return new SessionState();
            }
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

            var path = sessionFilePath.IsBlank() ? DefaultSessionFilePath() : sessionFilePath.Trim();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (state.Version <= 0)
                state.Version = 1;

            var json = JsonSerializer.Serialize(state, s_JsonOptions);
            File.WriteAllText(path, json);
        }
    }
}
