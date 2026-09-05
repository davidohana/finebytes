using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.Filters.Audio;

namespace Mfr.Tests.Ui.FilterEditors.Audio
{
    /// <summary>
    /// Unit tests for <see cref="AudioTagSetterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class AudioTagSetterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies defaults match a new Audio Tag Setter (all fields omitted, auto-increment on).
        /// </summary>
        [Fact]
        public void Audio_tag_setter_defaults_omit_all_fields()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Setter", new AudioTagSetterFilter());
            var editor = new AudioTagSetterFilterEditorViewModel(step);

            Assert.Equal(AudioTagSetterFieldChoice.All.Count, editor.FieldRows.Count);
            Assert.All(editor.FieldRows, row => Assert.False(row.IsActive));
            Assert.True(_Row(editor, AudioTagSetterFieldKind.Track).AutoIncrement);
            Assert.True(((AudioTagSetterFilter)step.Filter).Options.TrackAutoIncrement);
            Assert.Null(((AudioTagSetterFilter)step.Filter).Options.Title);
        }

        /// <summary>
        /// Verifies three-state mode + text replace step options for a string field.
        /// </summary>
        [Fact]
        public void Audio_tag_setter_title_mode_and_text_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Setter", new AudioTagSetterFilter());
            var editor = new AudioTagSetterFilterEditorViewModel(step);
            var title = _Row(editor, AudioTagSetterFieldKind.Title);

            title.Text = "<file-name>";
            title.IsActive = true;
            var options = ((AudioTagSetterFilter)step.Filter).Options;
            Assert.NotNull(options.Title);
            Assert.Equal("<file-name>", options.Title.Text);
            Assert.False(options.Title.OnlyIfEmpty);

            title.IsActive = null;
            options = ((AudioTagSetterFilter)step.Filter).Options;
            Assert.NotNull(options.Title);
            Assert.True(options.Title.OnlyIfEmpty);

            title.IsActive = false;
            options = ((AudioTagSetterFilter)step.Filter).Options;
            Assert.Null(options.Title);
        }

        /// <summary>
        /// Verifies track auto-increment and track field options update together.
        /// </summary>
        [Fact]
        public void Audio_tag_setter_track_and_auto_increment_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Setter", new AudioTagSetterFilter());
            var editor = new AudioTagSetterFilterEditorViewModel(step);
            var track = _Row(editor, AudioTagSetterFieldKind.Track);

            track.Text = "1";
            track.IsActive = true;
            track.AutoIncrement = false;

            var options = ((AudioTagSetterFilter)step.Filter).Options;
            Assert.NotNull(options.Track);
            Assert.Equal("1", options.Track.Text);
            Assert.False(options.TrackAutoIncrement);

            track.AutoIncrement = true;
            Assert.True(((AudioTagSetterFilter)step.Filter).Options.TrackAutoIncrement);
        }

        /// <summary>
        /// Verifies hydrating from options restores three-state modes and texts.
        /// </summary>
        [Fact]
        public void Audio_tag_setter_syncs_from_existing_options()
        {
            var step = new AppliedFilterStepViewModel(
                "Audio Tag Setter",
                new AudioTagSetterFilter(
                    new AudioTagSetterOptions(
                        Title: new AudioTagStringFieldOptions("Fixed", OnlyIfEmpty: true),
                        Year: new AudioTagStringFieldOptions("2004"),
                        Track: new AudioTagStringFieldOptions("3"),
                        TrackAutoIncrement: false
                    )
                )
            );
            var editor = new AudioTagSetterFilterEditorViewModel(step);

            var title = _Row(editor, AudioTagSetterFieldKind.Title);
            Assert.Null(title.IsActive);
            Assert.Equal("Fixed", title.Text);

            var year = _Row(editor, AudioTagSetterFieldKind.Year);
            Assert.True(year.IsActive);
            Assert.Equal("2004", year.Text);

            var track = _Row(editor, AudioTagSetterFieldKind.Track);
            Assert.True(track.IsActive);
            Assert.Equal("3", track.Text);
            Assert.False(track.AutoIncrement);

            Assert.False(_Row(editor, AudioTagSetterFieldKind.Album).IsActive);
        }

        /// <summary>
        /// Verifies the field-row catalog covers every declared kind once.
        /// </summary>
        [Fact]
        public void Audio_tag_setter_field_rows_match_catalog()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Setter", new AudioTagSetterFilter());
            var editor = new AudioTagSetterFilterEditorViewModel(step);
            var catalogKinds = AudioTagSetterFieldChoice.All.Select(c => c.Kind).ToArray();

            Assert.Equal(catalogKinds, editor.FieldRows.Select(r => r.Kind));
            Assert.Equal(catalogKinds.Length, catalogKinds.Distinct().Count());
            Assert.Equal(
                Enum.GetValues<AudioTagSetterFieldKind>().OrderBy(kind => kind),
                catalogKinds.OrderBy(kind => kind)
            );
            Assert.True(_Row(editor, AudioTagSetterFieldKind.Lyrics).Multiline);
            Assert.True(_Row(editor, AudioTagSetterFieldKind.Track).ShowsAutoIncrement);
        }

        /// <summary>
        /// Verifies track auto-increment persists on options even when the track field is omitted.
        /// </summary>
        [Fact]
        public void Audio_tag_setter_auto_increment_updates_when_track_omitted()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Setter", new AudioTagSetterFilter());
            var editor = new AudioTagSetterFilterEditorViewModel(step);
            var track = _Row(editor, AudioTagSetterFieldKind.Track);

            Assert.Null(((AudioTagSetterFilter)step.Filter).Options.Track);
            Assert.True(((AudioTagSetterFilter)step.Filter).Options.TrackAutoIncrement);

            track.AutoIncrement = false;
            Assert.Null(((AudioTagSetterFilter)step.Filter).Options.Track);
            Assert.False(((AudioTagSetterFilter)step.Filter).Options.TrackAutoIncrement);
        }

        private static AudioTagSetterFieldRowViewModel _Row(
            AudioTagSetterFilterEditorViewModel editor,
            AudioTagSetterFieldKind kind
        )
        {
            return editor.FieldRows.Single(row => row.Kind == kind);
        }
    }
}
