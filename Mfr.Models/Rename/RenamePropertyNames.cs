namespace Mfr.Models.Rename
{
    /// <summary>
    /// Canonical <see cref="RenamePropertyChange.Property"/> names shared by GO change rows, undo OldValue apply, and IsUndoable.
    /// </summary>
    public static class RenamePropertyNames
    {
        /// <summary>File name without extension.</summary>
        public const string Prefix = "Prefix";

        /// <summary>Extension without leading dot.</summary>
        public const string Extension = "Extension";

        /// <summary>Parent directory path.</summary>
        public const string DirectoryPath = "DirectoryPath";

        /// <summary>Filesystem attributes.</summary>
        public const string Attributes = "Attributes";

        /// <summary>Creation timestamp.</summary>
        public const string CreationTime = "CreationTime";

        /// <summary>Last-write timestamp.</summary>
        public const string LastWriteTime = "LastWriteTime";

        /// <summary>Last-access timestamp.</summary>
        public const string LastAccessTime = "LastAccessTime";

        /// <summary>
        /// Tag Remover strip-all flag (logged at GO; unrestorable on undo).
        /// </summary>
        public const string StripAllEmbeddedTagsOnCommit = "StripAllEmbeddedTagsOnCommit";

        /// <summary>
        /// Prefix for embedded audio-tag block/field rows (<c>AudioTag.Block.{Block}[.{Field}]</c>).
        /// </summary>
        public const string AudioTagBlockPrefix = "AudioTag.Block.";

        /// <summary>ID3v1 block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Id3v1 = nameof(Tags.AudioTagBlockKind.Id3v1);

        /// <summary>ID3v2 block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Id3v2 = nameof(Tags.AudioTagBlockKind.Id3v2);

        /// <summary>Xiph/Vorbis block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Xiph = nameof(Tags.AudioTagBlockKind.Xiph);

        /// <summary>APE block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Ape = nameof(Tags.AudioTagBlockKind.Ape);

        /// <summary>RIFF INFO block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string RiffInfo = nameof(Tags.AudioTagBlockKind.RiffInfo);

        /// <summary>Apple/iTunes block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Apple = nameof(Tags.AudioTagBlockKind.Apple);

        /// <summary>ASF block name after <see cref="AudioTagBlockPrefix"/>.</summary>
        public const string Asf = nameof(Tags.AudioTagBlockKind.Asf);

        /// <summary>Full property name for an audio-tag block (presence row) or block root.</summary>
        /// <param name="blockName">Block segment (e.g. <see cref="Id3v2"/>).</param>
        /// <returns><see cref="AudioTagBlockPrefix"/> + <paramref name="blockName"/>.</returns>
        public static string AudioTagBlock(string blockName)
        {
            return AudioTagBlockPrefix + blockName;
        }
    }
}
