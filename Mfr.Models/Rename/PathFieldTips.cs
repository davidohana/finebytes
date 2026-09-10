namespace Mfr.Models.Rename
{
    /// <summary>
    /// Clarifying tooltips for path / file-name fields when labels alone are ambiguous.
    /// </summary>
    public static class PathFieldTips
    {
        /// <summary>Tooltip for <see cref="PathFieldLabels.FileName"/>.</summary>
        public const string FileName =
            "Name without extension (e.g. MySong for MySong.mp3). Distinct from Full File Name.";

        /// <summary>Tooltip for <see cref="PathFieldLabels.FullFileName"/>.</summary>
        public const string FullFileName = "File name including extension (e.g. MySong.mp3).";

        /// <summary>Tooltip for <see cref="PathFieldLabels.ParentDirectory"/>.</summary>
        public const string ParentDirectory =
            "Absolute path of the containing folder. Distinct from Parent Folder (segment name only).";

        /// <summary>Tooltip for <see cref="PathFieldLabels.ParentFolder"/>.</summary>
        public const string ParentFolder =
            "Immediate parent folder name only (not the full path). Optional level selects grandparents.";

        /// <summary>Tooltip for <see cref="PathFieldLabels.FileNameNumericValue"/>.</summary>
        public const string FileNameNumericValue =
            "First run of digits in the full file name (leading zeros stripped). No digits → 0.";
    }
}
