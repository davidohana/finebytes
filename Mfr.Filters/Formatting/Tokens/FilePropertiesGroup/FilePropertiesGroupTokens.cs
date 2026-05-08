namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Registers tokens that read filesystem properties (date, size, drive, label, count).
    /// </summary>
    internal static class FilePropertiesGroupTokens
    {
        /// <summary>
        /// Registers all File Properties group tokens into <paramref name="registry"/>.
        /// </summary>
        /// <param name="registry">Registry to populate.</param>
        public static void Register(FormatTokenRegistry registry)
        {
            registry.Register("file-date", FileDateResolver.Resolve);
            registry.Register("file-size", FileSizeResolver.Resolve);
            registry.Register("drive-letter", DriveLetterResolver.Resolve);
            registry.Register("label", LabelResolver.Resolve);
            registry.Register("file-count", FileCountResolver.Resolve);
        }
    }
}
