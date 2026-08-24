namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// One filesystem or special-location row before it is bound as a File List entry.
    /// </summary>
    /// <param name="Path">Full filesystem path or File List sentinel.</param>
    /// <param name="Name">Label shown in the listing.</param>
    /// <param name="IsDirectory">Whether the row opens as a folder.</param>
    /// <param name="Length">File length in bytes, or <see langword="null"/> for folders.</param>
    /// <param name="LastWriteTime">Last write time used for Date modified sort, when known.</param>
    /// <param name="ListingGroup">This PC sort group: volumes before known folders.</param>
    internal sealed record FileListListedItem(
        string Path,
        string Name,
        bool IsDirectory,
        long? Length,
        DateTime? LastWriteTime,
        int ListingGroup = 0
    );
}
