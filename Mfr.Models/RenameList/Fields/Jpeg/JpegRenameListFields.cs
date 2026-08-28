namespace Mfr.Models.RenameList.Fields.Jpeg
{
    /// <summary>
    /// All MFR7 Jpeg Tag Rename List fields (read-only EXIF originals).
    /// </summary>
    public static class JpegRenameListFields
    {
        /// <summary>
        /// MFR7 Jpeg property group id.
        /// </summary>
        public const string Group = "Jpeg";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "Jpeg Tag";

        /// <summary>
        /// Jpeg Tag group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new JpegExifRenameListField(
                "ExifDirectory*40091",
                "Title",
                JpegRenameListExifProperty.Title,
                defaultWidth: 100
            ),
            new JpegExifRenameListField(
                "ExifDirectory*40095",
                "Subject",
                JpegRenameListExifProperty.Subject,
                defaultWidth: 100
            ),
            new JpegExifRenameListField(
                "ExifDirectory*40093",
                "Author",
                JpegRenameListExifProperty.Author,
                defaultWidth: 100
            ),
            new JpegExifRenameListField(
                "ExifDirectory*40094",
                "Keywords",
                JpegRenameListExifProperty.Keywords,
                defaultWidth: 100
            ),
            new JpegExifRenameListField(
                "ExifDirectory*40092",
                "Comments",
                JpegRenameListExifProperty.Comments,
                defaultWidth: 100
            ),
            new JpegExifRenameListField(
                "ExifDirectory*36867",
                "Date/Time Taken",
                JpegRenameListExifProperty.DateTaken,
                defaultWidth: 60
            ),
            new JpegExifRenameListField("ExifDirectory*271", "Make", JpegRenameListExifProperty.Make),
            new JpegExifRenameListField(
                "ExifDirectory*272",
                "Model",
                JpegRenameListExifProperty.Model,
                defaultWidth: 60
            ),
            new JpegExifRenameListField("ExifDirectory*270", "Description", JpegRenameListExifProperty.Description),
            new JpegExifRenameListField(
                "ExifDirectory*315",
                "Artist",
                JpegRenameListExifProperty.Artist,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(
                "ExifDirectory*37393",
                "Image Number",
                JpegRenameListExifProperty.ImageNumber,
                defaultWidth: 40
            ),
            new JpegExifRenameListField(
                "ExifDirectory*37510",
                "User Comment",
                JpegRenameListExifProperty.UserComment,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(
                "ExifDirectory*33434",
                "Exposure Time",
                JpegRenameListExifProperty.Exposure,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(
                "ExifDirectory*33437",
                "FNumber",
                JpegRenameListExifProperty.FNumber,
                defaultWidth: 40
            ),
            new JpegExifRenameListField(
                "ExifDirectory*34855",
                "ISO Speed Ratings",
                JpegRenameListExifProperty.Iso,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(
                "ExifDirectory*37386",
                "Focal Length",
                JpegRenameListExifProperty.FocalLength,
                defaultWidth: 40
            ),
            new JpegExifRenameListField(
                "ExifDirectory*41989",
                "Focal Length In 35mm Film",
                JpegRenameListExifProperty.FocalLength35mm,
                defaultWidth: 80
            ),
        ];
    }
}
