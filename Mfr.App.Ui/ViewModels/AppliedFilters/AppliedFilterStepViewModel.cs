using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// One row in the Applied Filters list.
    /// </summary>
    public sealed partial class AppliedFilterStepViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new applied-filter step.
        /// </summary>
        /// <param name="displayName">Unique list label (catalog display name plus duplicate suffix).</param>
        /// <param name="filter">Filter configuration for this step.</param>
        public AppliedFilterStepViewModel(string displayName, BaseFilter filter)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentNullException.ThrowIfNull(filter);

            DisplayName = displayName;
            Filter = filter;
            _RefreshLabels(filter);
        }

        /// <summary>
        /// Gets whether this step participates when the chain runs.
        /// </summary>
        [ObservableProperty]
        private bool _enabled = true;

        /// <summary>
        /// Gets the unique list label for this step.
        /// </summary>
        [ObservableProperty]
        private string _displayName;

        /// <summary>
        /// Gets the catalog display name for this filter type (stable after rename).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        private string _catalogDisplayName = string.Empty;

        /// <summary>
        /// Gets the Apply-To label for string-target filters; otherwise empty.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        private string _applyToLabel = string.Empty;

        /// <summary>
        /// Gets the list subtitle: catalog type, plus Apply-To when present.
        /// </summary>
        public string Subtitle
        {
            get
            {
                if (string.IsNullOrEmpty(ApplyToLabel))
                {
                    return CatalogDisplayName;
                }

                return $"{CatalogDisplayName} · {ApplyToLabel}";
            }
        }

        /// <summary>
        /// Gets the filter configuration for this step.
        /// </summary>
        public BaseFilter Filter { get; private set; }

        /// <summary>
        /// Replaces the filter configuration and refreshes catalog / Apply-To labels.
        /// </summary>
        /// <param name="filter">New filter instance for this step.</param>
        internal void SetFilter(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            if (ReferenceEquals(Filter, filter))
            {
                return;
            }

            Filter = filter;
            _RefreshLabels(filter);
            OnPropertyChanged(nameof(Filter));
        }

        /// <summary>
        /// Replaces the list label shown for this step.
        /// </summary>
        /// <param name="displayName">New display name.</param>
        internal void SetDisplayName(string displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            DisplayName = displayName;
        }

        private void _RefreshLabels(BaseFilter filter)
        {
            CatalogDisplayName = FilterCatalog
                .Entries.Single(entry => entry.FilterType == filter.GetType())
                .DisplayName;
            ApplyToLabel = FilterTargetCatalog.GetApplyToLabel(filter);
        }
    }
}
