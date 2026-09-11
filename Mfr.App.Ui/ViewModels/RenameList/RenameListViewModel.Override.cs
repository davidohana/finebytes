using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Manual Override Field (F2) for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised after manual overrides are set or cleared so the shell can re-preview when Auto-Preview is on.
        /// </summary>
        public event EventHandler? ManualOverridesChanged;

        /// <summary>
        /// Gets whether Cancel Manual Override should appear on the row context menu.
        /// </summary>
        public bool CanShowCancelManualOverride => _CanCancelManualOverride();

        /// <summary>
        /// Gets the field key for the focused Rename List grid cell, or <see langword="null"/> when none.
        /// </summary>
        public RenameListFieldKey? FocusedFieldKey { get; private set; }

        /// <summary>
        /// Updates the focused cell field key used by Manual Override / Cancel.
        /// </summary>
        /// <param name="key">Focused column field key, or <see langword="null"/> when cleared.</param>
        internal void SetFocusedFieldKey(RenameListFieldKey? key)
        {
            if (FocusedFieldKey == key)
            {
                return;
            }

            FocusedFieldKey = key;
            OnPropertyChanged(nameof(FocusedFieldKey));
            _NotifyManualOverrideCommandsChanged();
        }

        /// <summary>
        /// Prompts for a value and applies the same manual override to all selected non-error rows.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanManualOverrideField))]
        public async Task ManualOverrideFieldAsync()
        {
            if (!_CanManualOverrideField() || FocusedFieldKey is not { } key)
            {
                return;
            }

            var field = RenameListFieldCatalog.GetField(key);
            if (!field.SupportsWrite)
            {
                return;
            }

            var eligible = _selectedEntries.Where(entry => _IsOverrideEligible(entry, key)).ToList();
            if (eligible.Count == 0)
            {
                return;
            }

            var promptHooks = UiHooks?.PromptAsync;
            if (promptHooks is null)
            {
                return;
            }

            var defaultValue = eligible[0].GetFieldText(key);
            var sideLabel = key.IsPreview
                ? "final value (after filters)"
                : "initial value (before filters)";
            var fieldLabel =
                eligible.Count > 1
                    ? $"'{field.DisplayName}' in {eligible.Count} items"
                    : $"'{field.DisplayName}'";
            var prompt = new TextInputPrompt
            {
                Title = "Manual Set Value",
                DefaultValue = defaultValue,
                Prompt = StatusHintDisplay.FromRuns(
                    new StatusHintRun("Set the "),
                    new StatusHintRun(sideLabel) { FontWeight = FontWeight.Bold },
                    new StatusHintRun(" of the field "),
                    new StatusHintRun(fieldLabel) { FontWeight = FontWeight.Bold },
                    new StatusHintRun(" to:")
                ),
                Note = StatusHintDisplay.FromRuns(
                    new StatusHintRun("Takes effect in the Rename List now ("),
                    new StatusHintRun("blue")
                    {
                        FontWeight = FontWeight.Bold,
                        ForegroundResourceKey = "RenameListManualOverrideForegroundBrush",
                    },
                    new StatusHintRun("). Files are updated when you press "),
                    new StatusHintRun("Go") { FontWeight = FontWeight.Bold },
                    new StatusHintRun(".")
                ),
            };

            var value = await promptHooks(prompt).ConfigureAwait(true);
            if (value is null || IsBusy)
            {
                return;
            }

            foreach (var entry in eligible)
            {
                entry.EngineItem.SetOverride(key, value);
            }

            _RefreshFieldDisplay();
            _NotifyManualOverrideCommandsChanged();
            ManualOverridesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clears the focused field's override on every selected row that has one.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanCancelManualOverride))]
        public void CancelManualOverride()
        {
            if (!_CanCancelManualOverride() || FocusedFieldKey is not { } key)
            {
                return;
            }

            _ClearOverridesForColumn(key, _selectedEntries);
        }

        /// <summary>
        /// Returns whether any row has a manual override on <paramref name="key"/> (header Cancel visibility).
        /// </summary>
        /// <param name="key">Column field key (original or preview side).</param>
        /// <returns><see langword="true"/> when at least one list row is overridden on that side.</returns>
        public bool HasColumnOverride(RenameListFieldKey key)
        {
            if (IsBusy || Entries.Count == 0)
            {
                return false;
            }

            return Entries.Any(entry => entry.IsOverridden(key));
        }

        /// <summary>
        /// Clears the override for <paramref name="key"/> on every row in the list.
        /// </summary>
        /// <param name="key">Column field key (original or preview side).</param>
        public void CancelManualOverrideForColumn(RenameListFieldKey key)
        {
            if (IsBusy || Entries.Count == 0)
            {
                return;
            }

            _ClearOverridesForColumn(key, Entries);
        }

        /// <summary>
        /// Clears <paramref name="key"/> overrides for <paramref name="entries"/>, then refreshes and notifies.
        /// </summary>
        /// <param name="key">Field side to clear.</param>
        /// <param name="entries">Rows to clear (selection or full list).</param>
        private void _ClearOverridesForColumn(RenameListFieldKey key, IEnumerable<RenameListEntry> entries)
        {
            var cleared = false;
            foreach (var entry in entries)
            {
                if (!entry.IsOverridden(key))
                {
                    continue;
                }

                entry.EngineItem.SetOverride(key, null);
                cleared = true;
            }

            if (!cleared)
            {
                return;
            }

            _RefreshFieldDisplay();
            _NotifyManualOverrideCommandsChanged();
            ManualOverridesChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool _CanManualOverrideField()
        {
            if (IsBusy || FocusedFieldKey is not { } key || _selectedEntries.Count == 0)
            {
                return false;
            }

            var field = RenameListFieldCatalog.GetField(key);
            if (!field.SupportsWrite)
            {
                return false;
            }

            return _selectedEntries.Any(entry => _IsOverrideEligible(entry, key));
        }

        private bool _CanCancelManualOverride()
        {
            if (IsBusy || FocusedFieldKey is not { } key || _selectedEntries.Count == 0)
            {
                return false;
            }

            return _selectedEntries.Any(entry => entry.IsOverridden(key));
        }

        /// <summary>
        /// Rows with a preview error or load-error cell are skipped for Manual Override defaults/apply.
        /// </summary>
        private static bool _IsOverrideEligible(RenameListEntry entry, RenameListFieldKey key)
        {
            if (entry.HasPreviewError)
            {
                return false;
            }

            if (entry.IsLoadError(key))
            {
                return false;
            }

            return true;
        }

        private void _NotifyManualOverrideCommandsChanged()
        {
            OnPropertyChanged(nameof(CanShowCancelManualOverride));
            ManualOverrideFieldCommand.NotifyCanExecuteChanged();
            CancelManualOverrideCommand.NotifyCanExecuteChanged();
        }
    }
}
