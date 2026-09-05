using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Unit tests for <see cref="InserterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class InserterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Inserter option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Inserter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Inserter", new InserterFilter());
            var editor = new InserterFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.InsertText);
            Assert.Equal(1, editor.Position);
            Assert.Equal(InserterOrigin.Beginning, editor.StartFrom);
            Assert.False(editor.Overwrite);

            editor.InsertText = "_-";
            editor.Position = 3;
            editor.StartFrom = InserterOrigin.End;
            editor.Overwrite = true;

            var options = ((InserterFilter)step.Filter).Options;
            Assert.Equal("_-", options.Text);
            Assert.Equal(3, options.Position);
            Assert.Equal(InserterOrigin.End, options.StartFrom);
            Assert.True(options.Overwrite);
        }
    }
}
