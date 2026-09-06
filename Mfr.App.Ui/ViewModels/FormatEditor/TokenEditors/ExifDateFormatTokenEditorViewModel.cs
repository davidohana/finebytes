using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;exif-date&gt;</c> format token.
    /// </summary>
    internal sealed partial class ExifDateFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        private const string DefaultFormat = "dd-MM-yyyy";

        /// <summary>
        /// Initializes the editor from existing arguments (empty → <c>dd-MM-yyyy</c>).
        /// </summary>
        /// <param name="args">Required .NET date format string.</param>
        public ExifDateFormatTokenEditorViewModel(string? args)
            : base("Date Picture Taken", "exif-date")
        {
            Format = string.IsNullOrWhiteSpace(args) ? DefaultFormat : args.Trim();
        }

        /// <summary>
        /// Gets or sets the .NET date format string.
        /// </summary>
        [ObservableProperty]
        private string _format = DefaultFormat;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var trimmed = Format.Trim();
            if (trimmed.Length == 0)
            {
                trimmed = DefaultFormat;
            }

            return CanonicalName + ":" + trimmed;
        }
    }
}
