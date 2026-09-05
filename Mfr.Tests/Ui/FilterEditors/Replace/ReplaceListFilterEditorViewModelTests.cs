using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.Filters.Replace;

namespace Mfr.Tests.Ui.FilterEditors.Replace
{
    /// <summary>
    /// Unit tests for <see cref="ReplaceListFilterEditorViewModel"/>.
    /// </summary>
    public sealed class ReplaceListFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Replace List option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Replace_list_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Replace List", new ReplaceListFilter());
            var editor = new ReplaceListFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.EntriesText);
            Assert.Equal(ReplacerMode.Literal, editor.Match.Mode);
            Assert.Equal(". => _\nfeat. => feature.\nLive", editor.EntriesWatermark);
            Assert.False(editor.Match.CaseSensitive);
            Assert.True(editor.Match.ReplaceAll);
            Assert.True(editor.Match.WholeWord);

            editor.Match.Mode = ReplacerMode.Wildcard;
            Assert.Equal("DSC*.JPG => photo.jpg\ntrack?.mp3 => track0.mp3\n*.tmp", editor.EntriesWatermark);
            editor.Match.Mode = ReplacerMode.Regex;
            Assert.Equal("[0-9]+ => N\n\\. => _\n\\s+ => _", editor.EntriesWatermark);

            editor.EntriesText = "a => b\nBlue Train => Blue_Train\nx";
            editor.Match.Mode = ReplacerMode.Wildcard;
            editor.Match.CaseSensitive = true;
            editor.Match.ReplaceAll = false;
            editor.Match.WholeWord = false;

            var options = ((ReplaceListFilter)step.Filter).Options;
            Assert.Equal(3, options.Entries.Count);
            Assert.Equal("a", options.Entries[0].Search);
            Assert.Equal("b", options.Entries[0].Replacement);
            Assert.Equal("Blue Train", options.Entries[1].Search);
            Assert.Equal("Blue_Train", options.Entries[1].Replacement);
            Assert.Equal("x", options.Entries[2].Search);
            Assert.Equal("", options.Entries[2].Replacement);
            Assert.Equal(ReplacerMode.Wildcard, options.Match.Mode);
            Assert.True(options.Match.CaseSensitive);
            Assert.False(options.Match.ReplaceAll);
            Assert.False(options.Match.WholeWord);
        }

        /// <summary>
        /// Verifies mode/flag edits keep structured entries that editor text cannot round-trip
        /// (search containing <c>=&gt;</c>).
        /// </summary>
        [Fact]
        public void Replace_list_flag_edits_preserve_entries_with_separator_in_search()
        {
            var filter = new ReplaceListFilter(
                Target: new FilePrefixTarget(),
                Options: new ReplaceListOptions(
                    Entries: [new ReplaceListEntry("a=>b", "x")],
                    Match: new ReplacerMatchOptions(
                        Mode: ReplacerMode.Literal,
                        CaseSensitive: false,
                        ReplaceAll: true,
                        WholeWord: true
                    )
                )
            );
            var step = new AppliedFilterStepViewModel("Replace List", filter);
            _ = new ReplaceListFilterEditorViewModel(step)
            {
                Match =
                {
                    Mode = ReplacerMode.Regex,
                    CaseSensitive = true,
                    WholeWord = false,
                },
            };

            var options = ((ReplaceListFilter)step.Filter).Options;
            Assert.Single(options.Entries);
            Assert.Equal("a=>b", options.Entries[0].Search);
            Assert.Equal("x", options.Entries[0].Replacement);
            Assert.Equal(ReplacerMode.Regex, options.Match.Mode);
            Assert.True(options.Match.CaseSensitive);
            Assert.False(options.Match.WholeWord);
        }
    }
}
