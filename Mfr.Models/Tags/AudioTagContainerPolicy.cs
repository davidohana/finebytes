namespace Mfr.Models.Tags
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
    /// <para>
    /// Container detection from TagLib lives in <c>Mfr.Metadata.AudioTagContainerDetector</c>.
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
            var alternatives =
                supported.Count == 0
                    ? "no tag blocks are supported there"
                    : "supported blocks: " + string.Join(", ", supported.Select(_DescribeBlock));

            throw new NotSupportedException(
                $"{_DescribeBlock(block)} tags are not supported in {_DescribeContainer(container)} files ({alternatives})."
            );
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
