namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Absolute test paths that are fully qualified on the host OS.
    /// </summary>
    /// <remarks>
    /// Prefer these over hard-coded <c>C:\…</c> strings so filter/path unit tests run on Linux CI.
    /// </remarks>
    internal static class TestPaths
    {
        /// <summary>
        /// Volume root used for synthetic absolute paths (<c>C:\</c> on Windows, <c>/</c> elsewhere).
        /// </summary>
        public static string VolumeRoot { get; } = OperatingSystem.IsWindows() ? @"C:\" : "/";

        /// <summary>
        /// Builds an absolute path under <see cref="VolumeRoot"/>.
        /// </summary>
        /// <param name="segments">Directory or file segments to append.</param>
        /// <returns>A fully qualified path for the host OS.</returns>
        public static string Absolute(params string[] segments)
        {
            return Path.Combine([VolumeRoot, .. segments]);
        }
    }
}
