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
        /// Gets or sets the minimum padded width (<c>0</c> = no padding).
        /// </summary>
        [ObservableProperty]
        private decimal _width;

        /// <summary>
        /// Gets or sets the pad character shown in the editor (space stored as options <c>"1"</c>).
        /// </summary>
        [ObservableProperty]
        private string _padCharText = "0";

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
        /// Gets whether separator editing applies (prepend/append).
        /// </summary>
        public bool HasSeparatorOptions => Position is CounterPosition.Prepend or CounterPosition.Append;

        partial void OnStartChanged(decimal value) => _ApplyOptions();

        partial void OnIncrementChanged(decimal value) => _ApplyOptions();

        partial void OnWidthChanged(decimal value) => _ApplyOptions();

        partial void OnPadCharTextChanged(string value) => _ApplyOptions();

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
                Width = filter.Options.Width;
                PadCharText = _PadCharToUi(filter.Options.PadChar);
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
                Width: ClampToInt(Width, 0, 100),
                PadChar: _PadCharFromUi(PadCharText),
                Position: Position,
                Separator: Separator ?? string.Empty,
                ResetPerFolder: ResetPerFolder
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static string _PadCharToUi(string? padChar)
        {
            if (string.IsNullOrEmpty(padChar) || padChar == "0")
            {
                return "0";
            }

            if (padChar == "1")
            {
                return " ";
            }

            return padChar[0].ToString();
        }

        private static string _PadCharFromUi(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "0";
            }

            var c = text[0];
            if (c == ' ')
            {
                return "1";
            }

            if (c == '0')
            {
                return "0";
            }

            return c.ToString();
        }
    }
}
