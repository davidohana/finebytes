using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Filters;

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

            _DecodeSignedPosition(
                NamedFormatOptionsBuilder.GetInt(keyToValue, "start", 1),
                defaultWhenZero: 1,
                out var fromPosition,
                out var fromAnchor
            );
            FromPosition = fromPosition;
            FromAnchorOption = fromAnchor;

            _DecodeSignedPosition(
                NamedFormatOptionsBuilder.GetInt(keyToValue, "end", -1),
                defaultWhenZero: -1,
                out var toPosition,
                out var toAnchor
            );
            ToPosition = toPosition;
            ToAnchorOption = toAnchor;

            Source = NamedFormatOptionsBuilder.GetString(keyToValue, "source", DefaultSource);
        }

        /// <summary>
        /// Gets left/right choices for from/to position anchors.
        /// </summary>
        public IReadOnlyList<StringScopeAnchorOption> AnchorOptions => StringScopeAnchorOption.All;

        /// <summary>
        /// Gets or sets the inclusive 1-based from position (positive magnitude).
        /// </summary>
        [ObservableProperty]
        private decimal _fromPosition = 1;

        /// <summary>
        /// Gets or sets whether <see cref="FromPosition"/> counts from the left or right.
        /// </summary>
        [ObservableProperty]
        private StringScopeAnchorOption _fromAnchorOption = StringScopeAnchorOption.All[0];

        /// <summary>
        /// Gets or sets the inclusive 1-based to position (positive magnitude).
        /// </summary>
        [ObservableProperty]
        private decimal _toPosition = 1;

        /// <summary>
        /// Gets or sets whether <see cref="ToPosition"/> counts from the left or right.
        /// </summary>
        [ObservableProperty]
        private StringScopeAnchorOption _toAnchorOption = StringScopeAnchorOption.All[1];

        /// <summary>
        /// Gets or sets the nested source format string (<c>source</c>).
        /// </summary>
        [ObservableProperty]
        private string _source = DefaultSource;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var start = _EncodeSignedPosition(FromPosition, FromAnchorOption);
            var end = _EncodeSignedPosition(ToPosition, ToAnchorOption);
            var source = string.IsNullOrWhiteSpace(Source) ? DefaultSource : Source.Trim();
            var args = NamedFormatOptionsBuilder.Join(
                ("start", NamedFormatOptionsBuilder.FormatInt(start)),
                ("end", NamedFormatOptionsBuilder.FormatInt(end)),
                ("source", source)
            );
            return CanonicalName + ":" + args;
        }

        private static void _DecodeSignedPosition(
            int signed,
            int defaultWhenZero,
            out decimal position,
            out StringScopeAnchorOption anchor
        )
        {
            var value = signed == 0 ? defaultWhenZero : signed;
            position = Math.Max(1, Math.Abs(value));
            anchor = StringScopeAnchorOption.FromAnchor(value < 0 ? StringScopeAnchor.Right : StringScopeAnchor.Left);
        }

        private static int _EncodeSignedPosition(decimal position, StringScopeAnchorOption anchor)
        {
            var n = Math.Max(1, (int)position);
            return anchor.Anchor == StringScopeAnchor.Right ? -n : n;
        }
    }
}
