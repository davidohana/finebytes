using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Trimming;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter Configuration editor for <see cref="ShrinkDuplicateCharactersFilter"/>.
    /// </summary>
    internal sealed partial class ShrinkDuplicateCharactersFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public ShrinkDuplicateCharactersFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the character whose adjacent duplicates are collapsed (empty = no-op).
        /// </summary>
        [ObservableProperty]
        private string _characterText = "-";

        partial void OnCharacterTextChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ShrinkDuplicateCharactersFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                CharacterText = filter.Options.Character == '\0' ? string.Empty : filter.Options.Character.ToString();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not ShrinkDuplicateCharactersFilter filter)
            {
                return;
            }

            var character = string.IsNullOrEmpty(CharacterText) ? '\0' : CharacterText[0];
            var options = new ShrinkDuplicateCharactersOptions(Character: character);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
