using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;token&gt;</c> meta format token.
    /// </summary>
    internal sealed partial class TokenFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        private const string DefaultSource = "<file-name>";

        /// <summary>
        /// Initializes the editor from existing named arguments.
        /// </summary>
        /// <param name="args">Named token-extract options.</param>
        public TokenFormatTokenEditorViewModel(string? args)
            : base("Token", "token")
        {
            if (!NamedFormatOptionsBuilder.TryParse(args, out var keyToValue) || keyToValue.Count == 0)
            {
                return;
            }

            TokenNumber = Math.Max(1, NamedFormatOptionsBuilder.GetInt(keyToValue, "tokenNumber", 1));
            Separator = NamedFormatOptionsBuilder.GetString(keyToValue, "separator", "-");
            IncludeNext = NamedFormatOptionsBuilder.GetBool(keyToValue, "includeNext", false);
            IncludePrev = NamedFormatOptionsBuilder.GetBool(keyToValue, "includePrev", false);
            Source = NamedFormatOptionsBuilder.GetString(keyToValue, "source", DefaultSource);
        }

        /// <summary>
        /// Gets or sets the 1-based token index (<c>tokenNumber</c>).
        /// </summary>
        [ObservableProperty]
        private decimal _tokenNumber = 1;

        /// <summary>
        /// Gets or sets the split separator (<c>separator</c>).
        /// </summary>
        [ObservableProperty]
        private string _separator = "-";

        /// <summary>
        /// Gets or sets whether to include following parts (<c>includeNext</c>).
        /// </summary>
        [ObservableProperty]
        private bool _includeNext;

        /// <summary>
        /// Gets or sets whether to include preceding parts (<c>includePrev</c>).
        /// </summary>
        [ObservableProperty]
        private bool _includePrev;

        /// <summary>
        /// Gets or sets the nested source format string (<c>source</c>).
        /// </summary>
        [ObservableProperty]
        private string _source = DefaultSource;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var tokenNumber = Math.Max(1, (int)TokenNumber);
            var separator = string.IsNullOrEmpty(Separator) ? "-" : Separator;
            var source = string.IsNullOrWhiteSpace(Source) ? DefaultSource : Source.Trim();
            var args = NamedFormatOptionsBuilder.Join(
                ("tokenNumber", NamedFormatOptionsBuilder.FormatInt(tokenNumber)),
                ("separator", separator),
                ("includeNext", NamedFormatOptionsBuilder.FormatBool(IncludeNext)),
                ("includePrev", NamedFormatOptionsBuilder.FormatBool(IncludePrev)),
                ("source", source)
            );
            return CanonicalName + ":" + args;
        }
    }
}
