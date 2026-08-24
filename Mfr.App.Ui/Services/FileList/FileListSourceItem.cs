namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Minimal File List row identity used when resolving Rename List add sources.
    /// </summary>
    /// <param name="FullPath">Full filesystem path or File List sentinel.</param>
    /// <param name="IsDirectory">Whether the row is a folder.</param>
    internal readonly record struct FileListSourceItem(string FullPath, bool IsDirectory);
}
