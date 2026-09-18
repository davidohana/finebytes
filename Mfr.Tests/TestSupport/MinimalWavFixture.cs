namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Copies the committed PCM WAV scaffold used by TagLib audio metadata integration tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Source file: <c>Fixtures/minimal-silent.wav</c> — 48 bytes, mono 16-bit 44100 Hz, two silent PCM samples.
    /// Built by copying the prior in-source scaffold into a stable on-disk fixture.
    /// </para>
    /// </remarks>
    internal static class MinimalWavFixture
    {
        internal static string SourcePath => FixturePaths.Require("minimal-silent.wav");

        internal static void CopyScratchTo(string absoluteDestinationPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absoluteDestinationPath);
            File.Copy(SourcePath, absoluteDestinationPath, overwrite: true);
        }
    }
}
