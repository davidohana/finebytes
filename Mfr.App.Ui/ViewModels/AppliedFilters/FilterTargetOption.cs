using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// One Apply-To group shown in the first Filter Options combo.
    /// </summary>
    /// <param name="Label">Group display text.</param>
    /// <param name="Targets">Targets available in this group.</param>
    public sealed record FilterTargetGroupOption(string Label, IReadOnlyList<FilterTargetOption> Targets);

    /// <summary>
    /// One <see cref="FilterTarget"/> choice in the second Filter Options combo.
    /// </summary>
    /// <param name="Label">Target display text.</param>
    /// <param name="Prototype">Target template; parameterized fields are applied in <see cref="BuildTarget"/>.</param>
    public sealed record FilterTargetOption(string Label, FilterTarget Prototype)
    {
        /// <summary>
        /// Builds a <see cref="FilterTarget"/> from this option and optional parameterized fields.
        /// </summary>
        /// <param name="ancestorFolderLevel">Ancestor distance when <see cref="Prototype"/> is <see cref="AncestorFolderTarget"/>.</param>
        /// <param name="id3v2Language">Language for multi-instance ID3v2 frames; empty becomes <see langword="null"/>.</param>
        /// <param name="id3v2Description">Description for multi-instance ID3v2 frames; empty becomes <see langword="null"/>.</param>
        /// <returns>Concrete target instance.</returns>
        public FilterTarget BuildTarget(int ancestorFolderLevel, string? id3v2Language, string? id3v2Description)
        {
            return Prototype switch
            {
                AncestorFolderTarget => new AncestorFolderTarget(Math.Max(1, ancestorFolderLevel)),
                Id3v2FrameTarget frame => new Id3v2FrameTarget(
                    frame.FrameId,
                    _TrimOrNull(id3v2Language),
                    _TrimOrNull(id3v2Description)
                ),
                _ => Prototype,
            };
        }

        /// <summary>
        /// Returns whether this option matches <paramref name="target"/> for catalog lookup.
        /// </summary>
        /// <param name="target">Current filter target.</param>
        /// <returns><see langword="true"/> when this option represents <paramref name="target"/>.</returns>
        public bool Matches(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return Prototype switch
            {
                AncestorFolderTarget => target is AncestorFolderTarget,
                Id3v2FrameTarget prototype => target is Id3v2FrameTarget frame
                    && string.Equals(prototype.FrameId, frame.FrameId, StringComparison.OrdinalIgnoreCase),
                XiphFieldTarget prototype => target is XiphFieldTarget xiph
                    && string.Equals(prototype.Key, xiph.Key, StringComparison.OrdinalIgnoreCase),
                _ => Prototype.Equals(target),
            };
        }

        private static string? _TrimOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
