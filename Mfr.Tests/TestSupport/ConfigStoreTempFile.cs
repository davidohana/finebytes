namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Disposable temp <c>config.json</c> path for <see cref="ConfigStore"/> tests.
    /// <para>On dispose: deletes the file (or owning temp directory) and resets the singleton via <see cref="ConfigStoreTestReset.LoadEmpty"/>.</para>
    /// </summary>
    public sealed class ConfigStoreTempFile : IDisposable
    {
        private readonly string? _directoryToDelete;

        /// <summary>
        /// Gets the config file path (may not exist yet).
        /// </summary>
        public string Path { get; }

        private ConfigStoreTempFile(string path, string? directoryToDelete = null)
        {
            Path = path;
            _directoryToDelete = directoryToDelete;
        }

        /// <summary>
        /// Unique config path that does not exist yet (caller may <c>Save</c> / <c>EnsureDefaultFile</c>).
        /// </summary>
        /// <returns>A disposable temp path.</returns>
        public static ConfigStoreTempFile Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mfr-test-config-" + Guid.NewGuid().ToString("N") + ".json"
            );
            return new ConfigStoreTempFile(path);
        }

        /// <summary>
        /// Resets <see cref="ConfigStore"/> to defaults, then returns a unique unused config path.
        /// </summary>
        /// <returns>A disposable temp path with the singleton already reset.</returns>
        public static ConfigStoreTempFile CreateReady()
        {
            ConfigStoreTestReset.LoadEmpty();
            return Create();
        }

        /// <summary>
        /// Writes <paramref name="json"/> and loads it into <see cref="ConfigStore"/>.
        /// </summary>
        /// <param name="json">Document text (default empty object).</param>
        /// <returns>A disposable temp path whose file is loaded.</returns>
        public static ConfigStoreTempFile CreateLoaded(string json = "{}")
        {
            var temp = Create();
            File.WriteAllText(temp.Path, json);
            ConfigStore.Load(temp.Path);
            return temp;
        }

        /// <summary>
        /// Writes <paramref name="json"/> without loading (caller will <c>Load</c> / assert).
        /// </summary>
        /// <param name="json">Document text.</param>
        /// <returns>A disposable temp path with the file on disk.</returns>
        public static ConfigStoreTempFile CreateWithContent(string json)
        {
            var temp = Create();
            File.WriteAllText(temp.Path, json);
            return temp;
        }

        /// <summary>
        /// Unique config path under a fresh temp directory with <paramref name="relativeSegments"/>
        /// that are not created yet (for <c>Save</c> creating missing directories).
        /// </summary>
        /// <param name="relativeSegments">Path segments under the temp directory (e.g. <c>nested</c>, <c>config.json</c>).</param>
        /// <returns>A disposable path; dispose deletes the whole temp directory.</returns>
        public static ConfigStoreTempFile CreateUnderNewDirectory(params string[] relativeSegments)
        {
            ArgumentNullException.ThrowIfNull(relativeSegments);
            if (relativeSegments.Length == 0)
            {
                throw new ArgumentException("At least one path segment is required.", nameof(relativeSegments));
            }

            var dir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mfr-test-config-dir-" + Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(dir);

            var path = dir;
            foreach (var segment in relativeSegments)
            {
                path = System.IO.Path.Combine(path, segment);
            }

            return new ConfigStoreTempFile(path, directoryToDelete: dir);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                if (_directoryToDelete is not null)
                {
                    if (Directory.Exists(_directoryToDelete))
                    {
                        Directory.Delete(_directoryToDelete, recursive: true);
                    }
                }
                else if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup.
            }

            ConfigStoreTestReset.LoadEmpty();
        }
    }
}
