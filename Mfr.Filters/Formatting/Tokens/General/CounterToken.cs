using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Resolves the <c>&lt;counter&gt;</c> token (legacy Magic File Renamer counter parameters).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full form:
    /// <c>&lt;counter:initial-value,increment-by,leading-zeroes-mode,leading-zeroes-total-length,reset-on-folder-change&gt;</c>.
    /// Bare <c>&lt;counter&gt;</c> is equivalent to <c>&lt;counter:1,1,0,2,0&gt;</c> (no leading zeros;
    /// fourth value is ignored in that mode).
    /// </para>
    /// <para>
    /// <c>leading-zeroes-mode</c>: <c>0</c> = none, <c>1</c> = automatic width from list scope,
    /// <c>2</c> = fixed width from the fourth parameter (minimum <c>1</c>).
    /// </para>
    /// <para>
    /// <c>reset-on-folder-change</c>: <c>1</c> uses per-folder ordering index and folder-local list counts;
    /// <c>0</c> uses global ordering index and total rename-list count.
    /// </para>
    /// </remarks>
    internal sealed class CounterToken : IFormatToken
    {
        private const string DefaultArg = "1,1,0,2,0";

        /// <summary>
        /// Parsed arguments for <c>&lt;counter&gt;</c>.
        /// </summary>
        /// <param name="InitialValue">Counter start value.</param>
        /// <param name="IncrementBy">Step applied per index.</param>
        /// <param name="LeadingZeroesMode"><c>0</c> none, <c>1</c> automatic width, <c>2</c> custom width.</param>
        /// <param name="LeadingZeroesTotalLength">Minimum digit width when mode is custom.</param>
        /// <param name="ResetOnFolderChange"><c>1</c> uses per-folder index.</param>
        private sealed record Options(
            int InitialValue,
            int IncrementBy,
            int LeadingZeroesMode,
            int LeadingZeroesTotalLength,
            int ResetOnFolderChange);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["counter"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are missing, invalid, or list sizing was not populated.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var options = _ParseOptions(arg);
            var usePerFolder = options.ResetOnFolderChange == 1;
            var n = usePerFolder ? item.Original.InFolderIndex : item.Original.GlobalIndex;
            var value = options.InitialValue + ((long)options.IncrementBy * n);
            var raw = value.ToString(CultureInfo.InvariantCulture);

            var padWidth = _ResolvePadWidth(
                options.LeadingZeroesMode,
                options.LeadingZeroesTotalLength,
                options.InitialValue,
                options.IncrementBy,
                item,
                usePerFolder);

            if (padWidth <= 0 || padWidth <= raw.Length)
                return raw;

            return raw.PadLeft(padWidth, '0');
        }

        private Options _ParseOptions(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var normalizedArg = string.IsNullOrWhiteSpace(arg) ? DefaultArg : arg;
            var parts = normalizedArg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                throw new InvalidOperationException(
                    $"Invalid {tokenDisplayName} token arg '{normalizedArg}'. Expected 5 comma-separated params or use '{tokenDisplayName}'.");
            }

            return new Options(
                InitialValue: int.Parse(parts[0], CultureInfo.InvariantCulture),
                IncrementBy: int.Parse(parts[1], CultureInfo.InvariantCulture),
                LeadingZeroesMode: int.Parse(parts[2], CultureInfo.InvariantCulture),
                LeadingZeroesTotalLength: int.Parse(parts[3], CultureInfo.InvariantCulture),
                ResetOnFolderChange: int.Parse(parts[4], CultureInfo.InvariantCulture));
        }

        private static int _ResolvePadWidth(
            int leadingZeroesMode,
            int leadingZeroesTotalLength,
            int start,
            int step,
            RenameItem item,
            bool usePerFolder)
        {
            switch (leadingZeroesMode)
            {
                case 0:
                    return 0;
                case 1:
                    var listCount = usePerFolder
                        ? item.Original.RenameListFolderSiblingCount
                        : item.Original.RenameListTotalCount;
                    if (listCount <= 0)
                    {
                        throw new InvalidOperationException(
                            "Counter token automatic leading-zero mode requires rename-list counts on the item (run preview from a populated rename list).");
                    }

                    var maxIndex = Math.Max(listCount - 1, 0);
                    return _AutomaticCounterWidth(start: start, step: step, maxIndex: maxIndex);
                case 2:
                    if (leadingZeroesTotalLength < 1)
                    {
                        throw new InvalidOperationException(
                            $"Counter token custom leading-zero mode requires a positive total length (got {leadingZeroesTotalLength}).");
                    }

                    return leadingZeroesTotalLength;
                default:
                    throw new InvalidOperationException(
                        $"Invalid counter leading-zeroes-mode '{leadingZeroesMode}' (expected 0, 1, or 2).");
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
