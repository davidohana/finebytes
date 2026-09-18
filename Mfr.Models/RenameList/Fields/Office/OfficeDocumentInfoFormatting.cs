using System.Diagnostics;
using System.Globalization;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Office
{
    /// <summary>
    /// Formats <see cref="OfficeDocumentInfo"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class OfficeDocumentInfoFormatting
    {
        /// <summary>
        /// Formats one Office property for display.
        /// </summary>
        /// <param name="office">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. Created/Modified use Invariant <see cref="DateTimeOffset"/>
        /// <c>"G"</c> for Token and <see cref="RenameListFieldDisplay.FormatFileDate"/> on
        /// <see cref="DateTimeOffset.LocalDateTime"/> for Grid; other arms are identical.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(
            OfficeDocumentInfo? office,
            OfficeDocumentField field,
            PropertyDisplayContext context
        )
        {
            if (office is null)
            {
                return string.Empty;
            }

            return field switch
            {
                OfficeDocumentField.Title => RenameListFieldDisplay.FormatOptionalText(office.Title),
                OfficeDocumentField.Author => RenameListFieldDisplay.FormatOptionalText(office.Author),
                OfficeDocumentField.Subject => RenameListFieldDisplay.FormatOptionalText(office.Subject),
                OfficeDocumentField.Keywords => RenameListFieldDisplay.FormatOptionalText(office.Keywords),
                OfficeDocumentField.Category => RenameListFieldDisplay.FormatOptionalText(office.Category),
                OfficeDocumentField.Description => RenameListFieldDisplay.FormatOptionalText(office.Description),
                OfficeDocumentField.LastModifiedBy => RenameListFieldDisplay.FormatOptionalText(office.LastModifiedBy),
                OfficeDocumentField.Created => _FormatDate(office.Created, context),
                OfficeDocumentField.Modified => _FormatDate(office.Modified, context),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Formats Created/Modified: Token keeps Invariant <see cref="DateTimeOffset"/>
        /// <c>"G"</c>; Grid uses <see cref="RenameListFieldDisplay.FormatFileDate"/> on local time.
        /// </summary>
        private static string _FormatDate(DateTimeOffset? value, PropertyDisplayContext context)
        {
            if (value is not { } date)
            {
                return string.Empty;
            }

            return context switch
            {
                PropertyDisplayContext.Token => date.ToString("G", CultureInfo.InvariantCulture),
                PropertyDisplayContext.Grid => RenameListFieldDisplay.FormatFileDate(date.LocalDateTime),
                _ => throw new UnreachableException(),
            };
        }
    }
}
