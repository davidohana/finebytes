namespace Mfr.App.Ui
{
    /// <summary>
    /// Parsed desktop UI startup arguments.
    /// </summary>
    /// <param name="Sources">Positional source paths or wildcards for Rename List seeding.</param>
    /// <param name="InitialFolder">Optional File List start directory; <c>null</c> when omitted.</param>
    /// <param name="IncludeFiles">Add-files override; <c>null</c> when omitted (use Options prefs).</param>
    /// <param name="IncludeFolders">Add-folders override; <c>null</c> when omitted (use Options prefs).</param>
    /// <param name="IncludeSubdirs">Recursive add override; <c>null</c> when omitted (use Options prefs).</param>
    /// <param name="IncludeHidden">Include-hidden override; <c>null</c> when omitted (use Options prefs).</param>
    internal sealed record UiStartupArgs(
        IReadOnlyList<string> Sources,
        string? InitialFolder,
        bool? IncludeFiles,
        bool? IncludeFolders,
        bool? IncludeSubdirs,
        bool? IncludeHidden
    )
    {
        /// <summary>
        /// Empty parse result (no sources, no browse folder, no modifier overrides).
        /// </summary>
        public static UiStartupArgs Empty { get; } =
            new(
                Sources: [],
                InitialFolder: null,
                IncludeFiles: null,
                IncludeFolders: null,
                IncludeSubdirs: null,
                IncludeHidden: null
            );

        /// <summary>
        /// Whether any startup intent was supplied (sources, browse folder, or add modifiers).
        /// </summary>
        public bool HasAnyIntent =>
            Sources.Count > 0
            || InitialFolder is not null
            || IncludeFiles is not null
            || IncludeFolders is not null
            || IncludeSubdirs is not null
            || IncludeHidden is not null;
    }
}
