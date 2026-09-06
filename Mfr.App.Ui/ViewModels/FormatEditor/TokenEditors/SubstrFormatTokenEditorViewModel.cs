using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;substr&gt;</c> format token.
    /// </summary>
    internal sealed partial class SubstrFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        private const string DefaultSource = "<file-name>";

        /// <summary>
        /// Initializes the editor from existing named arguments.
        /// </summary>
        /// <param name="args">Named <c>start</c>/<c>end</c>/<c>source</c> options.</param>
        public SubstrFormatTokenEditorViewModel(string? args)
            : base("Substring", "substr")
        {
            if (!NamedFormatOptionsBuilder.TryParse(args, out var keyToValue) || keyToValue.Count == 0)
            {
                return;
            }

            Start = NamedFormatOptionsBuilder.GetInt(keyToValue, "start", 1);
            End = NamedFormatOptionsBuilder.GetInt(keyToValue, "end", -1);
            Source = NamedFormatOptionsBuilder.GetString(keyToValue, "source", DefaultSource);
        }

        /// <summary>
        /// Gets or sets the 1-based start position (<c>start</c>; negative = from right).
        /// </summary>
        [ObservableProperty]
        private decimal _start = 1;

        /// <summary>
        /// Gets or sets the 1-based end position (<c>end</c>; negative = from right).
        /// </summary>
        [ObservableProperty]
        private decimal _end = -1;

        /// <summary>
        /// Gets or sets the nested source format string (<c>source</c>).
        /// </summary>
        [ObservableProperty]
        private string _source = DefaultSource;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var start = (int)Start;
            var end = (int)End;
            if (start == 0)
            {
                start = 1;
            }

            if (end == 0)
            {
                end = -1;
            }

            var source = string.IsNullOrWhiteSpace(Source) ? DefaultSource : Source.Trim();
            var args = NamedFormatOptionsBuilder.Join(
                ("start", NamedFormatOptionsBuilder.FormatInt(start)),
                ("end", NamedFormatOptionsBuilder.FormatInt(end)),
                ("source", source)
            );
            return CanonicalName + ":" + args;
        }
    }
}
