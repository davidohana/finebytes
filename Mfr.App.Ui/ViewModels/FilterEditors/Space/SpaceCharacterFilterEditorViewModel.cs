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
        private const string _percent20Replacement = "%20";
        private const string _spaceReplacement = " ";
        private const string _underscoreReplacement = "_";

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
            if (Definition != SpaceCharacterDefinition.Other && !string.IsNullOrEmpty(value))
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
                ReplacePercent20 = options.Replacements.Contains(_percent20Replacement);
                ReplaceSpaces = options.Replacements.Contains(_spaceReplacement);
                ReplaceUnderscores = options.Replacements.Contains(_underscoreReplacement);
                CustomText = _ResolveCustomReplacementText(options.Replacements);
                ReplaceCustom = CustomText.Length > 0;
            });
        }

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

        private List<string> _BuildReplacements()
        {
            var replacements = new List<string>(capacity: 4);
            if (ReplacePercent20)
            {
                replacements.Add(_percent20Replacement);
            }

            if (ReplaceSpaces)
            {
                replacements.Add(_spaceReplacement);
            }

            if (ReplaceUnderscores)
            {
                replacements.Add(_underscoreReplacement);
            }

            if (ReplaceCustom && CustomText.Length > 0)
            {
                replacements.Add(CustomText);
            }

            return replacements;
        }

        private static string _ResolveCustomReplacementText(IReadOnlyList<string> replacements)
        {
            foreach (var replacement in replacements)
            {
                if (replacement is not (_percent20Replacement or _spaceReplacement or _underscoreReplacement))
                {
                    return replacement;
                }
            }

            return string.Empty;
        }

        private char _ResolveSpaceCharacter()
        {
            return Definition switch
            {
                SpaceCharacterDefinition.Space => ' ',
                SpaceCharacterDefinition.Underscore => '_',
                SpaceCharacterDefinition.Other when OtherCharacter.Length > 0 => OtherCharacter[0],
                SpaceCharacterDefinition.Other => ' ',
                _ => ' ',
            };
        }

        /// <summary>
        /// Maps a persisted separator character to editor definition state.
        /// </summary>
        /// <param name="spaceCharacter">Separator from <see cref="SpaceCharacterOptions.SpaceCharacter"/>.</param>
        /// <returns>
        /// The matching <see cref="SpaceCharacterDefinition"/> and a one-character string when the definition is
        /// <see cref="SpaceCharacterDefinition.Other"/>; otherwise an empty string.
        /// </returns>
        private static (SpaceCharacterDefinition Definition, string OtherCharacter) _ResolveDefinition(
            char spaceCharacter
        )
        {
            return spaceCharacter switch
            {
                ' ' => (SpaceCharacterDefinition.Space, string.Empty),
                '_' => (SpaceCharacterDefinition.Underscore, string.Empty),
                var other => (SpaceCharacterDefinition.Other, other.ToString()),
            };
        }
    }
}
