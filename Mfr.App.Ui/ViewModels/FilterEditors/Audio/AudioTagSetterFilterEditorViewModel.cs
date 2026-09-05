using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Audio;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Filter Configuration editor for <see cref="AudioTagSetterFilter"/>.
    /// </summary>
    internal sealed class AudioTagSetterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private readonly Dictionary<AudioTagSetterFieldKind, AudioTagSetterFieldRowViewModel> _kindToRow;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public AudioTagSetterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            FieldRows =
            [
                .. AudioTagSetterFieldChoice.All.Select(choice => new AudioTagSetterFieldRowViewModel(
                    choice,
                    _ApplyOptions
                )),
            ];
            _kindToRow = FieldRows.ToDictionary(row => row.Kind);
            Sections =
            [
                .. Enum.GetValues<AudioTagSetterFieldGroup>()
                    .Select(group => new AudioTagSetterFieldSectionViewModel(
                        AudioTagSetterFieldChoice.HeaderFor(group),
                        [.. FieldRows.Where(row => row.Group == group)]
                    )),
            ];
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the per-field option rows (three-state mode + format text).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> FieldRows { get; }

        /// <summary>
        /// Gets field rows grouped into editor fieldsets (Track / Disc, Basic, Extended).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldSectionViewModel> Sections { get; }

        /// <summary>
        /// Copies current filter options into editor rows without live replace.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not AudioTagSetterFilter filter)
            {
                return;
            }

            var options = filter.Options;
            LoadWithoutApplying(() =>
            {
                foreach (var row in FieldRows)
                {
                    var spec = _SpecFor(options, row.Kind);
                    row.IsActive = _ModeFromSpec(spec);
                    row.Text = spec?.Text ?? string.Empty;
                    if (row.ShowsAutoIncrement)
                    {
                        row.AutoIncrement = options.TrackAutoIncrement;
                    }
                }
            });
        }

        /// <summary>
        /// Builds options from field rows and replaces the step filter when changed.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not AudioTagSetterFilter filter)
            {
                return;
            }

            var trackRow = _Row(AudioTagSetterFieldKind.Track);
            var options = new AudioTagSetterOptions(
                Performers: _ToSpec(_Row(AudioTagSetterFieldKind.Performers)),
                AlbumArtists: _ToSpec(_Row(AudioTagSetterFieldKind.AlbumArtists)),
                Title: _ToSpec(_Row(AudioTagSetterFieldKind.Title)),
                Album: _ToSpec(_Row(AudioTagSetterFieldKind.Album)),
                Genre: _ToSpec(_Row(AudioTagSetterFieldKind.Genre)),
                Comment: _ToSpec(_Row(AudioTagSetterFieldKind.Comment)),
                Composers: _ToSpec(_Row(AudioTagSetterFieldKind.Composers)),
                Lyrics: _ToSpec(_Row(AudioTagSetterFieldKind.Lyrics)),
                Grouping: _ToSpec(_Row(AudioTagSetterFieldKind.Grouping)),
                Copyright: _ToSpec(_Row(AudioTagSetterFieldKind.Copyright)),
                Conductor: _ToSpec(_Row(AudioTagSetterFieldKind.Conductor)),
                Year: _ToSpec(_Row(AudioTagSetterFieldKind.Year)),
                BeatsPerMinute: _ToSpec(_Row(AudioTagSetterFieldKind.BeatsPerMinute)),
                Track: _ToSpec(trackRow),
                TrackCount: _ToSpec(_Row(AudioTagSetterFieldKind.TrackCount)),
                Disc: _ToSpec(_Row(AudioTagSetterFieldKind.Disc)),
                DiscCount: _ToSpec(_Row(AudioTagSetterFieldKind.DiscCount)),
                TrackAutoIncrement: trackRow.AutoIncrement
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Maps a row’s three-state mode + text to filter field options (or omit).
        /// </summary>
        private static AudioTagStringFieldOptions? _ToSpec(AudioTagSetterFieldRowViewModel row)
        {
            if (row.IsActive is false)
            {
                return null;
            }

            var onlyIfEmpty = row.IsActive is null;
            return new AudioTagStringFieldOptions(Text: row.Text, OnlyIfEmpty: onlyIfEmpty);
        }

        /// <summary>
        /// Maps stored options to three-state checkbox value.
        /// </summary>
        private static bool? _ModeFromSpec(AudioTagStringFieldOptions? spec)
        {
            if (spec is null)
            {
                return false;
            }

            if (spec.OnlyIfEmpty)
            {
                return null;
            }

            return true;
        }

        /// <summary>
        /// Reads the options property for <paramref name="kind"/>.
        /// </summary>
        private static AudioTagStringFieldOptions? _SpecFor(AudioTagSetterOptions options, AudioTagSetterFieldKind kind)
        {
            return kind switch
            {
                AudioTagSetterFieldKind.Track => options.Track,
                AudioTagSetterFieldKind.TrackCount => options.TrackCount,
                AudioTagSetterFieldKind.Disc => options.Disc,
                AudioTagSetterFieldKind.DiscCount => options.DiscCount,
                AudioTagSetterFieldKind.Performers => options.Performers,
                AudioTagSetterFieldKind.AlbumArtists => options.AlbumArtists,
                AudioTagSetterFieldKind.Title => options.Title,
                AudioTagSetterFieldKind.Album => options.Album,
                AudioTagSetterFieldKind.Year => options.Year,
                AudioTagSetterFieldKind.Genre => options.Genre,
                AudioTagSetterFieldKind.Comment => options.Comment,
                AudioTagSetterFieldKind.Composers => options.Composers,
                AudioTagSetterFieldKind.Conductor => options.Conductor,
                AudioTagSetterFieldKind.Grouping => options.Grouping,
                AudioTagSetterFieldKind.Copyright => options.Copyright,
                AudioTagSetterFieldKind.BeatsPerMinute => options.BeatsPerMinute,
                AudioTagSetterFieldKind.Lyrics => options.Lyrics,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// Returns the row for <paramref name="kind"/>.
        /// </summary>
        private AudioTagSetterFieldRowViewModel _Row(AudioTagSetterFieldKind kind)
        {
            return _kindToRow[kind];
        }
    }
}
