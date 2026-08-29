using Mfr.Models.Filters;
using Mfr.Models.Tags;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Apply-To groups and targets available in Filter Options.
    /// </summary>
    public static class FilterTargetCatalog
    {
        private static readonly FilterTargetGroupOption _fileNameGroup = new(
            "File Name",
            [
                new FilterTargetOption("File Prefix", FilterTargetKind.FilePrefix),
                new FilterTargetOption("Extension", FilterTargetKind.FileExtension),
                new FilterTargetOption("Full File Name", FilterTargetKind.FileFullName),
            ]
        );

        private static readonly FilterTargetGroupOption _pathGroup = new(
            "Path",
            [
                new FilterTargetOption("Full Path", FilterTargetKind.FullPath),
                new FilterTargetOption("Parent Directory", FilterTargetKind.ParentDirectory),
                new FilterTargetOption("Ancestor Folder", FilterTargetKind.AncestorFolder),
            ]
        );

        private static readonly FilterTargetGroupOption _audioTagGroup = new(
            "Audio Tag",
            _BuildSemanticAudioOptions()
        );

        /// <summary>
        /// Gets Apply-To groups shown in Filter Options.
        /// </summary>
        public static IReadOnlyList<FilterTargetGroupOption> Groups { get; } =
            [_fileNameGroup, _pathGroup, _audioTagGroup];

        /// <summary>
        /// Resolves the catalog entry for <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Current filter target.</param>
        /// <returns>Group, option, and ancestor level when applicable.</returns>
        public static (FilterTargetGroupOption Group, FilterTargetOption Option, int AncestorFolderLevel) Resolve(
            FilterTarget target
        )
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is AncestorFolderTarget ancestor)
            {
                var option = _pathGroup.Targets.First(static entry => entry.Kind == FilterTargetKind.AncestorFolder);
                return (_pathGroup, option, ancestor.Level);
            }

            foreach (var group in Groups)
            {
                foreach (var option in group.Targets)
                {
                    if (option.Matches(target, ancestorFolderLevel: 1))
                    {
                        return (group, option, 1);
                    }
                }
            }

            return (_fileNameGroup, _fileNameGroup.Targets[0], 1);
        }

        /// <summary>
        /// Gets the Applied-list subtitle for <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Filter target instance.</param>
        /// <returns>Human-readable Apply-To label.</returns>
        public static string GetLabel(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return target switch
            {
                FilePrefixTarget => "File Prefix",
                FileExtensionTarget => "Extension",
                FileFullNameTarget => "Full File Name",
                FullPathTarget => "Full Path",
                ParentDirectoryTarget => "Parent Directory",
                AncestorFolderTarget ancestor
                    => ancestor.Level == 1 ? "Parent Folder" : $"Ancestor Folder ({ancestor.Level})",
                SemanticAudioFieldTarget semantic => _GetSemanticAudioLabel(semantic.Field),
                _ => string.Empty,
            };
        }

        private static IReadOnlyList<FilterTargetOption> _BuildSemanticAudioOptions()
        {
            return
            [
                .. Enum.GetValues<SemanticAudioField>()
                    .Select(field => new FilterTargetOption(_GetSemanticAudioLabel(field), FilterTargetKind.SemanticAudio, field)),
            ];
        }

        private static string _GetSemanticAudioLabel(SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Title => "Title",
                SemanticAudioField.Album => "Album",
                SemanticAudioField.Performers => "Artist",
                SemanticAudioField.AlbumArtists => "Album Artist",
                SemanticAudioField.Composers => "Composer",
                SemanticAudioField.Genre => "Genre",
                SemanticAudioField.Comment => "Comment",
                SemanticAudioField.Lyrics => "Lyrics",
                SemanticAudioField.Copyright => "Copyright",
                SemanticAudioField.Grouping => "Grouping",
                SemanticAudioField.Year => "Year",
                SemanticAudioField.Track => "Track",
                SemanticAudioField.TrackCount => "Track Count",
                SemanticAudioField.Disc => "Disc",
                SemanticAudioField.DiscCount => "Disc Count",
                SemanticAudioField.BeatsPerMinute => "BPM",
                SemanticAudioField.Conductor => "Conductor",
                SemanticAudioField.MusicBrainzArtistId => "MusicBrainz Artist Id",
                SemanticAudioField.MusicBrainzReleaseId => "MusicBrainz Release Id",
                SemanticAudioField.MusicBrainzTrackId => "MusicBrainz Track Id",
                SemanticAudioField.MusicBrainzReleaseArtistId => "MusicBrainz Release Artist Id",
                SemanticAudioField.MusicBrainzDiscId => "MusicBrainz Disc Id",
                SemanticAudioField.MusicBrainzReleaseStatus => "MusicBrainz Release Status",
                SemanticAudioField.MusicBrainzReleaseType => "MusicBrainz Release Type",
                SemanticAudioField.MusicBrainzReleaseCountry => "MusicBrainz Release Country",
                SemanticAudioField.MusicIpId => "MusicIP Id",
                SemanticAudioField.AmazonId => "Amazon Id",
                _ => field.ToString(),
            };
        }
    }
}
