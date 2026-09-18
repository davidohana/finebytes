namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Absolute paths to files under the test assembly <c>Fixtures</c> output folder.
    /// </summary>
    internal static class FixturePaths
    {
        /// <summary>
        /// Resolves an absolute path to a committed fixture copied beside the test assembly.
        /// </summary>
        /// <param name="fileName">File name under <c>Fixtures/</c> (for example <c>tiny.jpeg</c>).</param>
        /// <returns>Fully qualified fixture path.</returns>
        public static string Require(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            Assert.True(
                File.Exists(fixturePath),
                $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output."
            );
            return Path.GetFullPath(fixturePath);
        }
    }
}
