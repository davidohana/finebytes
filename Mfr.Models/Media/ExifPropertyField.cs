namespace Mfr.Models.Media
{
    /// <summary>
    /// Semantic EXIF fields shared by formatter tokens and Jpeg Rename List columns.
    /// </summary>
    public enum ExifPropertyField
    {
        /// <summary>Windows XP Title.</summary>
        Title,

        /// <summary>Windows XP Subject.</summary>
        Subject,

        /// <summary>Windows XP Author.</summary>
        Author,

        /// <summary>Windows XP Keywords.</summary>
        Keywords,

        /// <summary>Windows XP Comments.</summary>
        Comments,

        /// <summary>DateTimeOriginal.</summary>
        DateTaken,

        /// <summary>Camera make.</summary>
        Make,

        /// <summary>Camera model.</summary>
        Model,

        /// <summary>Image description.</summary>
        Description,

        /// <summary>IFD0 Artist.</summary>
        Artist,

        /// <summary>SubIFD image number (tag 37393).</summary>
        ImageNumber,

        /// <summary>SubIFD user comment.</summary>
        UserComment,

        /// <summary>Exposure time.</summary>
        Exposure,

        /// <summary>F-number.</summary>
        FNumber,

        /// <summary>ISO speed ratings.</summary>
        Iso,

        /// <summary>Focal length.</summary>
        FocalLength,

        /// <summary>Focal length in 35mm film.</summary>
        FocalLength35mm,

        /// <summary>GPS latitude (decimal degrees).</summary>
        GpsLatitude,

        /// <summary>GPS longitude (decimal degrees).</summary>
        GpsLongitude,
    }
}
