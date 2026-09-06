using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;id3v2&gt;</c> format token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Id3v2Token</c> models frame id plus optional content-descriptor for multi-instance frames.
    /// Language is not part of the token argument shape, so it is not edited here.
    /// </para>
    /// </remarks>
    internal sealed partial class Id3v2FormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Initializes the editor from existing <c>frameId</c> or <c>frameId:descriptor</c> arguments.
        /// </summary>
        /// <param name="args">Field-code argument.</param>
        public Id3v2FormatTokenEditorViewModel(string? args)
            : base("ID3v2 Custom Field", "id3v2")
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                SelectedFrame = Id3v2FrameChoice.Tit2;
                return;
            }

            var trimmed = args.Trim();
            var colon = trimmed.IndexOf(':');
            var frameIdPart = colon < 0 ? trimmed : trimmed[..colon];
            var remainder = colon < 0 ? null : trimmed[(colon + 1)..];
            SelectedFrame = Id3v2FrameChoice.For(frameIdPart);
            if (SelectedFrame.ShowsDescription && !string.IsNullOrWhiteSpace(remainder))
            {
                Description = remainder;
            }
        }

        /// <summary>
        /// Gets modeled ID3v2 frame choices.
        /// </summary>
        public IReadOnlyList<Id3v2FrameChoice> Frames => Id3v2FrameChoice.All;

        /// <summary>
        /// Gets or sets the selected frame id row.
        /// </summary>
        [ObservableProperty]
        private Id3v2FrameChoice _selectedFrame = Id3v2FrameChoice.Tit2;

        /// <summary>
        /// Gets or sets the optional content descriptor for multi-instance frames.
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets whether the description box applies.
        /// </summary>
        public bool ShowsDescription => SelectedFrame.ShowsDescription;

        partial void OnSelectedFrameChanged(Id3v2FrameChoice value) => OnPropertyChanged(nameof(ShowsDescription));

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var frameId = SelectedFrame.FrameId;
            if (!ShowsDescription)
            {
                return CanonicalName + ":" + frameId;
            }

            var descriptor = Description.Trim();
            return descriptor.Length == 0
                ? CanonicalName + ":" + frameId
                : CanonicalName + ":" + frameId + ":" + descriptor;
        }
    }
}
