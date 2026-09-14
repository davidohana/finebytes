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
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "Jpeg Tag";

        /// <summary>
        /// Property keys within <see cref="Group"/> (EXIF directory tag ids).
        /// </summary>
        public static class Key
        {
            /// <summary>XP Title.</summary>
            public const string Title = "ExifDirectory*40091";

            /// <summary>XP Subject.</summary>
            public const string Subject = "ExifDirectory*40095";

            /// <summary>XP Author.</summary>
            public const string Author = "ExifDirectory*40093";

            /// <summary>XP Keywords.</summary>
            public const string Keywords = "ExifDirectory*40094";

            /// <summary>XP Comments.</summary>
            public const string Comments = "ExifDirectory*40092";

            /// <summary>Date/time original (Date Taken).</summary>
            public const string DateTaken = "ExifDirectory*36867";

            /// <summary>Camera make.</summary>
            public const string Make = "ExifDirectory*271";

            /// <summary>Camera model.</summary>
            public const string Model = "ExifDirectory*272";

            /// <summary>Image description.</summary>
            public const string Description = "ExifDirectory*270";

            /// <summary>Artist.</summary>
            public const string Artist = "ExifDirectory*315";

            /// <summary>Image number.</summary>
            public const string ImageNumber = "ExifDirectory*37393";

            /// <summary>User comment.</summary>
            public const string UserComment = "ExifDirectory*37510";

            /// <summary>Exposure time.</summary>
            public const string Exposure = "ExifDirectory*33434";

            /// <summary>F-number.</summary>
            public const string FNumber = "ExifDirectory*33437";

            /// <summary>ISO speed ratings.</summary>
            public const string Iso = "ExifDirectory*34855";

            /// <summary>Focal length.</summary>
            public const string FocalLength = "ExifDirectory*37386";

            /// <summary>35mm-equivalent focal length.</summary>
            public const string FocalLength35mm = "ExifDirectory*41989";
        }

        /// <summary>
        /// Jpeg Tag group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new JpegExifRenameListField(Key.Title, "Title", JpegRenameListExifProperty.Title, defaultWidth: 160),
            new JpegExifRenameListField(Key.Subject, "Subject", JpegRenameListExifProperty.Subject, defaultWidth: 160),
            new JpegExifRenameListField(
                Key.Author,
                "Author",
                JpegRenameListExifProperty.Author,
                defaultWidth: 160,
                tip: JpegRenameListFieldTips.Author
            ),
            new JpegExifRenameListField(
                Key.Keywords,
                "Keywords",
                JpegRenameListExifProperty.Keywords,
                defaultWidth: 160
            ),
            new JpegExifRenameListField(
                Key.Comments,
                "Comments",
                JpegRenameListExifProperty.Comments,
                defaultWidth: 160
            ),
            new JpegExifRenameListField(
                Key.DateTaken,
                "Date/Time Taken",
                JpegRenameListExifProperty.DateTaken,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(Key.Make, "Make", JpegRenameListExifProperty.Make),
            new JpegExifRenameListField(Key.Model, "Model", JpegRenameListExifProperty.Model, defaultWidth: 60),
            new JpegExifRenameListField(
                Key.Description,
                "Description",
                JpegRenameListExifProperty.Description,
                defaultWidth: 160
            ),
            new JpegExifRenameListField(
                Key.Artist,
                "Artist",
                JpegRenameListExifProperty.Artist,
                defaultWidth: 120,
                tip: JpegRenameListFieldTips.Artist
            ),
            new JpegExifRenameListField(
                Key.ImageNumber,
                "Image Number",
                JpegRenameListExifProperty.ImageNumber,
                defaultWidth: 40
            ),
            new JpegExifRenameListField(
                Key.UserComment,
                "User Comment",
                JpegRenameListExifProperty.UserComment,
                defaultWidth: 140
            ),
            new JpegExifRenameListField(
                Key.Exposure,
                "Exposure Time",
                JpegRenameListExifProperty.Exposure,
                defaultWidth: 60
            ),
            new JpegExifRenameListField(Key.FNumber, "FNumber", JpegRenameListExifProperty.FNumber, defaultWidth: 40),
            new JpegExifRenameListField(Key.Iso, "ISO Speed Ratings", JpegRenameListExifProperty.Iso, defaultWidth: 60),
            new JpegExifRenameListField(
                Key.FocalLength,
                "Focal Length",
                JpegRenameListExifProperty.FocalLength,
                defaultWidth: 40
            ),
            new JpegExifRenameListField(
                Key.FocalLength35mm,
                "Focal Length In 35mm Film",
                JpegRenameListExifProperty.FocalLength35mm,
                defaultWidth: 80
            ),
        ];
    }
}
