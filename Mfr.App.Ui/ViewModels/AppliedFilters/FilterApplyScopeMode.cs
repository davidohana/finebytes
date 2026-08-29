namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Apply-scope mode for string-target filters in Filter Options.
    /// </summary>
    public enum FilterApplyScopeMode
    {
        /// <summary>Transform the entire target string.</summary>
        Whole,

        /// <summary>Transform an inclusive substring only.</summary>
        Substring,

        /// <summary>Transform one token after splitting by a separator.</summary>
        Token,
    }
}
