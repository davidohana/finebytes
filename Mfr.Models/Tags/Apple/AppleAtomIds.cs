namespace Mfr.Models.Tags.Apple
{
    /// <summary>
    /// Four-byte Apple/iTunes atom type identifiers used by overlay read/merge.
    /// </summary>
    public static class AppleAtomIds
    {
        /// <summary>Title atom (<c>©nam</c>).</summary>
        public static ReadOnlySpan<byte> Title => [0xA9, (byte)'n', (byte)'a', (byte)'m'];

        /// <summary>Album atom (<c>©alb</c>).</summary>
        public static ReadOnlySpan<byte> Album => [0xA9, (byte)'a', (byte)'l', (byte)'b'];

        /// <summary>Artist atom (<c>©ART</c>).</summary>
        public static ReadOnlySpan<byte> Artist => [0xA9, (byte)'A', (byte)'R', (byte)'T'];

        /// <summary>Album artist atom (<c>aART</c>).</summary>
        public static ReadOnlySpan<byte> AlbumArtist => [(byte)'a', (byte)'A', (byte)'R', (byte)'T'];

        /// <summary>Composer atom (<c>©wrt</c>).</summary>
        public static ReadOnlySpan<byte> Composer => [0xA9, (byte)'w', (byte)'r', (byte)'t'];

        /// <summary>Genre atom (<c>©gen</c>).</summary>
        public static ReadOnlySpan<byte> Genre => [0xA9, (byte)'g', (byte)'e', (byte)'n'];

        /// <summary>Comment atom (<c>©cmt</c>).</summary>
        public static ReadOnlySpan<byte> Comment => [0xA9, (byte)'c', (byte)'m', (byte)'t'];

        /// <summary>Lyrics atom (<c>©lyr</c>).</summary>
        public static ReadOnlySpan<byte> Lyrics => [0xA9, (byte)'l', (byte)'y', (byte)'r'];

        /// <summary>Copyright atom (<c>cprt</c>).</summary>
        public static ReadOnlySpan<byte> Copyright => [(byte)'c', (byte)'p', (byte)'r', (byte)'t'];

        /// <summary>Grouping atom (<c>©grp</c>).</summary>
        public static ReadOnlySpan<byte> Grouping => [0xA9, (byte)'g', (byte)'r', (byte)'p'];

        /// <summary>Release day/year atom (<c>©day</c>).</summary>
        public static ReadOnlySpan<byte> Day => [0xA9, (byte)'d', (byte)'a', (byte)'y'];

        /// <summary>Conductor atom (<c>cond</c>).</summary>
        public static ReadOnlySpan<byte> Conductor => [(byte)'c', (byte)'o', (byte)'n', (byte)'d'];
    }
}
