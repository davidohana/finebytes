using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Resolves the <c>&lt;counter:start,step,reset,width,pad&gt;</c> token.
    /// </summary>
    internal static class CounterResolver
    {
        /// <summary>
        /// Computes a per-item counter value from the supplied parameters.
        /// </summary>
        /// <param name="arg">Five comma-separated integers: start, step, reset, width, pad.</param>
        /// <param name="item">Rename item providing the index used to drive the counter.</param>
        /// <returns>The formatted counter value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="arg"/> does not provide exactly five values.</exception>
        public static string Resolve(string arg, RenameItem item)
        {
            var parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");

            var start = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var step = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var reset = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var width = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var pad = int.Parse(parts[4], CultureInfo.InvariantCulture);

            var n = reset == 1 ? item.Original.InFolderIndex : item.Original.GlobalIndex;
            var value = start + ((long)step * n);
            var raw = value.ToString(CultureInfo.InvariantCulture);
            if (width <= 0)
                return raw;

            var padChar = pad == 0 ? '0' : ' ';
            return raw.PadLeft(width, padChar);
        }
    }
}
