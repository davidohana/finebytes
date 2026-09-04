namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Positioning mode for counter insertion.
    /// </summary>
    public enum CounterPosition
    {
        Prepend,
        Append,
        Replace,
    }

    /// <summary>
    /// Leading-zero padding for formatted counter values (MFR7 Leading 0's mode).
    /// </summary>
    public enum CounterLeadingZerosMode
    {
        /// <summary>No padding.</summary>
        None,

        /// <summary>Pad so every value in the active list scope shares one width.</summary>
        Automatic,

        /// <summary>Pad to <see cref="CounterOptions.CustomLength"/> digits.</summary>
        Custom,
    }

    /// <summary>
    /// Options for counter generation and placement.
    /// </summary>
    /// <param name="Start">Counter start value.</param>
    /// <param name="Step">Counter increment step.</param>
    /// <param name="LeadingZerosMode">Leading-zero padding style.</param>
    /// <param name="CustomLength">Digit width when <paramref name="LeadingZerosMode"/> is <see cref="CounterLeadingZerosMode.Custom"/>.</param>
    /// <param name="Position">Where to place the counter result.</param>
    /// <param name="Separator">Separator used for prepend/append mode.</param>
    /// <param name="ResetPerFolder">Whether to reset per folder.</param>
    public sealed record CounterOptions(
        int Start,
        int Step,
        CounterLeadingZerosMode LeadingZerosMode,
        int CustomLength,
        CounterPosition Position,
        string Separator,
        bool ResetPerFolder
    );

    /// <summary>
    /// Injects generated counter values into a segment.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Counter options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Formatting, "Counter")]
    public sealed record CounterFilter(FilterTarget Target, CounterOptions Options, StringApplyScope? ApplyScope = null)
        : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, counter prepend with name suffix).
        /// </summary>
        public CounterFilter()
            : this(
                new FilePrefixTarget(),
                new CounterOptions(
                    Start: 1,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.None,
                    CustomLength: 2,
                    Position: CounterPosition.Prepend,
                    Separator: " - ",
                    ResetPerFolder: true
                )
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Counter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            var usePerFolder = Options.ResetPerFolder;
            var n = usePerFolder ? item.Original.InFolderIndex : item.Original.RenameListIndex;
            var counter = Options.Start + ((long)Options.Step * n);
            var formatted = CounterPadding.Format(counter, _ResolvePadWidth(item, usePerFolder));

            return Options.Position switch
            {
                CounterPosition.Replace => formatted,
                CounterPosition.Prepend => formatted + Options.Separator + value,
                CounterPosition.Append => value + Options.Separator + formatted,
                _ => value,
            };
        }

        private int _ResolvePadWidth(RenameItem item, bool usePerFolder)
        {
            switch (Options.LeadingZerosMode)
            {
                case CounterLeadingZerosMode.None:
                    return 0;
                case CounterLeadingZerosMode.Automatic:
                    var listCount = usePerFolder
                        ? item.Original.RenameListFolderSiblingCount
                        : item.Original.RenameListTotalCount;
                    if (listCount <= 0)
                    {
                        return 0;
                    }

                    var maxIndex = Math.Max(listCount - 1, 0);
                    return CounterPadding.AutomaticDigitWidth(Options.Start, Options.Step, maxIndex);
                case CounterLeadingZerosMode.Custom:
                    return Math.Max(Options.CustomLength, 1);
                default:
                    return 0;
            }
        }
    }
}
