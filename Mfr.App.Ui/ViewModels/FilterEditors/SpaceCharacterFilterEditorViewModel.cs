using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Filter Configuration editor for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    internal sealed partial class SpaceCharacterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isSyncing;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        /// <param name="filter">Current <see cref="SpaceCharacterFilter"/> instance.</param>
        public SpaceCharacterFilterEditorViewModel(AppliedFilterStepViewModel step, SpaceCharacterFilter filter)
            : base(step)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _SyncFromFilter(filter);
        }

        /// <summary>
        /// Gets or sets the word-separator definition choice.
        /// </summary>
        [ObservableProperty]
        private SpaceCharacterDefinition _definition;

        /// <summary>
        /// Gets or sets whether the space-character definition is selected.
        /// </summary>
        public bool IsDefinitionSpace
        {
            get => Definition == SpaceCharacterDefinition.Space;
            set
            {
                if (value)
                {
                    Definition = SpaceCharacterDefinition.Space;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the underscore definition is selected.
        /// </summary>
        public bool IsDefinitionUnderscore
        {
            get => Definition == SpaceCharacterDefinition.Underscore;
            set
            {
                if (value)
                {
                    Definition = SpaceCharacterDefinition.Underscore;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the custom-character definition is selected.
        /// </summary>
        public bool IsDefinitionOther
        {
            get => Definition == SpaceCharacterDefinition.Other;
            set
            {
                if (value)
                {
                    Definition = SpaceCharacterDefinition.Other;
                }
            }
        }

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

        partial void OnDefinitionChanged(SpaceCharacterDefinition value)
        {
            if (_isSyncing)
            {
                return;
            }

            OnPropertyChanged(nameof(IsDefinitionSpace));
            OnPropertyChanged(nameof(IsDefinitionUnderscore));
            OnPropertyChanged(nameof(IsDefinitionOther));
            _TryApplyOptions();
        }

        partial void OnOtherCharacterChanged(string value)
        {
            if (_isSyncing)
            {
                return;
            }

            if (Definition != SpaceCharacterDefinition.Other && !string.IsNullOrEmpty(value))
            {
                Definition = SpaceCharacterDefinition.Other;
                return;
            }

            _TryApplyOptions();
        }

        partial void OnReplaceSpacesChanged(bool value) => _TryApplyOptions();

        partial void OnReplaceUnderscoresChanged(bool value) => _TryApplyOptions();

        partial void OnReplacePercent20Changed(bool value) => _TryApplyOptions();

        partial void OnReplaceCustomChanged(bool value) => _TryApplyOptions();

        partial void OnCustomTextChanged(string value)
        {
            if (_isSyncing)
            {
                return;
            }

            if (!ReplaceCustom && !string.IsNullOrEmpty(value))
            {
                ReplaceCustom = true;
                return;
            }

            _TryApplyOptions();
        }

        private void _SyncFromFilter(SpaceCharacterFilter filter)
        {
            _isSyncing = true;
            try
            {
                var options = filter.Options;
                (Definition, OtherCharacter) = _ReadDefinition(options.SpaceCharacter);
                ReplaceSpaces = options.ReplaceSpaces;
                ReplaceUnderscores = options.ReplaceUnderscores;
                ReplacePercent20 = options.ReplacePercent20;
                CustomText = options.CustomText;
                ReplaceCustom = options.CustomText.Length > 0;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void _TryApplyOptions()
        {
            if (_isSyncing || Step.Filter is not SpaceCharacterFilter filter)
            {
                return;
            }

            var options = new SpaceCharacterOptions(
                SpaceCharacter: _ResolveSpaceCharacter(),
                ReplaceSpaces: ReplaceSpaces,
                ReplaceUnderscores: ReplaceUnderscores,
                ReplacePercent20: ReplacePercent20,
                CustomText: ReplaceCustom ? CustomText : string.Empty
            );
            ApplyIfChanged(filter, filter with { Options = options });
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

        private static (SpaceCharacterDefinition Definition, string OtherCharacter) _ReadDefinition(char spaceCharacter)
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
