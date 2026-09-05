using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Dedicated ViewModel tests for <see cref="NameListFilterEditorViewModel"/>.
    /// </summary>
    public sealed class NameListFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies sync loads line text and templates from an existing filter.
        /// </summary>
        [Fact]
        public void Syncs_existing_entries_prefix_and_suffix()
        {
            var step = new AppliedFilterStepViewModel(
                "Name List",
                new NameListFilter(
                    Target: new FilePrefixTarget(),
                    Options: new NameListOptions(Entries: ["Alpha", "", "Beta"], Prefix: "pre_", Suffix: "_suf")
                )
            );

            var editor = new NameListFilterEditorViewModel(step);

            Assert.Equal("Alpha\n\nBeta", editor.EntriesText);
            Assert.Equal("pre_", editor.Prefix);
            Assert.Equal("_suf", editor.Suffix);
        }

        /// <summary>
        /// Verifies option edits replace the step filter, including a trailing blank line slot.
        /// </summary>
        [Fact]
        public void Option_edits_update_step_including_trailing_blank()
        {
            var step = new AppliedFilterStepViewModel("Name List", new NameListFilter());
            var editor = new NameListFilterEditorViewModel(step)
            {
                EntriesText = "Alpha\nBeta\n",
                Prefix = "<counter:initial=1,step=1,padding=none,length=1,resetScope=global>_",
                Suffix = "_x",
            };

            var options = ((NameListFilter)step.Filter).Options;
            Assert.Equal(["Alpha", "Beta"], options.Entries);
            Assert.Equal("<counter:initial=1,step=1,padding=none,length=1,resetScope=global>_", options.Prefix);
            Assert.Equal("_x", options.Suffix);

            editor.EntriesText = "Alpha\nBeta\n\n";
            options = ((NameListFilter)step.Filter).Options;
            Assert.Equal(["Alpha", "Beta", ""], options.Entries);
        }
    }
}
