namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// File paths read from the OS file clipboard for Paste.
    /// </summary>
    /// <param name="Paths">Absolute source paths in clipboard order.</param>
    /// <param name="PreferMove">
    /// When <see langword="true"/>, Preferred DropEffect was Move (Cut); otherwise Copy.
    /// </param>
    public sealed record FileClipboardPaste(IReadOnlyList<string> Paths, bool PreferMove);
}
