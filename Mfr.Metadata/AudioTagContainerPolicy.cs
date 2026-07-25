using Mfr.Models.Tags;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Decides which tag blocks an audio container can hold, and which block to create when it holds none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format-specific operations (writing an ID3v2 frame, removing a single tag type, …) must call
    /// <see cref="EnsureSupported"/> first: an unsupported combination is an error the user sees on the rename row,
    /// never a silently skipped edit. Generic semantic writes do not consult this type except to pick the
    /// <see cref="GetRecommendedBlock">recommended block</see> for a file that carries no tags yet.
    /// </para>
    /// </remarks>
    public static class AudioTagContainerPolicy
    {
        private static readonly AudioTagBlockKind[] _MpegBlocks = [AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2];
        private static readonly AudioTagBlockKind[] _FlacBlocks = [AudioTagBlockKind.Xiph, AudioTagBlockKind.Ape];
        private static readonly AudioTagBlockKind[] _OggBlocks = [AudioTagBlockKind.Xiph];
        private static readonly AudioTagBlockKind[] _Mpeg4Blocks = [AudioTagBlockKind.Apple];
        private static readonly AudioTagBlockKind[] _AsfBlocks = [AudioTagBlockKind.Asf];
        private static readonly AudioTagBlockKind[] _RiffBlocks = [AudioTagBlockKind.RiffInfo];
        private static readonly AudioTagBlockKind[] _ApeBlocks = [AudioTagBlockKind.Ape];

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
        /// Lists the tag blocks <paramref name="container"/> can hold, in creation-preference order.
        /// </summary>
        /// <param name="container">Container to describe.</param>
        /// <returns>Supported block types; empty for <see cref="AudioContainerFormat.Unknown"/>.</returns>
        public static IReadOnlyList<AudioTagBlockKind> GetSupportedBlocks(AudioContainerFormat container)
        {
            return container switch
            {
                AudioContainerFormat.Mpeg => _MpegBlocks,
                AudioContainerFormat.Flac => _FlacBlocks,
                AudioContainerFormat.Ogg => _OggBlocks,
                AudioContainerFormat.Mpeg4 => _Mpeg4Blocks,
                AudioContainerFormat.Asf => _AsfBlocks,
                AudioContainerFormat.Riff => _RiffBlocks,
                AudioContainerFormat.Ape => _ApeBlocks,
                AudioContainerFormat.Unknown => [],
                _ => [],
            };
        }

        /// <summary>
        /// The block to create when a generic write targets a file of <paramref name="container"/> that carries no tags.
        /// </summary>
        /// <param name="container">Container to describe.</param>
        /// <returns>Recommended block type, or <see langword="null"/> when nothing can be created.</returns>
        public static AudioTagBlockKind? GetRecommendedBlock(AudioContainerFormat container)
        {
            var supported = GetSupportedBlocks(container);
            if (supported.Count == 0)
                return null;

            // MPEG prefers ID3v2 over the ID3v1 trailer; every other container lists a single block first.
            if (container == AudioContainerFormat.Mpeg)
                return AudioTagBlockKind.Id3v2;

            return supported[0];
        }

        /// <summary>
        /// Whether <paramref name="container"/> can hold <paramref name="block"/>.
        /// </summary>
        /// <param name="container">Container to test.</param>
        /// <param name="block">Block type to test.</param>
        /// <returns><see langword="true"/> when the combination is writable.</returns>
        public static bool Supports(AudioContainerFormat container, AudioTagBlockKind block)
        {
            return GetSupportedBlocks(container).Contains(block);
        }

        /// <summary>
        /// Throws when <paramref name="container"/> cannot hold <paramref name="block"/>.
        /// </summary>
        /// <param name="container">Container the operation targets.</param>
        /// <param name="block">Block type the operation reads or writes.</param>
        /// <exception cref="NotSupportedException">The container does not support that block type.</exception>
        public static void EnsureSupported(AudioContainerFormat container, AudioTagBlockKind block)
        {
            if (Supports(container, block))
                return;

            var supported = GetSupportedBlocks(container);
            var alternatives = supported.Count == 0
                ? "no tag blocks are supported there"
                : "supported blocks: " + string.Join(", ", supported.Select(_DescribeBlock));

            throw new NotSupportedException(
                $"{_DescribeBlock(block)} tags are not supported in {_DescribeContainer(container)} files ({alternatives}).");
        }

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

        private static string _DescribeBlock(AudioTagBlockKind block)
        {
            return block switch
            {
                AudioTagBlockKind.Id3v1 => "ID3v1",
                AudioTagBlockKind.Id3v2 => "ID3v2",
                AudioTagBlockKind.Xiph => "Xiph comment",
                AudioTagBlockKind.Ape => "APEv2",
                AudioTagBlockKind.Apple => "Apple/iTunes",
                AudioTagBlockKind.Asf => "ASF",
                AudioTagBlockKind.RiffInfo => "RIFF INFO",
                _ => block.ToString(),
            };
        }

        private static string _DescribeContainer(AudioContainerFormat container)
        {
            return container switch
            {
                AudioContainerFormat.Mpeg => "MP3",
                AudioContainerFormat.Flac => "FLAC",
                AudioContainerFormat.Ogg => "Ogg",
                AudioContainerFormat.Mpeg4 => "MP4/M4A",
                AudioContainerFormat.Asf => "WMA/ASF",
                AudioContainerFormat.Riff => "WAV",
                AudioContainerFormat.Ape => "Monkey's Audio",
                AudioContainerFormat.Unknown => "unrecognized audio",
                _ => "unrecognized audio",
            };
        }
    }
}
