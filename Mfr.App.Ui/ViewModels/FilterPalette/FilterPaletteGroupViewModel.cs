using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels.FilterPalette
{
    /// <summary>
    /// One exclusive group button on the Available Filters toolbar.
    /// </summary>
    /// <param name="group">The filter group, or <see langword="null"/> for the All button.</param>
    /// <param name="displayName">Tooltip and accessibility name.</param>
    /// <param name="iconAssetPath">
    /// App-relative Avalonia resource path (e.g. <c>/Assets/FilterGroups/FilterGroupAll.png</c>).
    /// </param>
    public sealed partial class FilterPaletteGroupViewModel(
        FilterGroup? group,
        string displayName,
        string iconAssetPath
    ) : ViewModelBase
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
        /// Gets the MFR 7 filter-group toolbar icon.
        /// </summary>
        /// <para>
        /// Loaded on first access so constructing the palette does not require Avalonia's asset loader.
        /// </para>
        public IImage Icon => field ??= _LoadIcon(iconAssetPath);

        /// <summary>
        /// Whether this group is the active exclusive selection.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Reads the PNG from the Mfr.App.Ui avares pack.
        /// </summary>
        private static Bitmap _LoadIcon(string assetPath)
        {
            var uri = new Uri($"avares://Mfr.App.Ui{assetPath}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
    }
}
