using Mfr.Models.Filters;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Models.RenameList.Fields.Id3v2
{
    /// <summary>
    /// All MP3 ID3v2 Rename List fields (modeled frames plus read-only Version).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Property keys are modeled frame ids from <see cref="Id3v2ModeledFrame.FrameIdToShortName"/>
    /// (no MFR7 legacy aliases). Display names use <see cref="Id3v2FrameLabels"/>. Writable frames
    /// use primary <see cref="Id3v2FrameTarget"/> instances (frame id only for
    /// <c>COMM</c>/<c>USLT</c>/<c>TXXX</c>). Unmodeled frames (web / UFID / APIC) are omitted.
    /// </para>
    /// </remarks>
    public static class Id3v2RenameListFields
    {
        /// <summary>
        /// ID3v2 property group id.
        /// </summary>
        public const string Group = "ID3v2";

        /// <summary>
        /// User-visible group label in the field shuttle groups list (MFR7 <c>MP3 ID3v2</c>).
        /// </summary>
        public const string GroupLabel = "MP3 ID3v2";

        /// <summary>
        /// Property keys within <see cref="Group"/> that are not frame ids.
        /// </summary>
        public static class Key
        {
            /// <summary>ID3v2 tag version (read-only).</summary>
            public const string Version = "Version";
        }

        /// <summary>
        /// ID3v2 group fields: Version, then modeled frames in Apply-To order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new Id3v2VersionRenameListField(),
            .. Id3v2ModeledFrame.AllModeledFrameIds.Select(static frameId => new Id3v2FrameRenameListField(
                frameId,
                _DefaultWidth(frameId)
            )),
        ];

        /// <summary>
        /// Wider default for title / free-text multi-instance frames; others stay at 160.
        /// </summary>
        private static int _DefaultWidth(string frameId)
        {
            return frameId is "TIT2" or "COMM" or "USLT" or "TXXX" ? 200 : 160;
        }
    }
}
