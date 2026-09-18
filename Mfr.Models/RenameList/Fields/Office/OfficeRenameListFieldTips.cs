namespace Mfr.Models.RenameList.Fields.Office
{
    /// <summary>
    /// Clarifying tooltips for Office Document Rename List columns.
    /// </summary>
    public static class OfficeRenameListFieldTips
    {
        /// <summary>Office Author column (maps from OPC Creator).</summary>
        public const string Author =
            "Document author from Office PackageProperties Creator (OPC core prop), not a creating application.";

        /// <summary>Office Category column.</summary>
        public const string Category = "Document category from Office PackageProperties.";

        /// <summary>Office LastModifiedBy column.</summary>
        public const string LastModifiedBy =
            "Name of the user who last saved the document (PackageProperties LastModifiedBy).";
    }
}
