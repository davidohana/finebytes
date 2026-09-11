using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Utils;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Loads and saves <see cref="SessionState"/> from <c>session.json</c>.
    /// <para>Missing or corrupt files yield an empty session; load never throws to the caller.</para>
    /// </summary>
    /// <remarks>
    /// Soft-load dialect: corrupt or missing → empty <see cref="SessionState"/>; the app continues.
    /// Same policy as <c>FilterDefaultsStore</c> (Engine); opposite of hard-fail
    /// <see cref="ConfigStore"/> and <c>PresetManager</c> (Engine).
    /// Do not unify these modes — the split is intentional product dialect.
    /// <para>
    /// When persisting <see cref="SessionState.AppliedFilters"/>, pass Engine <c>SessionJsonOptions.Default</c>
    /// so <c>BaseFilter</c> polymorphism and per-step soft-skip apply. Default options alone cannot round-trip
    /// the working chain.
    /// </para>
    /// </remarks>
    public static class SessionStore
    {
        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        /// <summary>
        /// Default session file path (<see cref="AppDataPaths.RoamingRoot"/> + <c>session.json</c>).
        /// </summary>
        /// <returns>Absolute path to the default session JSON file.</returns>
        public static string DefaultFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("session.json");
        }

        /// <summary>
        /// Default session file path (<see cref="DefaultFilePath"/>).
        /// </summary>
        /// <returns>Absolute path to the default session JSON file.</returns>
        private static string _DefaultSessionFilePath()
        {
            return DefaultFilePath();
        }

        /// <summary>
        /// Loads session state from <paramref name="sessionFilePath"/> when present and valid.
        /// </summary>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultSessionFilePath"/> is used.
        /// </param>
        /// <param name="options">
        /// Serializer options. When null, uses built-in session options (no filter polymorphism).
        /// Pass Engine <c>SessionJsonOptions.Default</c> when the file may contain
        /// <see cref="SessionState.AppliedFilters"/>.
        /// </param>
        /// <returns>Deserialized state, or a new empty <see cref="SessionState"/> when missing or unreadable.</returns>
        public static SessionState Load(string? sessionFilePath = null, JsonSerializerOptions? options = null)
        {
            return _Read(_ResolvePath(sessionFilePath), options ?? s_JsonOptions);
        }

        /// <summary>
        /// Writes <paramref name="state"/> to <paramref name="sessionFilePath"/>, creating the directory when needed.
        /// </summary>
        /// <param name="state">Session values to persist.</param>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultSessionFilePath"/> is used.
        /// </param>
        /// <param name="options">
        /// Serializer options. When null, uses built-in session options (no filter polymorphism).
        /// Pass Engine <c>SessionJsonOptions.Default</c> when writing <see cref="SessionState.AppliedFilters"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public static void Save(
            SessionState state,
            string? sessionFilePath = null,
            JsonSerializerOptions? options = null
        )
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

            var json = JsonSerializer.Serialize(state, options ?? s_JsonOptions);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Deletes the session JSON file when it exists (Reset Configuration).
        /// <para>Missing files are a no-op.</para>
        /// </summary>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultSessionFilePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public static void Delete(string? sessionFilePath = null)
        {
            var path = _ResolvePath(sessionFilePath);
            AppDataFile.DeleteFileIfExists(path, "session file");
        }

        /// <summary>
        /// Writes <paramref name="state"/> to the session file.
        /// <para>Failures are swallowed so preference saves do not crash the app.</para>
        /// </summary>
        /// <param name="state">Session values to persist.</param>
        /// <param name="sessionFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultSessionFilePath"/> is used.
        /// </param>
        /// <param name="options">
        /// Serializer options. When null, uses built-in session options. Pass Engine
        /// <c>SessionJsonOptions.Default</c> when writing <see cref="SessionState.AppliedFilters"/>.
        /// </param>
        public static void TrySave(
            SessionState state,
            string? sessionFilePath = null,
            JsonSerializerOptions? options = null
        )
        {
            try
            {
                Save(state, sessionFilePath, options);
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
            return sessionFilePath.IsBlank() ? _DefaultSessionFilePath() : sessionFilePath.Trim();
        }

        /// <summary>
        /// Reads session JSON, or an empty session when missing or unreadable.
        /// </summary>
        /// <param name="path">Absolute path to the session file.</param>
        /// <param name="options">Serializer options for this read.</param>
        /// <returns>Deserialized state, or a new empty session.</returns>
        private static SessionState _Read(string path, JsonSerializerOptions options)
        {
            if (!File.Exists(path))
            {
                return new SessionState();
            }

            try
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<SessionState>(json, options) ?? new SessionState();
                if (state.Version <= 0)
                {
                    state.Version = 1;
                }

                return state;
            }
            catch
            {
                return new SessionState();
            }
        }
    }
}
