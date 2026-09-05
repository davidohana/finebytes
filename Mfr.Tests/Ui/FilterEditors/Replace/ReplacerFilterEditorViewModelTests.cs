using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.Filters.Replace;

namespace Mfr.Tests.Ui.FilterEditors.Replace
{
    /// <summary>
    /// Unit tests for <see cref="ReplacerFilterEditorViewModel"/>.
    /// </summary>
    public sealed class ReplacerFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Replacer option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Replacer_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Replacer", new ReplacerFilter());

            var editor = new ReplacerFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.Find);

            Assert.Equal(string.Empty, editor.Replacement);

            Assert.Equal(ReplacerMode.Literal, editor.Match.Mode);

            Assert.Equal("feat.", editor.FindWatermark);

            Assert.Equal("feature.", editor.ReplacementWatermark);

            Assert.Contains("literally", editor.FindToolTip, StringComparison.Ordinal);

            Assert.DoesNotContain("$0", editor.ReplacementToolTip, StringComparison.Ordinal);

            Assert.False(editor.Match.CaseSensitive);

            Assert.True(editor.Match.ReplaceAll);

            Assert.False(editor.Match.WholeWord);

            editor.Match.Mode = ReplacerMode.Wildcard;

            Assert.Equal("DSC*.JPG", editor.FindWatermark);

            Assert.Equal("photo.jpg", editor.ReplacementWatermark);

            Assert.Contains("*", editor.FindToolTip, StringComparison.Ordinal);

            Assert.DoesNotContain("$0", editor.ReplacementToolTip, StringComparison.Ordinal);

            editor.Find = @"\((.+)\)";

            editor.Replacement = "$1";

            editor.Match.Mode = ReplacerMode.Regex;

            editor.Match.CaseSensitive = true;

            editor.Match.ReplaceAll = false;

            editor.Match.WholeWord = true;

            Assert.Equal(@"\((.+)\)", editor.FindWatermark);

            Assert.Equal("$1", editor.ReplacementWatermark);

            Assert.Contains("regex", editor.FindToolTip, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("$0", editor.ReplacementToolTip, StringComparison.Ordinal);

            var options = ((ReplacerFilter)step.Filter).Options;

            Assert.Equal(@"\((.+)\)", options.Find);

            Assert.Equal("$1", options.Replacement);

            Assert.Equal(ReplacerMode.Regex, options.Match.Mode);

            Assert.True(options.Match.CaseSensitive);

            Assert.False(options.Match.ReplaceAll);

            Assert.True(options.Match.WholeWord);
        }
    }
}
