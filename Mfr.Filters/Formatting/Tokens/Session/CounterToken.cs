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
    /// Full form uses named options (order-independent), for example
    /// <c>&lt;counter:initial=1, step=1, padding=none, length=2, resetScope=global&gt;</c>.
    /// Bare <c>&lt;counter&gt;</c> uses the same defaults (no leading zeros; <c>length</c> is ignored when <c>padding</c> is <c>none</c>).
    /// </para>
    /// <para>
    /// Options: <c>initial</c>, <c>step</c>, <c>padding</c>, <c>length</c>, <c>resetScope</c> (case-insensitive keys).
    /// Whitespace around <c>=</c> and commas is ignored. Omitted options use the defaults above.
    /// </para>
    /// <para>
    /// <c>padding</c>: <c>none</c>, <c>auto</c> (width from list scope), or <c>fixed</c> (pad to <c>length</c>,
    /// minimum digit width <c>1</c>).
    /// </para>
    /// <para>
    /// <c>resetScope</c>: <c>global</c> uses rename-list index and total counts;
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

        /// <summary>
        /// Padding keywords for <c>padding=…</c> (<c>none</c>, <c>auto</c>, <c>fixed</c>).
        /// </summary>
        private static readonly Dictionary<string, CounterPaddingMode> _keywordToPaddingMode = new(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = CounterPaddingMode.None,
            ["auto"] = CounterPaddingMode.Auto,
            ["fixed"] = CounterPaddingMode.Fixed,
        };

        /// <summary>
        /// Reset-scope keywords for <c>resetScope=…</c> (<c>global</c> vs <c>perFolder</c>).
        /// </summary>
        private static readonly Dictionary<string, int> _keywordToResetOnFolderChange = new(StringComparer.OrdinalIgnoreCase)
        {
            ["global"] = 0,
            ["perFolder"] = 1,
        };

        /// <summary>
        /// Default option values when <c>&lt;counter&gt;</c> has no argument or an option is omitted.
        /// </summary>
        private static readonly Dictionary<string, string> _counterDefaults = new(StringComparer.OrdinalIgnoreCase)
        {
            ["initial"] = "1",
            ["step"] = "1",
            ["padding"] = "none",
            ["length"] = "2",
            ["resetScope"] = "global",
        };

        private static readonly string[] _counterOptionKeys = ["initial", "step", "padding", "length", "resetScope"];

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
        public Formatter Compile(string tokenArgs)
        {
            var options = _ParseOptions(tokenArgs);
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

        private Options _ParseOptions(string tokenArgs)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Dictionary<string, string> merged;
            if (string.IsNullOrWhiteSpace(tokenArgs))
            {
                merged = new Dictionary<string, string>(_counterDefaults, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                var parsed = FormatOptionsParsing.ParseNamedKeyValuePairs(tokenArgs.Trim(), tokenDisplayName);
                FormatOptionsParsing.RequireKnownOptionKeysOnly(parsed, tokenDisplayName, _counterOptionKeys, nameof(tokenArgs));

                merged = new Dictionary<string, string>(_counterDefaults, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in parsed)
                    merged[kv.Key] = kv.Value;
            }

            var paddingMode = _ParsePaddingMode(tokenDisplayName, merged["padding"]);
            var resetOnFolderChange = _ParseResetScope(tokenDisplayName, merged["resetScope"]);

            return new Options(
                InitialValue: int.Parse(merged["initial"], CultureInfo.InvariantCulture),
                IncrementBy: int.Parse(merged["step"], CultureInfo.InvariantCulture),
                PaddingMode: paddingMode,
                LeadingZeroesTotalLength: int.Parse(merged["length"], CultureInfo.InvariantCulture),
                ResetOnFolderChange: resetOnFolderChange);
        }

        /// <summary>
        /// Parses <c>padding=…</c>.
        /// </summary>
        private static CounterPaddingMode _ParsePaddingMode(string tokenDisplayName, string raw)
        {
            if (!_keywordToPaddingMode.TryGetValue(raw.Trim(), out var mode))
                throw new ArgumentException(
                    $"{tokenDisplayName} padding '{raw}' is not supported (expected {FormatOptionsParsing.FormatExpectedKeywords(_keywordToPaddingMode.Keys)}).",
                    paramName: nameof(raw));

            return mode;
        }

        /// <summary>
        /// Parses <c>resetScope=…</c>.
        /// </summary>
        private static int _ParseResetScope(string tokenDisplayName, string raw)
        {
            if (!_keywordToResetOnFolderChange.TryGetValue(raw.Trim(), out var resetOnFolderChange))
                throw new ArgumentException(
                    $"{tokenDisplayName} reset scope '{raw}' is not supported (expected {FormatOptionsParsing.FormatExpectedKeywords(_keywordToResetOnFolderChange.Keys)}).",
                    paramName: nameof(raw));

            return resetOnFolderChange;
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
