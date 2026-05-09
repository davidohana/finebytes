using System.Diagnostics;
using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Session
{
    /// <summary>
    /// Resolves the <c>&lt;counter&gt;</c> token (rename-list index formatting).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full form:
    /// <c>&lt;counter:initial-value,increment-by,padding,length,reset-scope&gt;</c>.
    /// Bare <c>&lt;counter&gt;</c> is equivalent to <c>&lt;counter:1,1,none,2,global&gt;</c> (no leading zeros;
    /// fourth value is ignored in <c>none</c> padding mode).
    /// </para>
    /// <para>
    /// <c>padding</c>: <c>none</c>, <c>auto</c> (width from list scope), or <c>fixed</c> (pad to <c>length</c>,
    /// minimum digit width <c>1</c>).
    /// </para>
    /// <para>
    /// <c>reset-scope</c>: <c>global</c> uses rename-list index and total counts;
    /// <c>perFolder</c> uses per-folder index and sibling counts.
    /// </para>
    /// </remarks>
    internal sealed class CounterToken : IFormatToken
    {
        /// <summary>
        /// How <c>&lt;counter&gt;</c> pads numeric output with leading zeros.
        /// </summary>
        private enum CounterPaddingMode
        {
            /// <summary>No padding.</summary>
            None,

            /// <summary>Pad to the smallest width that fits all indices in the active list scope.</summary>
            Auto,

            /// <summary>Pad to <see cref="Options.LeadingZeroesTotalLength"/> digits.</summary>
            Fixed,
        }

        private const string DefaultArg = "1,1,none,2,global";

        /// <summary>
        /// Parsed arguments for <c>&lt;counter&gt;</c>.
        /// </summary>
        /// <param name="InitialValue">Counter start value.</param>
        /// <param name="IncrementBy">Step applied per index.</param>
        /// <param name="PaddingMode">Leading-zero padding behavior.</param>
        /// <param name="LeadingZeroesTotalLength">Minimum digit width when <paramref name="PaddingMode"/> is <see cref="CounterPaddingMode.Fixed"/>.</param>
        /// <param name="ResetOnFolderChange"><c>1</c> when reset scope is <c>perFolder</c>; <c>0</c> when <c>global</c>.</param>
        private sealed record Options(
            int InitialValue,
            int IncrementBy,
            CounterPaddingMode PaddingMode,
            int LeadingZeroesTotalLength,
            int ResetOnFolderChange);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["counter"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is missing required fields or has invalid values.</exception>
        /// <exception cref="InvalidOperationException">Thrown when automatic leading-zero mode needs list counts that are not populated.</exception>
        public Func<RenameItem, string> Compile(string arg)
        {
            var options = _ParseOptions(arg);
            return item =>
            {
                var usePerFolder = options.ResetOnFolderChange == 1;
                var n = usePerFolder ? item.Original.InFolderIndex : item.Original.RenameListIndex;
                var value = options.InitialValue + ((long)options.IncrementBy * n);
                var raw = value.ToString(CultureInfo.InvariantCulture);

                var padWidth = _ResolvePadWidth(
                    options.PaddingMode,
                    options.LeadingZeroesTotalLength,
                    options.InitialValue,
                    options.IncrementBy,
                    item,
                    usePerFolder);

                if (padWidth <= 0 || padWidth <= raw.Length)
                    return raw;

                return raw.PadLeft(padWidth, '0');
            };
        }

        private Options _ParseOptions(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var normalizedArg = string.IsNullOrWhiteSpace(arg) ? DefaultArg : arg;
            var parts = normalizedArg.Split(',', StringSplitOptions.TrimEntries);
            Require.That(
                parts.Length == 5,
                $"Invalid {tokenDisplayName} token arg '{normalizedArg}'. Expected 5 comma-separated params or use '{tokenDisplayName}'.",
                nameof(arg));

            var paddingMode = _ParsePaddingMode(tokenDisplayName, parts[2]);
            var resetOnFolderChange = _ParseResetScope(tokenDisplayName, parts[4]);

            return new Options(
                InitialValue: int.Parse(parts[0], CultureInfo.InvariantCulture),
                IncrementBy: int.Parse(parts[1], CultureInfo.InvariantCulture),
                PaddingMode: paddingMode,
                LeadingZeroesTotalLength: int.Parse(parts[3], CultureInfo.InvariantCulture),
                ResetOnFolderChange: resetOnFolderChange);
        }

        /// <summary>
        /// Parses the third comma-separated field (padding keyword).
        /// </summary>
        private static CounterPaddingMode _ParsePaddingMode(string tokenDisplayName, string raw)
        {
            var key = raw.Trim();
            if (key.Equals("none", StringComparison.OrdinalIgnoreCase))
                return CounterPaddingMode.None;
            if (key.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return CounterPaddingMode.Auto;
            if (key.Equals("fixed", StringComparison.OrdinalIgnoreCase))
                return CounterPaddingMode.Fixed;

            throw new ArgumentException(
                $"{tokenDisplayName} padding '{raw}' is not supported (expected none, auto, or fixed).",
                paramName: "arg");
        }

        /// <summary>
        /// Parses the fifth comma-separated field (reset scope keyword).
        /// </summary>
        private static int _ParseResetScope(string tokenDisplayName, string raw)
        {
            var key = raw.Trim();
            if (key.Equals("global", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (key.Equals("perFolder", StringComparison.OrdinalIgnoreCase))
                return 1;

            throw new ArgumentException(
                $"{tokenDisplayName} reset scope '{raw}' is not supported (expected global or perFolder).",
                paramName: "arg");
        }

        private static int _ResolvePadWidth(
            CounterPaddingMode paddingMode,
            int leadingZeroesTotalLength,
            int start,
            int step,
            RenameItem item,
            bool usePerFolder)
        {
            switch (paddingMode)
            {
                case CounterPaddingMode.None:
                    return 0;
                case CounterPaddingMode.Auto:
                    var listCount = usePerFolder
                        ? item.Original.RenameListFolderSiblingCount
                        : item.Original.RenameListTotalCount;
                    Check.That(
                        listCount > 0,
                        "Counter token automatic padding requires rename-list counts on the item (run preview from a populated rename list).");

                    var maxIndex = Math.Max(listCount - 1, 0);
                    return _AutomaticCounterWidth(start: start, step: step, maxIndex: maxIndex);
                case CounterPaddingMode.Fixed:
                    Require.That(
                        leadingZeroesTotalLength >= 1,
                        $"Counter token fixed padding requires a positive total length (got {leadingZeroesTotalLength}).",
                        "arg");

                    return leadingZeroesTotalLength;
                default:
                    throw new UnreachableException();
            }
        }

        /// <summary>
        /// Width needed so every value <c>start + step×i</c> for <c>i</c> in <c>0…maxIndex</c> fits when formatted invariant.
        /// </summary>
        private static int _AutomaticCounterWidth(int start, int step, int maxIndex)
        {
            var v0 = start + ((long)step * 0);
            var v1 = start + ((long)step * maxIndex);
            var lo = Math.Min(v0, v1);
            var hi = Math.Max(v0, v1);
            var w0 = _InvariantFormattedLength(lo);
            var w1 = _InvariantFormattedLength(hi);
            return Math.Max(w0, w1);
        }

        private static int _InvariantFormattedLength(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture).Length;
        }
    }
}
