using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Formatting
{
    /// <summary>
    /// Filter Configuration editor for <see cref="FormatterFilter"/> (format string only; token UI deferred).
    /// </summary>
    internal sealed partial class FormatterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public FormatterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the format string (literals and formatter tokens).
        /// </summary>
        [ObservableProperty]
        private string _template = string.Empty;

        partial void OnTemplateChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not FormatterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() => Template = filter.Options.Template);
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not FormatterFilter filter)
            {
                return;
            }

            var options = new FormatterOptions(Template: Template);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
