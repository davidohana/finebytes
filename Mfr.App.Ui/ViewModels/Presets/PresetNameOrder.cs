using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Shared display order for preset names (ordinal-ignore-case, then ordinal).
    /// </summary>
    public static class PresetNameOrder
    {
        /// <summary>
        /// Orders presets by <see cref="FilterPreset.Name"/> for lists and suggestions.
        /// </summary>
        /// <param name="presets">Presets to order.</param>
        /// <returns>Presets sorted by name.</returns>
        public static IOrderedEnumerable<FilterPreset> ByName(IEnumerable<FilterPreset> presets)
        {
            ArgumentNullException.ThrowIfNull(presets);
            return presets
                .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(preset => preset.Name, StringComparer.Ordinal);
        }
    }
}
