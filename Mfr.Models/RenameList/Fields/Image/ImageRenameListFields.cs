namespace Mfr.Models.RenameList.Fields.Image
{
    /// <summary>
    /// All MFR7 Image Rename List fields (read-only originals).
    /// </summary>
    public static class ImageRenameListFields
    {
        /// <summary>
        /// MFR7 Image property group id.
        /// </summary>
        public const string Group = "Image";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "Image";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>Image format.</summary>
            public const string Format = "Format";

            /// <summary>Width in pixels.</summary>
            public const string Width = "Width";

            /// <summary>Height in pixels.</summary>
            public const string Height = "Height";

            /// <summary>Bit depth.</summary>
            public const string BitDepth = "BitDepth";

            /// <summary>Horizontal resolution (DPI).</summary>
            public const string HorzRes = "HorzRes";

            /// <summary>Vertical resolution (DPI).</summary>
            public const string VertRes = "VertRes";

            /// <summary>Frame count.</summary>
            public const string Frames = "Frames";
        }

        /// <summary>
        /// Image group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new ImagePropertyRenameListField(Key.Format, "Format", ImagePropertyField.Format, defaultWidth: 60),
            new ImagePropertyRenameListField(Key.Width, "Width", ImagePropertyField.Width),
            new ImagePropertyRenameListField(Key.Height, "Height", ImagePropertyField.Height),
            new ImagePropertyRenameListField(Key.BitDepth, "Bit Depth", ImagePropertyField.BitDepth),
            new ImagePropertyRenameListField(
                Key.HorzRes,
                "Horizontal Resolution",
                ImagePropertyField.HorizontalResolutionDpi
            ),
            new ImagePropertyRenameListField(
                Key.VertRes,
                "Vertical Resolution",
                ImagePropertyField.VerticalResolutionDpi
            ),
            new ImagePropertyRenameListField(Key.Frames, "Frames Count", ImagePropertyField.FrameCount),
        ];
    }
}
