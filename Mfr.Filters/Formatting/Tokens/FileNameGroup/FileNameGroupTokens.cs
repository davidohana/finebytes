namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Registers tokens that read the file/folder name and path components.
    /// </summary>
    internal static class FileNameGroupTokens
    {
        /// <summary>
        /// Registers all File Name group tokens into <paramref name="registry"/>.
        /// </summary>
        /// <param name="registry">Registry to populate.</param>
        public static void Register(FormatTokenRegistry registry)
        {
            registry.Register("file-name", item => item.Original.Prefix);
            registry.Register("file-extension", item => item.Original.Extension);
            registry.RegisterAlias("ext", "file-extension");
            registry.Register("full-name", item => item.Original.Prefix + item.Original.Extension);
            registry.Register("full-path", item => item.Original.FullPath);
            registry.Register("parent-folder", ParentFolderResolver.Resolve);
        }
    }
}
