using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters.Formatting.Tokens.Generators;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;now&gt;</c> format token.
    /// </summary>
    internal sealed partial class NowFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Placeholder when Format is empty (engine uses <see cref="NowTokenDefaults.Format"/>).
        /// </summary>
        public const string EmptyFormatPlaceholder = "(" + NowTokenDefaults.Format + " when empty)";

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
        /// Gets or sets the optional .NET format string (empty → <see cref="NowTokenDefaults.Format"/>).
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
