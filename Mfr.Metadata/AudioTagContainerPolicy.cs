using Mfr.Models.Tags;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Detects audio container format via TagLib.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capability methods (<c>GetSupportedBlocks</c>, <c>EnsureSupported</c>, …) live on
    /// <see cref="Models.Tags.AudioTagContainerPolicy"/>.
    /// </para>
    /// </remarks>
    public static class AudioTagContainerPolicy
    {
        /// <summary>
        /// Detects the container of an existing audio file by opening it with TagLib.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>The detected container, or <see cref="AudioContainerFormat.Unknown"/> when unmodeled.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty or relative.</exception>
        /// <exception cref="IOException">TagLib cannot open the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the file structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static AudioContainerFormat Detect(string absolutePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

            if (!Path.IsPathFullyQualified(absolutePath))
                throw new ArgumentException("Path must be fully qualified.", nameof(absolutePath));

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            return DetectFrom(file);
        }

        /// <summary>
        /// Maps an open TagLib file to a container format.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Detected container, or <see cref="AudioContainerFormat.Unknown"/> when unmodeled.</returns>
        internal static AudioContainerFormat DetectFrom(TagLib.File file)
        {
            return file switch
            {
                TagLib.Mpeg.AudioFile => AudioContainerFormat.Mpeg,
                TagLib.Flac.File => AudioContainerFormat.Flac,
                TagLib.Ogg.File => AudioContainerFormat.Ogg,
                TagLib.Mpeg4.File => AudioContainerFormat.Mpeg4,
                TagLib.Asf.File => AudioContainerFormat.Asf,
                TagLib.Riff.File => AudioContainerFormat.Riff,
                TagLib.Ape.File => AudioContainerFormat.Ape,
                _ => AudioContainerFormat.Unknown,
            };
        }
    }
}
