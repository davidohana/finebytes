using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Case
{
    /// <summary>
    /// Filter Configuration editor for <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    internal sealed partial class SentenceEndCharactersFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public SentenceEndCharactersFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the characters that mark sentence endings for later filters.
        /// </summary>
        [ObservableProperty]
        private string _characters = string.Empty;

        partial void OnCharactersChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not SentenceEndCharactersFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                Characters = filter.Options.Characters;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not SentenceEndCharactersFilter filter)
            {
                return;
            }

            var options = new SentenceEndCharactersOptions(Characters: Characters ?? string.Empty);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
