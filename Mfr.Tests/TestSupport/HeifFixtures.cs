namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// HEIF test helpers. Committed binary is <c>tiny.heic</c> only; <c>.heif</c> coverage uses a temp copy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>tiny.heic</c> is Crest Convert <c>pampas-grass-small.heic</c> (CC0 1.0, SHA-256
    /// <c>bb24331d3d5b7c54a4e98faf40fe465be524f83f841076c63b699c6a74fe67bf</c>).
    /// </para>
    /// </remarks>
    internal static class HeifFixtures
    {
        /// <summary>
        /// Absolute path to the committed <c>tiny.heic</c> fixture.
        /// </summary>
        public static string TinyHeicPath => FixturePaths.Require("tiny.heic");

        /// <summary>
        /// Copies <c>tiny.heic</c> to a unique temp <c>.heif</c> path (same bytes; extension allowlist coverage).
        /// </summary>
        /// <returns>Temp file that deletes on dispose (best-effort).</returns>
        public static TempHeifCopy CopyTinyAsHeif()
        {
            var destination = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.heif");
            File.Copy(TinyHeicPath, destination, overwrite: true);
            return new TempHeifCopy(destination);
        }

        /// <summary>
        /// Temp <c>.heif</c> copy that best-effort deletes on dispose.
        /// </summary>
        internal sealed class TempHeifCopy : IDisposable
        {
            /// <summary>
            /// Absolute path to the temp <c>.heif</c> file.
            /// </summary>
            public string FullPath { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="TempHeifCopy"/> class.
            /// </summary>
            /// <param name="fullPath">Absolute path to the temp file owned by this instance.</param>
            internal TempHeifCopy(string fullPath)
            {
                FullPath = fullPath;
            }

            /// <summary>
            /// Best-effort deletes the temp file.
            /// </summary>
            public void Dispose()
            {
                try
                {
                    if (File.Exists(FullPath))
                    {
                        File.Delete(FullPath);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
