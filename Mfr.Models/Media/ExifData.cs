namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only MetadataExtractor EXIF snapshot for formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from the same MetadataExtractor open as <see cref="ImageProperties"/>.
    /// Never written back. Missing text fields are <see langword="null"/>; missing
    /// <see cref="DateTaken"/> is <see langword="null"/>. Extended tags are flattened into
    /// <see cref="TagToDescription"/> at map time (no raw MetadataExtractor directories are stored).
    /// </para>
    /// </remarks>
    public sealed record ExifData
    {
        /// <summary>
        /// Canonical <c>&lt;exif:source,name&gt;</c> directory aliases, in error-message order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lookup is case-insensitive. Typed GPS lat/lon is a later slice; the <c>GPS</c> alias
        /// still stores string descriptions in <see cref="TagToDescription"/>.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<string> SourceAliases { get; } =
        [
            "Exif", // IFD0: Make, Model, Artist, Description, Windows XP fields
            "ExifSub", // SubIFD: DateTimeOriginal, exposure, F-number, ISO, focal, User Comment
            "GPS", // GPS IFD strings only; typed lat/lon is later
            "IPTC", // IPTC-IIM captions/keywords/byline
            "Canon", // Canon makernote
            "Casio", // Casio Type1/Type2 makernotes (first tag wins)
            "FujiFilm", // Fujifilm makernote
            "Nikon", // Nikon Type1/Type2 makernotes (first tag wins)
            "Olympus", // Olympus makernote
            "Interop", // Interoperability IFD
            "Thumb", // Thumbnail IFD; DateTime is not copied into DateTaken
        ];

        private static readonly HashSet<string> _sourceAliasToIsKnown =
            new(SourceAliases, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns whether <paramref name="source"/> is a known <see cref="SourceAliases"/> value.
        /// </summary>
        /// <param name="source">Directory alias from an <c>&lt;exif:source,name&gt;</c> token.</param>
        /// <returns><see langword="true"/> when <paramref name="source"/> matches an alias (case-insensitive).</returns>
        public static bool IsKnownSourceAlias(string source)
        {
            return source.Length > 0 && _sourceAliasToIsKnown.Contains(source);
        }

        /// <summary>
        /// Gets DateTimeOriginal from EXIF SubIFD (tag 36867), or <see langword="null"/> when absent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Kind is <see cref="DateTimeKind.Unspecified"/>. No fallback to DateTimeDigitized or IFD0 DateTime.
        /// </para>
        /// </remarks>
        public DateTime? DateTaken { get; init; }

        /// <summary>
        /// Gets the camera manufacturer (IFD0 Make), or <see langword="null"/> when absent.
        /// </summary>
        public string? Make { get; init; }

        /// <summary>
        /// Gets the camera model (IFD0 Model), or <see langword="null"/> when absent.
        /// </summary>
        public string? Model { get; init; }

        /// <summary>
        /// Gets the exposure-time description (SubIFD), or <see langword="null"/> when absent.
        /// </summary>
        public string? Exposure { get; init; }

        /// <summary>
        /// Gets the F-number description (SubIFD), or <see langword="null"/> when absent.
        /// </summary>
        public string? FNumber { get; init; }

        /// <summary>
        /// Gets the ISO speed description (SubIFD), or <see langword="null"/> when absent.
        /// </summary>
        public string? Iso { get; init; }

        /// <summary>
        /// Gets the focal-length description (SubIFD), or <see langword="null"/> when absent.
        /// </summary>
        public string? FocalLength { get; init; }

        /// <summary>
        /// Gets the 35mm-equivalent focal-length description (SubIFD), or <see langword="null"/> when absent.
        /// </summary>
        public string? FocalLength35mm { get; init; }

        /// <summary>
        /// Gets Windows XP Title (IFD0), or <see langword="null"/> when absent.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets Windows XP Subject (IFD0), or <see langword="null"/> when absent.
        /// </summary>
        public string? Subject { get; init; }

        /// <summary>
        /// Gets Windows XP Author (IFD0), or <see langword="null"/> when absent.
        /// </summary>
        public string? Author { get; init; }

        /// <summary>
        /// Gets Windows XP Keywords (IFD0), or <see langword="null"/> when absent.
        /// </summary>
        public string? Keywords { get; init; }

        /// <summary>
        /// Gets Windows XP Comment (IFD0), or <see langword="null"/> when absent.
        /// </summary>
        public string? Comments { get; init; }

        /// <summary>
        /// Gets IFD0 Artist, or <see langword="null"/> when absent.
        /// </summary>
        public string? Artist { get; init; }

        /// <summary>
        /// Gets SubIFD User Comment, or <see langword="null"/> when absent.
        /// </summary>
        public string? UserComment { get; init; }

        /// <summary>
        /// Gets IFD0 Image Description, or <see langword="null"/> when absent.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets flattened EXIF/IPTC/makernote descriptions keyed by <c>{alias}/{tag-name}</c> and
        /// <c>{alias}/{tag-id}</c> (case-insensitive). Empty when none.
        /// </summary>
        public IReadOnlyDictionary<string, string> TagToDescription { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
