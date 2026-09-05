using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Formatting
{
    /// <summary>
    /// Filter Configuration editor for <see cref="TokenMoverFilter"/>.
    /// </summary>
    internal sealed partial class TokenMoverFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public TokenMoverFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the substring that separates tokens.
        /// </summary>
        [ObservableProperty]
        private string _delimiter = "-";

        /// <summary>
        /// Gets or sets the one-based index of the token to move.
        /// </summary>
        [ObservableProperty]
        private decimal _tokenNumber = 1;

        /// <summary>
        /// Gets or sets the offset in token positions (positive toward the end, negative toward the start).
        /// </summary>
        [ObservableProperty]
        private decimal _moveBy = 1;

        partial void OnDelimiterChanged(string value) => _ApplyOptions();

        partial void OnTokenNumberChanged(decimal value) => _ApplyOptions();

        partial void OnMoveByChanged(decimal value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not TokenMoverFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                Delimiter = filter.Options.Delimiter;
                TokenNumber = filter.Options.TokenNumber;
                MoveBy = filter.Options.MoveBy;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not TokenMoverFilter filter)
            {
                return;
            }

            var options = new TokenMoverOptions(
                Delimiter: Delimiter,
                TokenNumber: ClampToInt(TokenNumber, 1, 1000),
                MoveBy: ClampToInt(MoveBy, -999, 999)
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
