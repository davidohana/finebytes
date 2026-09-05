using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Audio;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Filter Configuration editor for <see cref="Id3v2FieldSetterFilter"/>.
    /// </summary>
    internal sealed partial class Id3v2FieldSetterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public Id3v2FieldSetterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _selectedFrame = Id3v2FrameChoice.Tit2;
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets modeled ID3v2 frame combo choices.
        /// </summary>
        public IReadOnlyList<Id3v2FrameChoice> Frames => Id3v2FrameChoice.All;

        /// <summary>
        /// Gets or sets which ID3v2 frame to set.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowsLanguage))]
        [NotifyPropertyChangedFor(nameof(ShowsDescription))]
        private Id3v2FrameChoice _selectedFrame;

        /// <summary>
        /// Gets or sets the plain text or formatter template written to the frame.
        /// </summary>
        [ObservableProperty]
        private string _text = string.Empty;

        /// <summary>
        /// Gets or sets whether to set only when the current frame value is empty.
        /// </summary>
        [ObservableProperty]
        private bool _onlyIfEmpty;

        /// <summary>
        /// Gets or sets ISO-639-2 language for <c>COMM</c>/<c>USLT</c> (empty = primary default).
        /// </summary>
        [ObservableProperty]
        private string _language = string.Empty;

        /// <summary>
        /// Gets or sets the content descriptor for multi-instance frames (empty = primary).
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets whether the language field is shown for the selected frame.
        /// </summary>
        public bool ShowsLanguage => SelectedFrame.ShowsLanguage;

        /// <summary>
        /// Gets whether the description field is shown for the selected frame.
        /// </summary>
        public bool ShowsDescription => SelectedFrame.ShowsDescription;

        partial void OnSelectedFrameChanged(Id3v2FrameChoice value)
        {
            if (IsLoading)
            {
                return;
            }

            if (!value.ShowsLanguage || !value.ShowsDescription)
            {
                LoadWithoutApplying(() =>
                {
                    if (!value.ShowsLanguage)
                    {
                        Language = string.Empty;
                    }

                    if (!value.ShowsDescription)
                    {
                        Description = string.Empty;
                    }
                });
            }

            _ApplyOptions();
        }

        partial void OnTextChanged(string value) => _ApplyOptions();

        partial void OnOnlyIfEmptyChanged(bool value) => _ApplyOptions();

        partial void OnLanguageChanged(string value) => _ApplyOptions();

        partial void OnDescriptionChanged(string value) => _ApplyOptions();

        /// <summary>
        /// Copies current filter options into editor properties without live replace.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not Id3v2FieldSetterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                SelectedFrame = Id3v2FrameChoice.For(filter.Options.FrameId);
                Text = filter.Options.Text;
                OnlyIfEmpty = filter.Options.OnlyIfEmpty;
                Language = filter.Options.Language ?? string.Empty;
                Description = filter.Options.Description ?? string.Empty;
            });
        }

        /// <summary>
        /// Builds options from editor fields and replaces the step filter when changed.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || SelectedFrame is null || Step.Filter is not Id3v2FieldSetterFilter filter)
            {
                return;
            }

            var language = SelectedFrame.ShowsLanguage ? _NullIfWhiteSpace(Language) : null;
            var description = SelectedFrame.ShowsDescription ? _NullIfWhiteSpace(Description) : null;
            var options = new Id3v2FieldSetterOptions(
                FrameId: SelectedFrame.FrameId,
                Text: Text,
                OnlyIfEmpty: OnlyIfEmpty,
                Language: language,
                Description: description
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static string? _NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
