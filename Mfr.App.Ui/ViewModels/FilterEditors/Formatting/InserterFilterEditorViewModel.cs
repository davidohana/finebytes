using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Formatting
{
    /// <summary>
    /// Filter Configuration editor for <see cref="InserterFilter"/>.
    /// </summary>
    internal sealed partial class InserterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public InserterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the text to insert (literal or formatter template when tokens are present).
        /// </summary>
        [ObservableProperty]
        private string _insertText = string.Empty;

        /// <summary>
        /// Gets or sets the one-based insert position.
        /// </summary>
        [ObservableProperty]
        private decimal _position = 1;

        /// <summary>
        /// Gets or sets whether the position counts from the beginning or end of the segment.
        /// </summary>
        [ObservableProperty]
        private InserterOrigin _startFrom = InserterOrigin.Beginning;

        /// <summary>
        /// Gets or sets whether inserted text overwrites existing characters.
        /// </summary>
        [ObservableProperty]
        private bool _overwrite;

        partial void OnInsertTextChanged(string value) => _ApplyOptions();

        partial void OnPositionChanged(decimal value) => _ApplyOptions();

        partial void OnStartFromChanged(InserterOrigin value) => _ApplyOptions();

        partial void OnOverwriteChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not InserterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                InsertText = filter.Options.Text ?? string.Empty;
                Position = filter.Options.Position;
                StartFrom = filter.Options.StartFrom;
                Overwrite = filter.Options.Overwrite;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not InserterFilter filter)
            {
                return;
            }

            var options = new InserterOptions(
                Text: InsertText ?? string.Empty,
                Position: ClampToInt(Position, 1, 200),
                StartFrom: StartFrom,
                Overwrite: Overwrite
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
