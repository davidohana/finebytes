using Mfr.Models.Filters;
using Mfr.Models.Tags;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Identifies one selectable Apply-To target in Filter Options.
    /// </summary>
    public enum FilterTargetKind
    {
        /// <summary>File name without extension.</summary>
        FilePrefix,

        /// <summary>File extension including the leading dot.</summary>
        FileExtension,

        /// <summary>Full file name (prefix + extension).</summary>
        FileFullName,

        /// <summary>Preview full file path.</summary>
        FullPath,

        /// <summary>Preview containing-directory path.</summary>
        ParentDirectory,

        /// <summary>Ancestor folder segment name.</summary>
        AncestorFolder,

        /// <summary>Semantic audio overlay field.</summary>
        SemanticAudio,
    }

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
    /// <param name="Kind">Target kind written onto the selected step.</param>
    /// <param name="AudioField">Semantic audio field when <paramref name="Kind"/> is <see cref="FilterTargetKind.SemanticAudio"/>.</param>
    public sealed record FilterTargetOption(string Label, FilterTargetKind Kind, SemanticAudioField? AudioField = null)
    {
        /// <summary>
        /// Builds a <see cref="FilterTarget"/> from this option and optional ancestor level.
        /// </summary>
        /// <param name="ancestorFolderLevel">Ancestor distance when <see cref="Kind"/> is <see cref="FilterTargetKind.AncestorFolder"/>.</param>
        /// <returns>Concrete target instance.</returns>
        public FilterTarget BuildTarget(int ancestorFolderLevel)
        {
            return Kind switch
            {
                FilterTargetKind.FilePrefix => new FilePrefixTarget(),
                FilterTargetKind.FileExtension => new FileExtensionTarget(),
                FilterTargetKind.FileFullName => new FileFullNameTarget(),
                FilterTargetKind.FullPath => new FullPathTarget(),
                FilterTargetKind.ParentDirectory => new ParentDirectoryTarget(),
                FilterTargetKind.AncestorFolder => new AncestorFolderTarget(Math.Max(1, ancestorFolderLevel)),
                FilterTargetKind.SemanticAudio => new SemanticAudioFieldTarget(AudioField!.Value),
                _ => throw new InvalidOperationException($"Unsupported target kind: {Kind}."),
            };
        }

        /// <summary>
        /// Returns whether this option matches <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Current filter target.</param>
        /// <returns><see langword="true"/> when this option represents <paramref name="target"/>.</returns>
        public bool Matches(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return Kind switch
            {
                FilterTargetKind.FilePrefix => target is FilePrefixTarget,
                FilterTargetKind.FileExtension => target is FileExtensionTarget,
                FilterTargetKind.FileFullName => target is FileFullNameTarget,
                FilterTargetKind.FullPath => target is FullPathTarget,
                FilterTargetKind.ParentDirectory => target is ParentDirectoryTarget,
                FilterTargetKind.AncestorFolder => target is AncestorFolderTarget,
                FilterTargetKind.SemanticAudio => target is SemanticAudioFieldTarget semantic
                    && semantic.Field == AudioField,
                _ => false,
            };
        }
    }
}
