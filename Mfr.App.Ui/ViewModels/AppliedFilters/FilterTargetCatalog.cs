using Mfr.Filters;
using Mfr.Models.Filters;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;

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
                new FilterTargetOption("File Prefix", new FilePrefixTarget()),
                new FilterTargetOption("Extension", new FileExtensionTarget()),
                new FilterTargetOption("Full File Name", new FileFullNameTarget()),
            ]
        );

        private static readonly FilterTargetGroupOption _pathGroup = new(
            "Path",
            [
                new FilterTargetOption("Full Path", new FullPathTarget()),
                new FilterTargetOption("Parent Directory", new ParentDirectoryTarget()),
                new FilterTargetOption("Ancestor Folder", new AncestorFolderTarget(1)),
            ]
        );

        private static readonly FilterTargetGroupOption _audioTagGroup = new("Audio Tag", _BuildSemanticAudioOptions());

        private static readonly FilterTargetGroupOption _id3v1Group = new("ID3v1", _BuildId3v1Options());

        private static readonly FilterTargetGroupOption _id3v2Group = new("ID3v2", _BuildId3v2Options());

        private static readonly FilterTargetGroupOption _xiphGroup = new("Xiph", _BuildXiphOptions());

        /// <summary>
        /// Gets Apply-To groups shown in Filter Options.
        /// </summary>
        public static IReadOnlyList<FilterTargetGroupOption> Groups { get; } =
        [_fileNameGroup, _pathGroup, _audioTagGroup, _id3v1Group, _id3v2Group, _xiphGroup];

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

            if (target is Id3v2FrameTarget frame)
            {
                return _GetId3v2DisplayLabel(frame);
            }

            var entry = _FindEntry(target);
            return entry?.Option.Label ?? string.Empty;
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
        /// Formats an ID3v2 Apply-To label, using the frame description when present.
        /// </summary>
        /// <param name="frame">ID3v2 target.</param>
        /// <returns>Friendly frame id, or <c>FRAMEID (description)</c> when a descriptor is set.</returns>
        private static string _GetId3v2DisplayLabel(Id3v2FrameTarget frame)
        {
            if (!string.IsNullOrEmpty(frame.Description))
            {
                return $"{frame.FrameId.ToUpperInvariant()} ({frame.Description})";
            }

            return _GetId3v2FrameLabel(frame.FrameId);
        }

        private static IReadOnlyList<FilterTargetOption> _BuildSemanticAudioOptions()
        {
            return
            [
                .. Enum.GetValues<SemanticAudioField>()
                    .Select(field => new FilterTargetOption(
                        _GetSemanticAudioLabel(field),
                        new SemanticAudioFieldTarget(field)
                    )),
            ];
        }

        private static IReadOnlyList<FilterTargetOption> _BuildId3v1Options()
        {
            return
            [
                .. Enum.GetValues<Id3v1Field>()
                    .Select(field => new FilterTargetOption(field.ToString(), new Id3v1FieldTarget(field))),
            ];
        }

        private static IReadOnlyList<FilterTargetOption> _BuildId3v2Options()
        {
            return
            [
                .. Id3v2ModeledFrame.AllModeledFrameIds.Select(frameId => new FilterTargetOption(
                    _GetId3v2FrameLabel(frameId),
                    new Id3v2FrameTarget(frameId)
                )),
            ];
        }

        private static IReadOnlyList<FilterTargetOption> _BuildXiphOptions()
        {
            return
            [
                .. XiphKnownKeys.All.Select(key => new FilterTargetOption(
                    _GetXiphKeyLabel(key),
                    new XiphFieldTarget(key)
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

        private static string _GetId3v2FrameLabel(string frameId)
        {
            return frameId.ToUpperInvariant() switch
            {
                "TALB" => "TALB (Album)",
                "TBPM" => "TBPM (BPM)",
                "TCOM" => "TCOM (Composer)",
                "TCON" => "TCON (Genre)",
                "TCOP" => "TCOP (Copyright)",
                "COMM" => "COMM (Comment)",
                "TENC" => "TENC (Encoded By)",
                "TEXT" => "TEXT (Lyricist)",
                "TIT1" => "TIT1 (Grouping)",
                "TIT2" => "TIT2 (Title)",
                "TIT3" => "TIT3 (Subtitle)",
                "TPE1" => "TPE1 (Artist)",
                "TPE2" => "TPE2 (Album Artist)",
                "TPOS" => "TPOS (Disc)",
                "TRCK" => "TRCK (Track)",
                "TYER" => "TYER (Year)",
                "TDRC" => "TDRC (Recording Date)",
                "USLT" => "USLT (Lyrics)",
                "TXXX" => "TXXX (Custom)",
                _ => frameId,
            };
        }

        private static string _GetXiphKeyLabel(string key)
        {
            return key.ToUpperInvariant() switch
            {
                "TITLE" => "Title",
                "ALBUM" => "Album",
                "ARTIST" => "Artist",
                "ALBUMARTIST" => "Album Artist",
                "COMPOSER" => "Composer",
                "GENRE" => "Genre",
                "DESCRIPTION" => "Description",
                "COMMENT" => "Comment",
                "LYRICS" => "Lyrics",
                "UNSYNCEDLYRICS" => "Unsynced Lyrics",
                "COPYRIGHT" => "Copyright",
                "GROUPING" => "Grouping",
                "CONTENTGROUP" => "Content Group",
                "DATE" => "Date",
                "YEAR" => "Year",
                "TRACKNUMBER" => "Track Number",
                "TRACKTOTAL" => "Track Total",
                "TOTALTRACKS" => "Total Tracks",
                "DISCNUMBER" => "Disc Number",
                "DISCTOTAL" => "Disc Total",
                "TOTALDISCS" => "Total Discs",
                "BPM" => "BPM",
                "TEMPO" => "Tempo",
                "CONDUCTOR" => "Conductor",
                "MUSICBRAINZ_ARTISTID" => "MusicBrainz Artist Id",
                "MUSICBRAINZ_ALBUMID" => "MusicBrainz Album Id",
                "MUSICBRAINZ_ALBUMARTISTID" => "MusicBrainz Album Artist Id",
                "MUSICBRAINZ_TRACKID" => "MusicBrainz Track Id",
                "MUSICBRAINZ_DISCID" => "MusicBrainz Disc Id",
                "MUSICBRAINZ_ALBUMSTATUS" => "MusicBrainz Album Status",
                "MUSICBRAINZ_ALBUMTYPE" => "MusicBrainz Album Type",
                "MUSICBRAINZ_RELEASECOUNTRY" => "MusicBrainz Release Country",
                "MUSICIP_PUID" => "MusicIP Id",
                "ASIN" => "Amazon Id",
                _ => key,
            };
        }
    }
}
