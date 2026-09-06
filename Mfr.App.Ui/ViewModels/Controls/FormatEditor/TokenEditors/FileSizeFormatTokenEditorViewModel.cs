using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;file-size&gt;</c> format token.
    /// </summary>
    internal sealed partial class FileSizeFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Unit combo row for the first <c>file-size</c> argument segment.
        /// </summary>
        public sealed record UnitChoice(string Value, string Label)
        {
            /// <inheritdoc />
            public override string ToString()
            {
                return Label;
            }
        }

        /// <summary>
        /// Gets unit rows (auto / bytes / KB / MB / GB).
        /// </summary>
        public static IReadOnlyList<UnitChoice> UnitChoices { get; } =
        [new("auto", "Auto"), new("b", "Bytes"), new("kb", "KB"), new("mb", "MB"), new("gb", "GB")];

        /// <summary>
        /// Initializes the editor from existing <c>unit</c> or <c>unit,decimals</c> arguments.
        /// </summary>
        /// <param name="args">Positional unit and optional decimals.</param>
        public FileSizeFormatTokenEditorViewModel(string? args)
            : base("Size", "file-size")
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return;
            }

            var parts = args.Split(',', 2, StringSplitOptions.TrimEntries);
            var unitArg = parts.Length > 0 ? parts[0] : string.Empty;
            Unit = _ResolveUnit(unitArg);

            if (
                parts.Length > 1
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimals)
            )
            {
                Decimals = Math.Max(0, decimals);
            }
        }

        /// <summary>
        /// Gets or sets the size unit.
        /// </summary>
        [ObservableProperty]
        private UnitChoice _unit = UnitChoices[0];

        /// <summary>
        /// Gets or sets fractional digit count.
        /// </summary>
        [ObservableProperty]
        private decimal _decimals;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var decimals = Math.Max(0, (int)Decimals);
            var isDefaultAuto = string.Equals(Unit.Value, "auto", StringComparison.OrdinalIgnoreCase) && decimals == 0;
            if (isDefaultAuto)
            {
                return CanonicalName;
            }

            if (decimals == 0)
            {
                return CanonicalName + ":" + Unit.Value;
            }

            return CanonicalName + ":" + Unit.Value + "," + NamedFormatOptionsBuilder.FormatInt(decimals);
        }

        /// <summary>
        /// Maps a positional unit argument to a combo row (aliases <c>bytes</c> → <c>b</c>).
        /// </summary>
        private static UnitChoice _ResolveUnit(string unitArg)
        {
            if (unitArg.Length == 0 || string.Equals(unitArg, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return UnitChoices[0];
            }

            var isBytes =
                string.Equals(unitArg, "bytes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(unitArg, "b", StringComparison.OrdinalIgnoreCase);
            if (isBytes)
            {
                return UnitChoices[1];
            }

            return UnitChoices.FirstOrDefault(c => string.Equals(c.Value, unitArg, StringComparison.OrdinalIgnoreCase))
                ?? UnitChoices[0];
        }
    }
}
