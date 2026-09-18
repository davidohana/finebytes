namespace Mfr.Models.RenameList.Fields.Epub
{
    /// <summary>
    /// EPUB Dublin Core Info properties shared by formatter tokens and Rename List columns.
    /// </summary>
    internal enum EpubDocumentField
    {
        /// <summary>dc:title.</summary>
        Title,

        /// <summary>dc:creator (author / organization).</summary>
        Creator,

        /// <summary>dc:publisher.</summary>
        Publisher,

        /// <summary>dc:language.</summary>
        Language,

        /// <summary>dc:date (literal string).</summary>
        Date,

        /// <summary>Preferred dc:identifier.</summary>
        Identifier,

        /// <summary>dc:subject.</summary>
        Subject,

        /// <summary>dc:description.</summary>
        Description,
    }
}
