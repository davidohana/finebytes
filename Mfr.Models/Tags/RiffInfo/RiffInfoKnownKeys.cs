namespace Mfr.Models.Tags.RiffInfo
{
    /// <summary>
    /// RIFF LIST INFO fourCC keys modeled for read, write, and semantic projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unknown on-disk chunks survive field-patch by omission; only keys listed in <see cref="All"/> are
    /// loaded or written. Ids follow the standard INFO fourCC spellings (not TagLib façade aliases).
    /// </para>
    /// </remarks>
    public static class RiffInfoKnownKeys
    {
        /// <summary>Title chunk (<c>INAM</c>).</summary>
        public const string Title = "INAM";

        /// <summary>Album / product chunk (<c>IPRD</c>).</summary>
        public const string Album = "IPRD";

        /// <summary>Artist / performers chunk (<c>IART</c>).</summary>
        public const string Artist = "IART";

        /// <summary>Genre chunk (<c>IGNR</c>).</summary>
        public const string Genre = "IGNR";

        /// <summary>Comment chunk (<c>ICMT</c>).</summary>
        public const string Comment = "ICMT";

        /// <summary>Copyright chunk (<c>ICOP</c>).</summary>
        public const string Copyright = "ICOP";

        /// <summary>Creation date / year chunk (<c>ICRD</c>).</summary>
        public const string Year = "ICRD";

        /// <summary>Track number chunk (<c>ITRK</c>).</summary>
        public const string Track = "ITRK";

        /// <summary>
        /// Known keys in stable display order for Metadata field I/O.
        /// </summary>
        public static IReadOnlyList<string> All { get; } =
        [Title, Album, Artist, Genre, Comment, Copyright, Year, Track];
    }
}
