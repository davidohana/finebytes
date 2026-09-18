using Mfr.Filters;
using Mfr.Filters.Attributes;
using Mfr.Filters.Misc;

namespace Mfr.Engine.RenameScript
{
    /// <summary>
    /// Which filter Apply-To domains can appear in a generated rename script (path + RAHS only).
    /// </summary>
    public static class RenameScriptFilterSupport
    {
        /// <summary>
        /// Whether <paramref name="target"/> mutations can be expressed as rename-script ops.
        /// </summary>
        /// <param name="target">Filter Apply-To target.</param>
        /// <returns>
        /// <see langword="true"/> for name/path/attribute targets; <see langword="false"/> for timestamps and audio tags.
        /// </returns>
        public static bool IsSupported(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return target switch
            {
                FileNameTarget
                or FileExtensionTarget
                or FileFullNameTarget
                or FullPathTarget
                or ParentDirectoryTarget
                or AncestorFolderTarget
                or FileAttributesTarget => true,
                _ => false,
            };
        }

        /// <summary>
        /// Whether <paramref name="filter"/> only writes domains the rename script can emit.
        /// </summary>
        /// <param name="filter">Filter instance from the Filter Chain.</param>
        /// <returns>
        /// <see langword="true"/> when path/name/attrs only; <see langword="false"/> for date/tag writers
        /// (and string filters aimed at those targets).
        /// </returns>
        public static bool IsSupported(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            return filter switch
            {
                StringTargetFilter stringFilter => IsSupported(stringFilter.Target),
                AttributesSetterFilter => true,
                PathMoverFilter => true,
                IFixedApplyToFilter => false,
                _ => true,
            };
        }

        /// <summary>
        /// Lists display names of enabled chain steps that cannot be fully represented in a rename script.
        /// </summary>
        /// <param name="steps">Enabled flag, filter, and list label per step.</param>
        /// <returns>Distinct labels in encounter order.</returns>
        public static IReadOnlyList<string> GetUnsupportedEnabledStepNames(
            IEnumerable<(bool Enabled, BaseFilter Filter, string DisplayName)> steps
        )
        {
            ArgumentNullException.ThrowIfNull(steps);

            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (enabled, filter, displayName) in steps)
            {
                if (!enabled || IsSupported(filter))
                {
                    continue;
                }

                if (!seen.Add(displayName))
                {
                    continue;
                }

                names.Add(displayName);
            }

            return names;
        }
    }
}
