namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// One save-dialog type filter (label + glob patterns).
    /// </summary>
    /// <param name="Name">Filter label shown in the dialog (e.g. <c>CSV files</c>).</param>
    /// <param name="Patterns">Glob patterns (e.g. <c>*.csv</c>).</param>
    public sealed record SaveFilePickType(string Name, IReadOnlyList<string> Patterns);

    /// <summary>
    /// Options for <see cref="FileSavePicker"/> / Rename List <c>PickSavePathAsync</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prefer <see cref="FileTypes"/> for multi-type dialogs. When <see cref="FileTypes"/> is null or empty,
    /// the picker builds a single primary filter from <see cref="FileTypeName"/> plus an All-files entry.
    /// </para>
    /// </remarks>
    public sealed class SaveFilePickOptions
    {
        /// <summary>
        /// Dialog title.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Default extension without a leading dot (e.g. <c>csv</c> or <c>bat</c>).
        /// </summary>
        public required string DefaultExtension { get; init; }

        /// <summary>
        /// Optional suggested file name (without forcing the extension).
        /// </summary>
        public string? SuggestedFileName { get; init; }

        /// <summary>
        /// Primary filter label when <see cref="FileTypes"/> is omitted (e.g. <c>Text files</c>).
        /// </summary>
        public string? FileTypeName { get; init; }

        /// <summary>
        /// Explicit type filters (e.g. bat + ps1 + all). When set and non-empty, replaces the single-type default.
        /// </summary>
        public IReadOnlyList<SaveFilePickType>? FileTypes { get; init; }
    }
}
