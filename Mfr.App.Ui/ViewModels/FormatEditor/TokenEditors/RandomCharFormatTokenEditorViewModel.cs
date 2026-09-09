using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;random-char&gt;</c> format token.
    /// </summary>
    internal sealed partial class RandomCharFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Initializes the editor from existing <c>low,high</c> arguments (empty → <c>A,Z</c>).
        /// </summary>
        /// <param name="args">Comma-separated low/high characters.</param>
        public RandomCharFormatTokenEditorViewModel(string? args)
            : base("Random Char", "random-char")
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return;
            }

            var parts = args.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length >= 1 && parts[0].Length > 0)
            {
                Low = parts[0][0].ToString();
            }

            if (parts.Length >= 2 && parts[1].Length > 0)
            {
                High = parts[1][0].ToString();
            }
        }

        /// <summary>
        /// Gets or sets the low endpoint character (first code unit used).
        /// </summary>
        [ObservableProperty]
        private string _low = "A";

        /// <summary>
        /// Gets or sets the high endpoint character (first code unit used).
        /// </summary>
        [ObservableProperty]
        private string _high = "Z";

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var low = _FirstCharOrDefault(Low, 'A');
            var high = _FirstCharOrDefault(High, 'Z');
            return CanonicalName + ":" + low + "," + high;
        }

        /// <summary>
        /// Sets the range to digits <c>0</c>–<c>9</c> (MFR7 Digits sample).
        /// </summary>
        [RelayCommand]
        public void DigitsSample()
        {
            _ApplySample("0", "9");
        }

        /// <summary>
        /// Sets the range to uppercase letters <c>A</c>–<c>Z</c> (MFR7 Upper Letters sample).
        /// </summary>
        [RelayCommand]
        public void UpperLettersSample()
        {
            _ApplySample("A", "Z");
        }

        /// <summary>
        /// Sets the range to lowercase letters <c>a</c>–<c>z</c> (MFR7 Lower Letters sample).
        /// </summary>
        [RelayCommand]
        public void LowerLettersSample()
        {
            _ApplySample("a", "z");
        }

        private void _ApplySample(string low, string high)
        {
            Low = low;
            High = high;
        }

        private static char _FirstCharOrDefault(string text, char fallback)
        {
            return string.IsNullOrEmpty(text) ? fallback : text[0];
        }
    }
}
