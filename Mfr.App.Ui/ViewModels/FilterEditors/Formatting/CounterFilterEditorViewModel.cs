using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Formatting
{
    /// <summary>
    /// Filter Configuration editor for <see cref="CounterFilter"/>.
    /// </summary>
    internal sealed partial class CounterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CounterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the Leading 0's mode choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<CounterLeadingZerosMode> LeadingZerosModes { get; } =
        [CounterLeadingZerosMode.None, CounterLeadingZerosMode.Automatic, CounterLeadingZerosMode.Custom];

        /// <summary>
        /// Gets or sets the counter start value (index 0).
        /// </summary>
        [ObservableProperty]
        private decimal _start = 1;

        /// <summary>
        /// Gets or sets the increment applied per file index.
        /// </summary>
        [ObservableProperty]
        private decimal _increment = 1;

        /// <summary>
        /// Gets or sets the leading-zero padding mode.
        /// </summary>
        [ObservableProperty]
        private CounterLeadingZerosMode _leadingZerosMode = CounterLeadingZerosMode.None;

        /// <summary>
        /// Gets or sets the digit width when mode is Custom.
        /// </summary>
        [ObservableProperty]
        private decimal _customLength = 2;

        /// <summary>
        /// Gets or sets where the formatted counter is placed relative to the segment.
        /// </summary>
        [ObservableProperty]
        private CounterPosition _position = CounterPosition.Prepend;

        /// <summary>
        /// Gets or sets the separator between counter and segment for prepend/append.
        /// </summary>
        [ObservableProperty]
        private string _separator = " - ";

        /// <summary>
        /// Gets or sets whether the counter uses per-folder indexes.
        /// </summary>
        [ObservableProperty]
        private bool _resetPerFolder = true;

        /// <summary>
        /// Gets whether the custom length spinner is shown.
        /// </summary>
        public bool HasCustomLength => LeadingZerosMode == CounterLeadingZerosMode.Custom;

        /// <summary>
        /// Gets whether separator editing applies (prepend/append).
        /// </summary>
        public bool HasSeparatorOptions => Position is CounterPosition.Prepend or CounterPosition.Append;

        partial void OnStartChanged(decimal value) => _ApplyOptions();

        partial void OnIncrementChanged(decimal value) => _ApplyOptions();

        partial void OnLeadingZerosModeChanged(CounterLeadingZerosMode value)
        {
            OnPropertyChanged(nameof(HasCustomLength));
            _ApplyOptions();
        }

        partial void OnCustomLengthChanged(decimal value) => _ApplyOptions();

        partial void OnPositionChanged(CounterPosition value)
        {
            OnPropertyChanged(nameof(HasSeparatorOptions));
            _ApplyOptions();
        }

        partial void OnSeparatorChanged(string value) => _ApplyOptions();

        partial void OnResetPerFolderChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not CounterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                Start = filter.Options.Start;
                Increment = filter.Options.Step;
                LeadingZerosMode = filter.Options.LeadingZerosMode;
                CustomLength = Math.Max(filter.Options.CustomLength, 1);
                Position = filter.Options.Position;
                Separator = filter.Options.Separator ?? string.Empty;
                ResetPerFolder = filter.Options.ResetPerFolder;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not CounterFilter filter)
            {
                return;
            }

            var options = new CounterOptions(
                Start: ClampToInt(Start, -999999, 999999),
                Step: ClampToInt(Increment, -99999, 99999),
                LeadingZerosMode: LeadingZerosMode,
                CustomLength: ClampToInt(CustomLength, 1, 100),
                Position: Position,
                Separator: Separator ?? string.Empty,
                ResetPerFolder: ResetPerFolder
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
