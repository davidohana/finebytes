using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;now&gt;</c> format token.
    /// </summary>
    internal sealed partial class NowFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Initializes the editor from existing arguments (empty → bare <c>now</c>).
        /// </summary>
        /// <param name="args">Optional .NET date/time format string.</param>
        public NowFormatTokenEditorViewModel(string? args)
            : base("Now", "now")
        {
            Format = string.IsNullOrWhiteSpace(args) ? string.Empty : args.Trim();
        }

        /// <summary>
        /// Gets or sets the optional .NET format string (empty → ISO-8601 default).
        /// </summary>
        [ObservableProperty]
        private string _format = string.Empty;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var trimmed = Format.Trim();
            return trimmed.Length == 0 ? CanonicalName : CanonicalName + ":" + trimmed;
        }
    }
}
