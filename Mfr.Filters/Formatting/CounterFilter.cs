using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Positioning mode for counter insertion.
    /// </summary>
    public enum CounterPosition
    {
        Prepend,
        Append,
        Replace
    }

    /// <summary>
    /// Options for counter generation and placement.
    /// </summary>
    /// <param name="Start">Counter start value.</param>
    /// <param name="Step">Counter increment step.</param>
    /// <param name="Width">Output width for padding.</param>
    /// <param name="PadChar">Pad character selector.</param>
    /// <param name="Position">Where to place the counter result.</param>
    /// <param name="Separator">Separator used for prepend/append mode.</param>
    /// <param name="ResetPerFolder">Whether to reset per folder.</param>
    public sealed record CounterOptions(
        int Start,
        int Step,
        int Width,
        string PadChar,
        CounterPosition Position,
        string Separator,
        bool ResetPerFolder);

    /// <summary>
    /// Injects generated counter values into a segment.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Counter options.</param>
    public sealed record CounterFilter(
        FilterTarget Target,
        CounterOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Counter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            var n = Options.ResetPerFolder ? item.Original.InFolderIndex : item.Original.GlobalIndex;
            var counter = Options.Start + ((long)Options.Step * n);

            var pad = Options.PadChar switch
            {
                "0" => '0',
                "1" => ' ',
                _ => string.IsNullOrEmpty(Options.PadChar) ? '0' : Options.PadChar[0]
            };

            var raw = counter.ToString(CultureInfo.InvariantCulture);
            var formatted = Options.Width > 0 ? raw.PadLeft(Options.Width, pad) : raw;

            return Options.Position switch
            {
                CounterPosition.Replace => formatted,
                CounterPosition.Prepend => formatted + Options.Separator + value,
                CounterPosition.Append => value + Options.Separator + formatted,
                _ => value
            };
        }
    }
}
