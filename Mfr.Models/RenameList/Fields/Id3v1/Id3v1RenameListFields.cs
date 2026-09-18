using Mfr.Models.Filters;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Models.RenameList.Fields.Id3v1
{
    /// <summary>
    /// All MP3 ID3v1 Rename List fields (Title, Artist, Album, Year, Comment, Track, Genre).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Property keys and display names match <see cref="Id3v1Field"/> names (same as Filter Options
    /// Apply-To). Writable fields use <see cref="Id3v1FieldTarget"/>. Resolve reads the
    /// overlay ID3v1 block via <see cref="AudioOverlayBlockFieldIo"/>.
    /// </para>
    /// </remarks>
    public static class Id3v1RenameListFields
    {
        /// <summary>
        /// ID3v1 property group id.
        /// </summary>
        public const string Group = "ID3v1";

        /// <summary>
        /// User-visible group label in the field shuttle groups list (MFR7 <c>MP3 ID3v1</c>).
        /// </summary>
        public const string GroupLabel = "MP3 ID3v1";

        /// <summary>
        /// ID3v1 group fields in Apply-To / enum order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            .. Enum.GetValues<Id3v1Field>()
                .Select(static field => new Id3v1RenameListField(field, _DefaultWidth(field))),
        ];

        /// <summary>
        /// Wider default for title / free-text; others stay at 160.
        /// </summary>
        private static int _DefaultWidth(Id3v1Field field)
        {
            return field is Id3v1Field.Title or Id3v1Field.Comment ? 200 : 160;
        }
    }
}
