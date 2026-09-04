using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Misc;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Misc
{
    /// <summary>
    /// Filter Configuration editor for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    internal sealed partial class StripParenthesesFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public StripParenthesesFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the bracket pair type to strip.
        /// </summary>
        [ObservableProperty]
        private ParenthesisType _type = ParenthesisType.Round;

        /// <summary>
        /// Gets or sets whether bracketed contents are removed with the delimiters.
        /// </summary>
        [ObservableProperty]
        private bool _removeContents = true;

        partial void OnTypeChanged(ParenthesisType value) => _ApplyOptions();

        partial void OnRemoveContentsChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not StripParenthesesFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                Type = filter.Options.Type;
                RemoveContents = filter.Options.RemoveContents;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not StripParenthesesFilter filter)
            {
                return;
            }

            var options = new StripParenthesesOptions(Type: Type, RemoveContents: RemoveContents);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
