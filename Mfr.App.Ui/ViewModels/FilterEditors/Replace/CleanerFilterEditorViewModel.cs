using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Replace;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Replace
{
    /// <summary>
    /// Filter Configuration editor for <see cref="CleanerFilter"/>.
    /// </summary>
    internal sealed partial class CleanerFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CleanerFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets whether Windows-illegal file-name characters are cleaned.
        /// </summary>
        [ObservableProperty]
        private bool _removeIllegalChars;

        /// <summary>
        /// Gets or sets the custom character list to clean.
        /// </summary>
        [ObservableProperty]
        private string _customCharsToRemove = string.Empty;

        /// <summary>
        /// Gets or sets whether cleaned characters are replaced instead of deleted.
        /// </summary>
        [ObservableProperty]
        private bool _replaceWith;

        /// <summary>
        /// Gets or sets the replacement string used when <see cref="ReplaceWith"/> is enabled.
        /// </summary>
        [ObservableProperty]
        private string _replacement = string.Empty;

        partial void OnRemoveIllegalCharsChanged(bool value) => _ApplyOptions();

        partial void OnCustomCharsToRemoveChanged(string value) => _ApplyOptions();

        partial void OnReplaceWithChanged(bool value) => _ApplyOptions();

        partial void OnReplacementChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not CleanerFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                RemoveIllegalChars = filter.Options.RemoveIllegalChars;
                CustomCharsToRemove = filter.Options.CustomCharsToRemove ?? string.Empty;
                var replacement = filter.Options.Replacement ?? string.Empty;
                ReplaceWith = replacement.Length > 0;
                Replacement = replacement;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not CleanerFilter filter)
            {
                return;
            }

            var options = new CleanerOptions(
                RemoveIllegalChars: RemoveIllegalChars,
                CustomCharsToRemove: CustomCharsToRemove ?? string.Empty,
                Replacement: ReplaceWith ? Replacement ?? string.Empty : string.Empty
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
