using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors
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

        private static char _FirstCharOrDefault(string text, char fallback)
        {
            return string.IsNullOrEmpty(text) ? fallback : text[0];
        }
    }
}
