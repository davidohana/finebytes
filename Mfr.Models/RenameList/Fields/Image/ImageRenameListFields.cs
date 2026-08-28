namespace Mfr.Models.RenameList.Fields.Image
{
    /// <summary>
    /// All MFR7 Image Rename List fields (read-only originals).
    /// </summary>
    public static class ImageRenameListFields
    {
        /// <summary>
        /// Image group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new ImagePropertyRenameListField("Format", "Format", ImageRenameListProperty.Format, defaultWidth: 60),
            new ImagePropertyRenameListField("Width", "Width", ImageRenameListProperty.Width),
            new ImagePropertyRenameListField("Height", "Height", ImageRenameListProperty.Height),
            new ImagePropertyRenameListField("BitDepth", "Bit Depth", ImageRenameListProperty.BitDepth),
            new ImagePropertyRenameListField(
                "HorzRes",
                "Horizontal Resolution",
                ImageRenameListProperty.HorizontalResolutionDpi
            ),
            new ImagePropertyRenameListField(
                "VertRes",
                "Vertical Resolution",
                ImageRenameListProperty.VerticalResolutionDpi
            ),
            new ImagePropertyRenameListField("Frames", "Frames Count", ImageRenameListProperty.FrameCount),
        ];
    }
}
