using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// ComboBox row for a modeled ID3v2 frame id with a user-visible label.
    /// </summary>
    /// <param name="FrameId">Four-character frame id (uppercase).</param>
    /// <param name="DisplayName">Label shown in the field picker (e.g. <c>TIT2 (Title)</c>).</param>
    internal sealed record Id3v2FrameChoice(string FrameId, string DisplayName)
    {
        /// <summary>
        /// Modeled frame rows in Apply-To order (<see cref="Id3v2ModeledFrame.AllModeledFrameIds"/>).
        /// </summary>
        public static IReadOnlyList<Id3v2FrameChoice> All { get; } =
        [.. Id3v2ModeledFrame.AllModeledFrameIds.Select(id => new Id3v2FrameChoice(id, Id3v2FrameLabels.For(id)))];

        /// <summary>
        /// Default frame row (<c>TIT2</c>) used for empty/unknown ids.
        /// </summary>
        public static Id3v2FrameChoice Tit2 { get; } = All.First(c => c.FrameId == "TIT2");

        /// <summary>
        /// Returns the combo row for <paramref name="frameId"/>, or TIT2 when unknown/empty.
        /// </summary>
        /// <param name="frameId">Frame id from filter options (any casing).</param>
        /// <returns>Matching choice, or the TIT2 row as fallback.</returns>
        public static Id3v2FrameChoice For(string? frameId)
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return Tit2;
            }

            var normalized = frameId.Trim().ToUpperInvariant();
            foreach (var choice in All)
            {
                if (choice.FrameId == normalized)
                {
                    return choice;
                }
            }

            return Tit2;
        }

        /// <summary>
        /// Gets whether the language box applies (<c>COMM</c> / <c>USLT</c>).
        /// </summary>
        public bool ShowsLanguage => Id3v2ModeledFrame.UsesLanguageIdentity(FrameId);

        /// <summary>
        /// Gets whether the description box applies (<c>COMM</c> / <c>USLT</c> / <c>TXXX</c>).
        /// </summary>
        public bool ShowsDescription => Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(FrameId);

        /// <inheritdoc />
        public override string ToString()
        {
            return DisplayName;
        }
    }
}
