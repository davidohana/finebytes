using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;counter&gt;</c> format token.
    /// </summary>
    internal sealed partial class CounterFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Gets padding mode rows for <c>padding=…</c>.
        /// </summary>
        public static IReadOnlyList<FormatTokenChoice> PaddingChoices { get; } =
        [new("none", "None"), new("auto", "Auto"), new("fixed", "Fixed")];

        /// <summary>
        /// Gets reset-scope rows for <c>resetScope=…</c>.
        /// </summary>
        public static IReadOnlyList<FormatTokenChoice> ResetScopeChoices { get; } =
        [new("global", "Global"), new("perFolder", "Per folder")];

        /// <summary>
        /// Initializes the editor from existing token arguments (empty → defaults).
        /// </summary>
        /// <param name="args">Argument text after <c>counter:</c>, or empty for defaults.</param>
        public CounterFormatTokenEditorViewModel(string? args)
            : base("Counter", "counter")
        {
            _ApplyArgs(args);
        }

        /// <summary>
        /// Gets or sets the counter start value (<c>initial</c>).
        /// </summary>
        [ObservableProperty]
        private decimal _initial = 1;

        /// <summary>
        /// Gets or sets the per-index step (<c>step</c>).
        /// </summary>
        [ObservableProperty]
        private decimal _step = 1;

        /// <summary>
        /// Gets or sets the padding mode (<c>padding</c>).
        /// </summary>
        [ObservableProperty]
        private FormatTokenChoice _padding = PaddingChoices[0];

        /// <summary>
        /// Gets or sets the fixed pad width (<c>length</c>).
        /// </summary>
        [ObservableProperty]
        private decimal _length = 2;

        /// <summary>
        /// Gets or sets the reset scope (<c>resetScope</c>).
        /// </summary>
        [ObservableProperty]
        private FormatTokenChoice _resetScope = ResetScopeChoices[0];

        /// <summary>
        /// Gets whether the length spinner applies (fixed padding).
        /// </summary>
        public bool IsLengthEnabled => string.Equals(Padding.Value, "fixed", StringComparison.OrdinalIgnoreCase);

        partial void OnPaddingChanged(FormatTokenChoice value) => OnPropertyChanged(nameof(IsLengthEnabled));

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var args = NamedFormatOptionsBuilder.Join(
                ("initial", NamedFormatOptionsBuilder.FormatInt((int)Initial)),
                ("step", NamedFormatOptionsBuilder.FormatInt((int)Step)),
                ("padding", Padding.Value),
                ("length", NamedFormatOptionsBuilder.FormatInt((int)Length)),
                ("resetScope", ResetScope.Value)
            );
            return CanonicalName + ":" + args;
        }

        private void _ApplyArgs(string? args)
        {
            if (!NamedFormatOptionsBuilder.TryParse(args, out var keyToValue))
            {
                return;
            }

            Initial = NamedFormatOptionsBuilder.GetInt(keyToValue, "initial", 1);
            Step = NamedFormatOptionsBuilder.GetInt(keyToValue, "step", 1);
            Length = Math.Max(1, NamedFormatOptionsBuilder.GetInt(keyToValue, "length", 2));

            var paddingRaw = NamedFormatOptionsBuilder.GetString(keyToValue, "padding", "none");
            Padding =
                PaddingChoices.FirstOrDefault(c =>
                    string.Equals(c.Value, paddingRaw, StringComparison.OrdinalIgnoreCase)
                ) ?? PaddingChoices[0];

            var resetRaw = NamedFormatOptionsBuilder.GetString(keyToValue, "resetScope", "global");
            ResetScope =
                ResetScopeChoices.FirstOrDefault(c =>
                    string.Equals(c.Value, resetRaw, StringComparison.OrdinalIgnoreCase)
                ) ?? ResetScopeChoices[0];
        }
    }
}
