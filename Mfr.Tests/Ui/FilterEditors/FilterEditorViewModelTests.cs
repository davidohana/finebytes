using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/>.
    /// </summary>
    public sealed class FilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies an empty Applied selection clears the configuration title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_no_selection_clears_title()
        {
            var editor = new FilterEditorViewModel();

            editor.SyncSelection([]);

            Assert.False(editor.HasSelectedStep);
            Assert.Equal(string.Empty, editor.TitleText);
        }

        /// <summary>
        /// Verifies the first selected step sets the Applied Filter title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_one_step_sets_title()
        {
            var editor = new FilterEditorViewModel();
            var step = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());

            editor.SyncSelection([step]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies multi-select uses the first selected row for the title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_multi_select_uses_first_row()
        {
            var editor = new FilterEditorViewModel();
            var first = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());
            var second = new AppliedFilterStepViewModel("Letters Case", new LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies Shrink Duplicate Characters character edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Shrink_duplicate_character_text_updates_step_options()
        {
            var step = new AppliedFilterStepViewModel(
                "Shrink Duplicate Characters",
                new ShrinkDuplicateCharactersFilter()
            );
            var editor = new ShrinkDuplicateCharactersFilterEditorViewModel(step);

            Assert.Equal("-", editor.CharacterText);
            Assert.Equal('-', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);

            editor.CharacterText = ">";
            Assert.Equal('>', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);

            editor.CharacterText = string.Empty;
            Assert.Equal('\0', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);
        }

        /// <summary>
        /// Verifies an empty/null character on the filter loads as an empty editor field.
        /// </summary>
        [Fact]
        public void Shrink_duplicate_null_character_loads_as_empty_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Shrink Duplicate Characters",
                new ShrinkDuplicateCharactersFilter(
                    new FilePrefixTarget(),
                    new ShrinkDuplicateCharactersOptions(Character: '\0')
                )
            );
            var editor = new ShrinkDuplicateCharactersFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.CharacterText);
        }

        /// <summary>
        /// Verifies Trim Between position/anchor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Trim_between_positions_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);

            Assert.Equal(2, editor.StartValue);
            Assert.Equal(Side.Left, editor.StartAnchor);
            Assert.Equal(4, editor.EndValue);
            Assert.Equal(Side.Left, editor.EndAnchor);

            editor.StartValue = 13;
            editor.EndValue = 5;
            editor.EndAnchor = Side.Right;

            var options = ((TrimBetweenFilter)step.Filter).Options;
            Assert.Equal(new Position(13, Side.Left), options.Start);
            Assert.Equal(new Position(5, Side.Right), options.End);
        }

        /// <summary>
        /// Verifies Fix Leading 0's option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Fix_leading_zeros_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", new FixLeadingZerosFilter());
            var editor = new FixLeadingZerosFilterEditorViewModel(step);

            Assert.Equal(2, editor.Width);
            Assert.False(editor.RemoveExtraZeros);
            Assert.Equal(1, editor.MaxCount);
            Assert.True(editor.WholeWordOnly);

            editor.Width = 4;
            editor.RemoveExtraZeros = true;
            editor.MaxCount = 0;
            editor.WholeWordOnly = false;

            var options = ((FixLeadingZerosFilter)step.Filter).Options;
            Assert.Equal(4, options.Width);
            Assert.True(options.RemoveExtraZeros);
            Assert.Equal(0, options.MaxCount);
            Assert.False(options.WholeWordOnly);
        }

        /// <summary>
        /// Verifies Space After chars/neighbor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Space_after_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Space After", new SpaceAfterFilter());
            var editor = new SpaceTriggerFilterEditorViewModel(step);

            Assert.Equal(",;!", editor.Chars);
            Assert.True(editor.OnlyWhenNeighborLetterOrDigit);
            Assert.Contains("after", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = ".,";
            editor.OnlyWhenNeighborLetterOrDigit = false;

            var options = ((SpaceAfterFilter)step.Filter).Options;
            Assert.Equal(".,", options.AfterChars);
            Assert.False(options.OnlyWhenNextIsLetterOrDigit);
        }

        /// <summary>
        /// Verifies Space Around chars/neighbor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Space_around_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Space Around", new SpaceAroundFilter());
            var editor = new SpaceTriggerFilterEditorViewModel(step);

            Assert.Equal("-", editor.Chars);
            Assert.True(editor.OnlyWhenNeighborLetterOrDigit);
            Assert.Contains("before and after", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = "+=";
            editor.OnlyWhenNeighborLetterOrDigit = false;

            var options = ((SpaceAroundFilter)step.Filter).Options;
            Assert.Equal("+=", options.AroundChars);
            Assert.False(options.OnlyWhenNeighboringAreLettersOrDigits);
        }

        /// <summary>
        /// Verifies Capitalize After trigger-char edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Capitalize_after_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Capitalize After", new CapitalizeAfterFilter());
            var editor = new CharacterListFilterEditorViewModel(step);

            Assert.Equal(",!()[]{};-", editor.Chars);
            Assert.Contains("succeed", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = "._";

            var options = ((CapitalizeAfterFilter)step.Filter).Options;
            Assert.Equal("._", options.CapitalizeAfterChars);
        }

        /// <summary>
        /// Verifies Sentence End Characters list edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Sentence_end_characters_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Sentence End Characters", new SentenceEndCharactersFilter());
            var editor = new CharacterListFilterEditorViewModel(step);

            Assert.Equal("-.!", editor.Chars);
            Assert.Contains("sentence had ended", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = ":;";

            var options = ((SentenceEndCharactersFilter)step.Filter).Options;
            Assert.Equal(":;", options.Characters);
        }

        /// <summary>
        /// Verifies Strip Parentheses type/contents edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Strip_parentheses_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Strip Parentheses", new StripParenthesesFilter());
            var editor = new StripParenthesesFilterEditorViewModel(step);

            Assert.Equal(ParenthesisType.Round, editor.Type);
            Assert.True(editor.RemoveContents);

            editor.Type = ParenthesisType.Square;
            editor.RemoveContents = false;

            var options = ((StripParenthesesFilter)step.Filter).Options;
            Assert.Equal(ParenthesisType.Square, options.Type);
            Assert.False(options.RemoveContents);
        }

        /// <summary>
        /// Verifies Cleaner option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Cleaner_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Cleaner", new CleanerFilter());
            var editor = new CleanerFilterEditorViewModel(step);

            Assert.True(editor.RemoveIllegalChars);
            Assert.Equal(@"!""#$%&'()*+,/:;<=>?@[]\^`{}|~", editor.CustomCharsToRemove);
            Assert.False(editor.ReplaceWith);
            Assert.Equal(string.Empty, editor.Replacement);

            editor.RemoveIllegalChars = false;
            editor.CustomCharsToRemove = "@#";
            editor.Replacement = "_";
            editor.ReplaceWith = true;

            var options = ((CleanerFilter)step.Filter).Options;
            Assert.False(options.RemoveIllegalChars);
            Assert.Equal("@#", options.CustomCharsToRemove);
            Assert.Equal("_", options.Replacement);

            editor.ReplaceWith = false;

            options = ((CleanerFilter)step.Filter).Options;
            Assert.Equal(string.Empty, options.Replacement);
            Assert.Equal("_", editor.Replacement);
        }

        /// <summary>
        /// Verifies Counter option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Counter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Counter", new CounterFilter());
            var editor = new CounterFilterEditorViewModel(step);

            Assert.Equal(1, editor.Start);
            Assert.Equal(1, editor.Increment);
            Assert.Equal(0, editor.Width);
            Assert.Equal("0", editor.PadCharText);
            Assert.Equal(CounterPosition.Prepend, editor.Position);
            Assert.Equal(" - ", editor.Separator);
            Assert.True(editor.ResetPerFolder);
            Assert.True(editor.HasSeparatorOptions);

            editor.Start = 10;
            editor.Increment = 5;
            editor.Width = 3;
            editor.PadCharText = " ";
            editor.Position = CounterPosition.Replace;
            editor.Separator = "_";
            editor.ResetPerFolder = false;

            Assert.False(editor.HasSeparatorOptions);

            var options = ((CounterFilter)step.Filter).Options;
            Assert.Equal(10, options.Start);
            Assert.Equal(5, options.Step);
            Assert.Equal(3, options.Width);
            Assert.Equal("1", options.PadChar);
            Assert.Equal(CounterPosition.Replace, options.Position);
            Assert.Equal("_", options.Separator);
            Assert.False(options.ResetPerFolder);

            editor.Position = CounterPosition.Append;
            editor.PadCharText = "X";

            options = ((CounterFilter)step.Filter).Options;
            Assert.Equal(CounterPosition.Append, options.Position);
            Assert.Equal("X", options.PadChar);
            Assert.True(editor.HasSeparatorOptions);
        }

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
