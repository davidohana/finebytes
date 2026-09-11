using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Applies manual field overrides onto <see cref="RenameItem.Preview"/> via catalog write targets.
    /// </summary>
    internal static class RenameListFieldOverrides
    {
        /// <summary>
        /// Writes one side's manual overrides onto <paramref name="item"/>'s preview snapshot.
        /// </summary>
        /// <param name="item">Rename item whose <see cref="RenameItem.Preview"/> is updated.</param>
        /// <param name="isPreview">
        /// When <see langword="false"/>, original-side overrides (MFR7 <c>PreviewStart</c>);
        /// when <see langword="true"/>, preview-side (MFR7 <c>PreviewEnd</c>).
        /// </param>
        /// <returns>
        /// <see langword="false"/> when a write failed and the item is marked with a preview error;
        /// otherwise <see langword="true"/>.
        /// </returns>
        public static bool TryApplyToPreview(RenameItem item, bool isPreview)
        {
            ArgumentNullException.ThrowIfNull(item);

            foreach (var (key, value) in item.EnumerateOverrides(isPreview))
            {
                if (!RenameListFieldCatalog.TryGetField(key, out var field) || field.WriteTarget is not { } writeTarget)
                {
                    continue;
                }

                try
                {
                    item.Preview.SetTargetString(writeTarget, value);
                }
                catch (Exception ex)
                {
                    item.SetPreviewError(message: ex.Message, cause: ex);
                    return false;
                }
            }

            return true;
        }
    }
}
