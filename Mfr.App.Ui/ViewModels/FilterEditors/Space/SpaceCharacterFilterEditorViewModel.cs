using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Space
{
    /// <summary>
    /// Filter Configuration editor for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    internal sealed partial class SpaceCharacterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public SpaceCharacterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the word-separator definition choice.
        /// </summary>
        [ObservableProperty]
        private SpaceCharacterDefinition _definition;

        /// <summary>
        /// Gets or sets the custom separator character when <see cref="Definition"/> is <see cref="SpaceCharacterDefinition.Other"/>.
        /// </summary>
        [ObservableProperty]
        private string _otherCharacter = string.Empty;

        /// <summary>
        /// Gets or sets whether U+0020 SPACE is replaced with the defined separator.
        /// </summary>
        [ObservableProperty]
        private bool _replaceSpaces;

        /// <summary>
        /// Gets or sets whether underscore is replaced with the defined separator.
        /// </summary>
        [ObservableProperty]
        private bool _replaceUnderscores;

        /// <summary>
        /// Gets or sets whether the literal <c>%20</c> sequence is replaced with the defined separator.
        /// </summary>
        [ObservableProperty]
        private bool _replacePercent20;

        /// <summary>
        /// Gets or sets whether <see cref="CustomText"/> is applied.
        /// </summary>
        [ObservableProperty]
        private bool _replaceCustom;

        /// <summary>
        /// Gets or sets the custom substring replaced when <see cref="ReplaceCustom"/> is enabled.
        /// </summary>
        [ObservableProperty]
        private string _customText = string.Empty;

        partial void OnDefinitionChanged(SpaceCharacterDefinition value) => _ApplyOptions();

        partial void OnOtherCharacterChanged(string value)
        {
            // MFR7 replaces the custom char on each keypress; keep the last character when pasting.
            if (value.Length > 1)
            {
                OtherCharacter = value[^1].ToString();
                return;
            }

            if (Definition != SpaceCharacterDefinition.Other && value.Length > 0)
            {
                Definition = SpaceCharacterDefinition.Other;
                return;
            }

            _ApplyOptions();
        }

        partial void OnReplaceSpacesChanged(bool value) => _ApplyOptions();

        partial void OnReplaceUnderscoresChanged(bool value) => _ApplyOptions();

        partial void OnReplacePercent20Changed(bool value) => _ApplyOptions();

        partial void OnReplaceCustomChanged(bool value) => _ApplyOptions();

        partial void OnCustomTextChanged(string value)
        {
            if (!ReplaceCustom && !string.IsNullOrEmpty(value))
            {
                ReplaceCustom = true;
                return;
            }

            _ApplyOptions();
        }

        /// <summary>
        /// Loads editor fields from the step's <see cref="SpaceCharacterFilter"/> without applying.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not SpaceCharacterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                var options = filter.Options;
                (Definition, OtherCharacter) = _ResolveDefinition(options.SpaceCharacter);
                ReplacePercent20 = options.Replacements.Contains(SpaceCharacterOptions.Percent20Replacement);
                ReplaceSpaces = options.Replacements.Contains(SpaceCharacterOptions.SpaceReplacement);
                ReplaceUnderscores = options.Replacements.Contains(SpaceCharacterOptions.UnderscoreReplacement);
                CustomText = _ResolveCustomReplacementText(options.Replacements);
                ReplaceCustom = CustomText.Length > 0;
            });
        }

        /// <summary>
        /// Writes current editor fields onto the step filter when options changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Other with an empty character box persists <c>\0</c>. <see cref="SpaceCharacterFilter"/> setup then
        /// throws (MFR7 message) so preview shows an error instead of silently falling back to U+0020 SPACE.
        /// </para>
        /// </remarks>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not SpaceCharacterFilter filter)
            {
                return;
            }

            var options = new SpaceCharacterOptions(
                SpaceCharacter: _ResolveSpaceCharacter(),
                Replacements: _BuildReplacements()
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Builds the replacements list from checkbox and custom-text state (known tokens first).
        /// </summary>
        /// <returns>Ordered replacement substrings for <see cref="SpaceCharacterOptions.Replacements"/>.</returns>
        private List<string> _BuildReplacements()
        {
            var replacements = new List<string>(capacity: 4);
            if (ReplacePercent20)
            {
                replacements.Add(SpaceCharacterOptions.Percent20Replacement);
            }

            if (ReplaceSpaces)
            {
                replacements.Add(SpaceCharacterOptions.SpaceReplacement);
            }

            if (ReplaceUnderscores)
            {
                replacements.Add(SpaceCharacterOptions.UnderscoreReplacement);
            }

            if (ReplaceCustom && CustomText.Length > 0)
            {
                replacements.Add(CustomText);
            }

            return replacements;
        }

        /// <summary>
        /// Returns the first replacement that is not a built-in %20 / space / underscore token.
        /// </summary>
        /// <param name="replacements">Persisted replacement list.</param>
        /// <returns>Custom substring, or empty when none.</returns>
        private static string _ResolveCustomReplacementText(IReadOnlyList<string> replacements)
        {
            foreach (var replacement in replacements)
            {
                if (
                    replacement
                    is not (
                        SpaceCharacterOptions.Percent20Replacement
                        or SpaceCharacterOptions.SpaceReplacement
                        or SpaceCharacterOptions.UnderscoreReplacement
                    )
                )
                {
                    return replacement;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves the separator character from definition radios and the Other text box.
        /// </summary>
        /// <returns>
        /// Word separator to persist; Other with an empty box yields <c>\0</c> (rejected at filter setup).
        /// </returns>
        private char _ResolveSpaceCharacter()
        {
            return Definition switch
            {
                SpaceCharacterDefinition.Space => ' ',
                SpaceCharacterDefinition.Underscore => '_',
                SpaceCharacterDefinition.Other when OtherCharacter.Length > 0 => OtherCharacter[0],
                SpaceCharacterDefinition.Other => '\0',
                _ => '\0',
            };
        }

        /// <summary>
        /// Maps a persisted separator character to editor definition state.
        /// </summary>
        /// <param name="spaceCharacter">Separator from <see cref="SpaceCharacterOptions.SpaceCharacter"/>.</param>
        /// <returns>
        /// The matching <see cref="SpaceCharacterDefinition"/> and a one-character string when the definition is
        /// <see cref="SpaceCharacterDefinition.Other"/>; otherwise an empty string. <c>\0</c> maps to Other with
        /// an empty box.
        /// </returns>
        private static (SpaceCharacterDefinition Definition, string OtherCharacter) _ResolveDefinition(
            char spaceCharacter
        )
        {
            return spaceCharacter switch
            {
                ' ' => (SpaceCharacterDefinition.Space, string.Empty),
                '_' => (SpaceCharacterDefinition.Underscore, string.Empty),
                '\0' => (SpaceCharacterDefinition.Other, string.Empty),
                var other => (SpaceCharacterDefinition.Other, other.ToString()),
            };
        }
    }
}
