using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;file-date&gt;</c> format token.
    /// </summary>
    internal sealed partial class FileDateFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        private const string DefaultFormat = "dd-MM-yyyy";

        /// <summary>
        /// Gets date-kind rows (<c>creation</c>/<c>lastWrite</c>/<c>lastAccess</c>).
        /// </summary>
        public static IReadOnlyList<FormatTokenChoice> KindChoices { get; } =
        [new("creation", "Creation"), new("lastWrite", "Last write"), new("lastAccess", "Last access")];

        /// <summary>
        /// Initializes the editor from existing <c>format,kind</c> arguments.
        /// </summary>
        /// <param name="args">Positional format and date-kind.</param>
        public FileDateFormatTokenEditorViewModel(string? args)
            : base("Date/Time", "file-date")
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return;
            }

            var trimmed = args.Trim();
            var lastComma = trimmed.LastIndexOf(',');
            if (lastComma < 0)
            {
                Format = trimmed;
                return;
            }

            var formatPart = trimmed[..lastComma].Trim();
            var kindPart = trimmed[(lastComma + 1)..].Trim();
            if (formatPart.Length > 0)
            {
                Format = formatPart;
            }

            Kind =
                KindChoices.FirstOrDefault(c => string.Equals(c.Value, kindPart, StringComparison.OrdinalIgnoreCase))
                ?? KindChoices[0];
        }

        /// <summary>
        /// Gets or sets the .NET date format string.
        /// </summary>
        [ObservableProperty]
        private string _format = DefaultFormat;

        /// <summary>
        /// Gets or sets the filesystem timestamp kind.
        /// </summary>
        [ObservableProperty]
        private FormatTokenChoice _kind = KindChoices[0];

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var format = string.IsNullOrWhiteSpace(Format) ? DefaultFormat : Format.Trim();
            return CanonicalName + ":" + format + "," + Kind.Value;
        }
    }
}
