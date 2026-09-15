using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.FilterChain;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Models.Config;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Filter Configuration pane host for type-specific option editors.
    /// </summary>
    public sealed partial class FilterEditorViewModel : ViewModelBase
    {
        /// <summary>
        /// Title bar text when no Filter Chain row is selected.
        /// </summary>
        public const string EmptyTitleText = "Filter Configuration";

        /// <summary>
        /// Body hint when no Filter Chain row is selected.
        /// </summary>
        public const string EmptySelectionHint = "Select a filter in Filter Chain to configure it.";

        private bool _persistSession;
        private Func<IReadOnlyList<RenameItem>>? _resolveSampleRenameItems;
        private Func<string, RenameItem?>? _resolveRenameItemByFullPath;

        /// <summary>
        /// Gets whether a Filter Chain row is driving the configuration pane.
        /// </summary>
        [ObservableProperty]
        private bool _hasSelectedStep;

        /// <summary>
        /// Gets the pane title: empty-state label, or <c>Filter: …</c> when a row is selected.
        /// </summary>
        [ObservableProperty]
        private string _titleText = EmptyTitleText;

        /// <summary>
        /// Gets the type-specific options editor for the selected filter, or <see langword="null"/> when none.
        /// </summary>
        [ObservableProperty]
        private FilterOptionsEditorViewModel? _optionsEditor;

        /// <summary>
        /// Gets or sets whether the format-token picker catalog is expanded (shared across format editors).
        /// <para>
        /// When session persistence is enabled, changes write through to
        /// <see cref="ConfigStore.FilterEditor"/> (flushed on main-window close).
        /// </para>
        /// </summary>
        [ObservableProperty]
        private bool _formatTokenPickerExpanded = true;

        /// <summary>
        /// Supplies Rename List sample resolution for Visual Trim Helper editors.
        /// </summary>
        /// <param name="resolveSampleRenameItems">Returns current Rename List engine items (first used for init).</param>
        /// <param name="resolveRenameItemByFullPath">Looks up a row by original full path for drag-drop.</param>
        internal void SetSampleRenameItemSource(
            Func<IReadOnlyList<RenameItem>> resolveSampleRenameItems,
            Func<string, RenameItem?> resolveRenameItemByFullPath
        )
        {
            ArgumentNullException.ThrowIfNull(resolveSampleRenameItems);
            ArgumentNullException.ThrowIfNull(resolveRenameItemByFullPath);
            _resolveSampleRenameItems = resolveSampleRenameItems;
            _resolveRenameItemByFullPath = resolveRenameItemByFullPath;
        }

        /// <summary>
        /// Restores Filter Configuration chrome from <see cref="ConfigStore.FilterEditor"/> when persisting.
        /// </summary>
        /// <param name="persistSession">
        /// When <see langword="true"/>, restore from <see cref="ConfigStore"/> and write through on change.
        /// When <see langword="false"/>, use first-launch defaults with no persistence.
        /// </param>
        internal void ApplySession(bool persistSession)
        {
            _persistSession = false;
            FormatTokenPickerExpanded =
                !persistSession || (ConfigStore.FilterEditor?.FormatTokenPickerExpanded ?? true);
            _persistSession = persistSession;
        }

        /// <summary>
        /// Updates the pane from the Filter Chain selection (first row when multi-select).
        /// </summary>
        /// <param name="selectedSteps">Current Filter Chain selection.</param>
        internal void SyncSelection(IReadOnlyList<FilterChainStepViewModel> selectedSteps)
        {
            ArgumentNullException.ThrowIfNull(selectedSteps);

            var step = selectedSteps.Count > 0 ? selectedSteps[0] : null;
            HasSelectedStep = step is not null;
            TitleText = step is null ? EmptyTitleText : $"Filter: {step.DisplayName}";
            OptionsEditor = step is null
                ? null
                : FilterOptionsEditorFactory.Create(step, _resolveSampleRenameItems, _resolveRenameItemByFullPath);
        }

        /// <summary>
        /// Reloads Visual Trim Helper Rename List navigation when the list membership changes.
        /// </summary>
        internal void RefreshTrimHelperRenameItems()
        {
            if (OptionsEditor is IHasVisualTrimHelper editor)
            {
                editor.TrimHelper.RefreshRenameItems();
            }
        }

        partial void OnFormatTokenPickerExpandedChanged(bool value)
        {
            if (_persistSession)
            {
                ConfigStore.EnsureFilterEditor().FormatTokenPickerExpanded = value;
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
                oldValue.FlushPendingLiveListTextApply();
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
