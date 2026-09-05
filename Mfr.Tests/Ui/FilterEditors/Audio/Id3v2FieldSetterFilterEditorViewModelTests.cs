using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.Filters.Audio;

namespace Mfr.Tests.Ui.FilterEditors.Audio
{
    /// <summary>
    /// Unit tests for <see cref="Id3v2FieldSetterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class Id3v2FieldSetterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies defaults match a new ID3v2 Field Setter (TIT2, empty text).
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_defaults_tit2_empty_text()
        {
            var step = new AppliedFilterStepViewModel("ID3v2 Field Setter", new Id3v2FieldSetterFilter());
            var editor = new Id3v2FieldSetterFilterEditorViewModel(step);

            Assert.Equal("TIT2", editor.SelectedFrame.FrameId);
            Assert.Equal(string.Empty, editor.Text);
            Assert.False(editor.OnlyIfEmpty);
            Assert.False(editor.ShowsLanguage);
            Assert.False(editor.ShowsDescription);

            var options = ((Id3v2FieldSetterFilter)step.Filter).Options;
            Assert.Equal("TIT2", options.FrameId);
            Assert.Equal(string.Empty, options.Text);
            Assert.False(options.OnlyIfEmpty);
            Assert.Null(options.Language);
            Assert.Null(options.Description);
        }

        /// <summary>
        /// Verifies frame, text, and only-if-empty replace step options.
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("ID3v2 Field Setter", new Id3v2FieldSetterFilter());
            _ = new Id3v2FieldSetterFilterEditorViewModel(step)
            {
                SelectedFrame = Id3v2FrameChoice.For("TALB"),
                Text = "<file-name>",
                OnlyIfEmpty = true,
            };

            var options = ((Id3v2FieldSetterFilter)step.Filter).Options;
            Assert.Equal("TALB", options.FrameId);
            Assert.Equal("<file-name>", options.Text);
            Assert.True(options.OnlyIfEmpty);
            Assert.Null(options.Language);
            Assert.Null(options.Description);
        }

        /// <summary>
        /// Verifies COMM shows language/description and empty identity maps to null options.
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_comm_identity_fields_update_options()
        {
            var step = new AppliedFilterStepViewModel("ID3v2 Field Setter", new Id3v2FieldSetterFilter());
            var editor = new Id3v2FieldSetterFilterEditorViewModel(step)
            {
                SelectedFrame = Id3v2FrameChoice.For("COMM"),
            };
            Assert.True(editor.ShowsLanguage);
            Assert.True(editor.ShowsDescription);

            editor.Text = "Hi";
            editor.Language = "eng";
            editor.Description = "desc";

            var options = ((Id3v2FieldSetterFilter)step.Filter).Options;
            Assert.Equal("COMM", options.FrameId);
            Assert.Equal("Hi", options.Text);
            Assert.Equal("eng", options.Language);
            Assert.Equal("desc", options.Description);

            editor.Language = "  ";
            editor.Description = string.Empty;
            options = ((Id3v2FieldSetterFilter)step.Filter).Options;
            Assert.Null(options.Language);
            Assert.Null(options.Description);
        }

        /// <summary>
        /// Verifies leaving a multi-instance frame clears language/description on the step.
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_leaving_comm_clears_identity()
        {
            var step = new AppliedFilterStepViewModel(
                "ID3v2 Field Setter",
                new Id3v2FieldSetterFilter(
                    new Id3v2FieldSetterOptions(FrameId: "COMM", Text: "X", Language: "eng", Description: "d")
                )
            );
            var editor = new Id3v2FieldSetterFilterEditorViewModel(step);
            Assert.Equal("eng", editor.Language);
            Assert.Equal("d", editor.Description);

            editor.SelectedFrame = Id3v2FrameChoice.For("TIT2");
            Assert.False(editor.ShowsLanguage);
            Assert.Equal(string.Empty, editor.Language);
            Assert.Equal(string.Empty, editor.Description);

            var options = ((Id3v2FieldSetterFilter)step.Filter).Options;
            Assert.Equal("TIT2", options.FrameId);
            Assert.Null(options.Language);
            Assert.Null(options.Description);
        }

        /// <summary>
        /// Verifies hydrating from options restores frame and flags.
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_hydrates_from_existing_options()
        {
            var step = new AppliedFilterStepViewModel(
                "ID3v2 Field Setter",
                new Id3v2FieldSetterFilter(
                    new Id3v2FieldSetterOptions(FrameId: "txxx", Text: "custom", OnlyIfEmpty: true, Description: "key")
                )
            );
            var editor = new Id3v2FieldSetterFilterEditorViewModel(step);

            Assert.Equal("TXXX", editor.SelectedFrame.FrameId);
            Assert.Equal("custom", editor.Text);
            Assert.True(editor.OnlyIfEmpty);
            Assert.False(editor.ShowsLanguage);
            Assert.True(editor.ShowsDescription);
            Assert.Equal("key", editor.Description);
        }
    }
}
