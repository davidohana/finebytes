using Mfr.Models;
using Mfr.Models.Filters;

namespace Mfr.Core
{
    /// <summary>
    /// Provides rename-item helpers for filter-based preview name transformation.
    /// </summary>
    public static class RenameItemExtensions
    {
        /// <summary>
        /// Applies enabled filters to update the item's preview file name.
        /// </summary>
        /// <param name="item">The rename item receiving transformed preview metadata.</param>
        /// <param name="filters">The configured filters to apply in order.</param>
        public static void ApplyFilters(this RenameItem item, IReadOnlyList<Filter> filters)
        {
            item.ResetPreview();
            var prefix = item.Original.Prefix;
            var extension = item.Original.Extension;

            foreach (var filter in filters)
            {
                if (!filter.Enabled)
                {
                    continue;
                }

                if (filter.Target is not FileNameTarget fileTarget)
                {
                    throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filter.Type}' got '{filter.Target.Family}'.");
                }

                var mode = fileTarget.FileNameMode;
                var segment = mode switch
                {
                    FileNameTargetMode.Prefix => prefix,
                    FileNameTargetMode.Extension => extension,
                    FileNameTargetMode.Full => prefix + extension,
                    _ => throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.")
                };

                var transformed = filter.Apply(segment, item);

                switch (mode)
                {
                    case FileNameTargetMode.Prefix:
                        prefix = transformed;
                        break;
                    case FileNameTargetMode.Extension:
                        extension = transformed;
                        break;
                    case FileNameTargetMode.Full:
                        var fullName = Path.GetFileName(transformed);
                        extension = Path.GetExtension(fullName);
                        prefix = Path.GetFileNameWithoutExtension(fullName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.");
                }
            }

            item.SetPreviewName(prefix, extension);
        }
    }
}
