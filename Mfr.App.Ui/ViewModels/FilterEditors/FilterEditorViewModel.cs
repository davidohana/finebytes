using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Filter Configuration pane host for type-specific option editors.
    /// </summary>
    public sealed partial class FilterEditorViewModel : ViewModelBase
    {
        private SessionState? _session;

        /// <summary>
        /// Gets whether an Applied Filters row is driving the configuration pane.
        /// </summary>
        [ObservableProperty]
        private bool _hasSelectedStep;

        /// <summary>
        /// Gets the applied-filter title, e.g. <c>Applied Filter: Shrink Spaces</c>.
        /// </summary>
        [ObservableProperty]
        private string _titleText = string.Empty;

        /// <summary>
        /// Gets the type-specific options editor for the selected filter, or <see langword="null"/> when none.
        /// </summary>
        [ObservableProperty]
        private FilterOptionsEditorViewModel? _optionsEditor;

        /// <summary>
        /// Gets or sets whether the format-token picker catalog is expanded (shared across format editors).
        /// <para>
        /// When a session document is attached, changes write through to
        /// <see cref="SessionStateFilterEditor.FormatTokenPickerExpanded"/> (flushed on main-window close).
        /// </para>
        /// </summary>
        [ObservableProperty]
        private bool _formatTokenPickerExpanded = true;

        /// <summary>
        /// Restores Filter Configuration chrome from <paramref name="session"/> and keeps a live write-through.
        /// </summary>
        /// <param name="session">
        /// Loaded session document, or <see langword="null"/> for first-launch defaults and no persistence.
        /// </param>
        internal void ApplySession(SessionState? session)
        {
            _session = null;
            FormatTokenPickerExpanded = session?.FilterEditor?.FormatTokenPickerExpanded ?? true;
            _session = session;
        }

        /// <summary>
        /// Updates the pane from the Applied Filters selection (first row when multi-select).
        /// </summary>
        /// <param name="selectedSteps">Current Applied list selection.</param>
        internal void SyncSelection(IReadOnlyList<AppliedFilterStepViewModel> selectedSteps)
        {
            ArgumentNullException.ThrowIfNull(selectedSteps);

            var step = selectedSteps.Count > 0 ? selectedSteps[0] : null;
            HasSelectedStep = step is not null;
            TitleText = step is null ? string.Empty : $"Applied Filter: {step.DisplayName}";
            OptionsEditor = step is null ? null : FilterOptionsEditorFactory.Create(step);
        }

        partial void OnFormatTokenPickerExpandedChanged(bool value)
        {
            if (_session is not null)
            {
                _session.EnsureFilterEditor().FormatTokenPickerExpanded = value;
            }

            if (OptionsEditor is { } editor && editor.FormatTokenPickerExpanded != value)
            {
                editor.FormatTokenPickerExpanded = value;
            }
        }

        partial void OnOptionsEditorChanged(
            FilterOptionsEditorViewModel? oldValue,
            FilterOptionsEditorViewModel? newValue
        )
        {
            if (oldValue is not null)
            {
                oldValue.PropertyChanged -= _OnOptionsEditorPropertyChanged;
            }

            if (newValue is null)
            {
                return;
            }

            newValue.FormatTokenPickerExpanded = FormatTokenPickerExpanded;
            newValue.PropertyChanged += _OnOptionsEditorPropertyChanged;
        }

        private void _OnOptionsEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName != nameof(FilterOptionsEditorViewModel.FormatTokenPickerExpanded)
                || sender is not FilterOptionsEditorViewModel editor
            )
            {
                return;
            }

            if (FormatTokenPickerExpanded != editor.FormatTokenPickerExpanded)
            {
                FormatTokenPickerExpanded = editor.FormatTokenPickerExpanded;
            }
        }
    }
}
