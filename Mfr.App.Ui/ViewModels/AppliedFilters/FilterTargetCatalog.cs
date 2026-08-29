using Mfr.Filters;
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

        private static readonly FilterTargetGroupOption _audioTagGroup = new("Audio Tag", _BuildSemanticAudioOptions());

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

            var entry = _FindEntry(target);
            if (entry is null)
            {
                return (_fileNameGroup, _fileNameGroup.Targets[0], 1);
            }

            var ancestorFolderLevel = target is AncestorFolderTarget ancestor ? ancestor.Level : 1;
            return (entry.Value.Group, entry.Value.Option, ancestorFolderLevel);
        }

        /// <summary>
        /// Finds the catalog group and option for <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Current filter target.</param>
        /// <returns>Matching catalog entry, or <see langword="null"/> when the target is not in the catalog.</returns>
        private static (FilterTargetGroupOption Group, FilterTargetOption Option)? _FindEntry(FilterTarget target)
        {
            foreach (var group in Groups)
            {
                foreach (var option in group.Targets)
                {
                    if (option.Matches(target))
                    {
                        return (group, option);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the Applied-list subtitle for <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">Applied filter instance.</param>
        /// <returns>Subtitle text for string-target filters; otherwise an empty string.</returns>
        public static string GetApplyToLabel(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter is not StringTargetFilter stringFilter)
            {
                return string.Empty;
            }

            var label = GetLabel(stringFilter.Target);
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            return stringFilter.ApplyScope switch
            {
                SubstringApplyScope => $"{label} (Substring)",
                TokenApplyScope => $"{label} (Token)",
                _ => label,
            };
        }

        /// <summary>
        /// Gets the Apply-To display text for <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Filter target instance.</param>
        /// <returns>Human-readable target label.</returns>
        public static string GetLabel(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is AncestorFolderTarget ancestor)
            {
                return ancestor.Level == 1 ? "Parent Folder" : $"Ancestor Folder ({ancestor.Level})";
            }

            var entry = _FindEntry(target);
            return entry?.Option.Label ?? string.Empty;
        }

        private static IReadOnlyList<FilterTargetOption> _BuildSemanticAudioOptions()
        {
            return
            [
                .. Enum.GetValues<SemanticAudioField>()
                    .Select(field => new FilterTargetOption(
                        _GetSemanticAudioLabel(field),
                        FilterTargetKind.SemanticAudio,
                        field
                    )),
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
