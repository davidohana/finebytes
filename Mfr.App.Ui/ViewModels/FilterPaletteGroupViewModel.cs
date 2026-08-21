using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// One exclusive group button on the Available Filters toolbar.
    /// </summary>
    /// <param name="group">The filter group, or <see langword="null"/> for the All button.</param>
    /// <param name="displayName">Tooltip and accessibility name.</param>
    /// <param name="iconKey">Resource key for the toolbar icon geometry.</param>
    public sealed partial class FilterPaletteGroupViewModel(FilterGroup? group, string displayName, string iconKey)
        : ViewModelBase
    {
        /// <summary>
        /// Gets the filter group, or <see langword="null"/> for All.
        /// </summary>
        public FilterGroup? Group { get; } = group;

        /// <summary>
        /// Gets the tooltip / accessibility name.
        /// </summary>
        public string DisplayName { get; } = displayName;

        /// <summary>
        /// Gets the <c>StreamGeometry</c> resource key for the icon.
        /// </summary>
        public string IconKey { get; } = iconKey;

        /// <summary>
        /// Whether this group is the active exclusive selection.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;
    }
}
