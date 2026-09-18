using Mfr.Models.Filters;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Models.RenameList.Fields.Xiph
{
    /// <summary>
    /// All Xiph Rename List fields (modeled keys from <see cref="XiphKnownKeys.All"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Property keys match known Xiph comment keys (same as Filter Options Apply-To). Display names
    /// and tips use <see cref="XiphKeyLabels"/>. Writable fields use <see cref="XiphFieldTarget"/>.
    /// Resolve reads the overlay Xiph block via <see cref="AudioOverlayBlockFieldIo"/>.
    /// </para>
    /// </remarks>
    public static class XiphRenameListFields
    {
        /// <summary>
        /// Xiph property group id.
        /// </summary>
        public const string Group = "Xiph";

        /// <summary>
        /// User-visible group label in the field shuttle groups list (matches Apply-To <c>Xiph</c>).
        /// </summary>
        public const string GroupLabel = "Xiph";

        /// <summary>
        /// Xiph group fields in known-key / Apply-To order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [.. XiphKnownKeys.All.Select(static key => new XiphRenameListField(key, _DefaultWidth(key)))];

        /// <summary>
        /// Wider default for title / free-text; others stay at 160.
        /// </summary>
        private static int _DefaultWidth(string key)
        {
            return
                key
                    is XiphKnownKeys.Title
                        or XiphKnownKeys.Description
                        or XiphKnownKeys.Comment
                        or XiphKnownKeys.Lyrics
                        or XiphKnownKeys.UnsyncedLyrics
                ? 200
                : 160;
        }
    }
}
