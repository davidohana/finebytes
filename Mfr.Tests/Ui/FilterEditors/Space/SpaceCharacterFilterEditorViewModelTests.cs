using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.FilterEditors.Space
{
    /// <summary>
    /// Unit tests for <see cref="SpaceCharacterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class SpaceCharacterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Space Character defaults sync and option edits replace the step filter.
        /// </summary>
        [Fact]
        public void Space_character_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Space Character", new SpaceCharacterFilter());
            var editor = new SpaceCharacterFilterEditorViewModel(step);

            Assert.Equal(SpaceCharacterDefinition.Space, editor.Definition);
            Assert.Equal(string.Empty, editor.OtherCharacter);
            Assert.True(editor.ReplacePercent20);
            Assert.True(editor.ReplaceSpaces);
            Assert.True(editor.ReplaceUnderscores);
            Assert.False(editor.ReplaceCustom);
            Assert.Equal(string.Empty, editor.CustomText);
            Assert.Equal(
                SpaceCharacterOptions.DefaultReplacements,
                ((SpaceCharacterFilter)step.Filter).Options.Replacements
            );

            editor.Definition = SpaceCharacterDefinition.Underscore;
            editor.ReplaceSpaces = false;
            editor.CustomText = "++";

            var options = ((SpaceCharacterFilter)step.Filter).Options;
            Assert.Equal('_', options.SpaceCharacter);
            Assert.Equal(
                [SpaceCharacterOptions.Percent20Replacement, SpaceCharacterOptions.UnderscoreReplacement, "++"],
                options.Replacements
            );
            Assert.True(editor.ReplaceCustom);
        }

        /// <summary>
        /// Verifies a non-default Space Character filter loads into the editor fields.
        /// </summary>
        [Fact]
        public void Space_character_loads_non_default_options()
        {
            var step = new AppliedFilterStepViewModel(
                "Space Character",
                new SpaceCharacterFilter(
                    new FilePrefixTarget(),
                    new SpaceCharacterOptions(
                        SpaceCharacter: '-',
                        Replacements: [SpaceCharacterOptions.Percent20Replacement, "++"]
                    )
                )
            );
            var editor = new SpaceCharacterFilterEditorViewModel(step);

            Assert.Equal(SpaceCharacterDefinition.Other, editor.Definition);
            Assert.Equal("-", editor.OtherCharacter);
            Assert.True(editor.ReplacePercent20);
            Assert.False(editor.ReplaceSpaces);
            Assert.False(editor.ReplaceUnderscores);
            Assert.True(editor.ReplaceCustom);
            Assert.Equal("++", editor.CustomText);
        }

        /// <summary>
        /// Verifies typing an Other character selects the Other definition.
        /// </summary>
        [Fact]
        public void Space_character_other_text_selects_other_definition()
        {
            var step = new AppliedFilterStepViewModel("Space Character", new SpaceCharacterFilter());
            var editor = new SpaceCharacterFilterEditorViewModel(step) { OtherCharacter = "." };

            Assert.Equal(SpaceCharacterDefinition.Other, editor.Definition);
            Assert.Equal('.', ((SpaceCharacterFilter)step.Filter).Options.SpaceCharacter);
        }

        /// <summary>
        /// Verifies Other with an empty character does not overwrite the last valid separator (no silent space fallback).
        /// </summary>
        [Fact]
        public void Space_character_empty_other_does_not_apply()
        {
            var step = new AppliedFilterStepViewModel("Space Character", new SpaceCharacterFilter());
            var editor = new SpaceCharacterFilterEditorViewModel(step) { Definition = SpaceCharacterDefinition.Other };
            Assert.Equal(' ', ((SpaceCharacterFilter)step.Filter).Options.SpaceCharacter);

            editor.OtherCharacter = "-";
            Assert.Equal('-', ((SpaceCharacterFilter)step.Filter).Options.SpaceCharacter);

            editor.OtherCharacter = string.Empty;
            Assert.Equal(SpaceCharacterDefinition.Other, editor.Definition);
            Assert.Equal('-', ((SpaceCharacterFilter)step.Filter).Options.SpaceCharacter);
        }

        /// <summary>
        /// Verifies typing a second Other character replaces the first (MFR7 KeyPress parity).
        /// </summary>
        [Fact]
        public void Space_character_other_text_keeps_last_character()
        {
            var step = new AppliedFilterStepViewModel("Space Character", new SpaceCharacterFilter());
            var editor = new SpaceCharacterFilterEditorViewModel(step) { OtherCharacter = "-." };

            Assert.Equal(".", editor.OtherCharacter);
            Assert.Equal('.', ((SpaceCharacterFilter)step.Filter).Options.SpaceCharacter);
        }
    }
}
