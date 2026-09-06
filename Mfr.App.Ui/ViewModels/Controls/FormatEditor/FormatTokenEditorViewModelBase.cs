using System.ComponentModel;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor
{
    /// <summary>
    /// Shared base for format-token parameter editor view-models.
    /// </summary>
    /// <remarks>
    /// Initializes title and canonical name.
    /// </remarks>
    /// <param name="title">Dialog title.</param>
    /// <param name="canonicalName">Token canonical name.</param>
    internal abstract class FormatTokenEditorViewModelBase(string title, string canonicalName)
        : ViewModelBase,
            IFormatTokenEditorViewModel
    {
        /// <inheritdoc />
        public string Title { get; } = title;

        /// <inheritdoc />
        public string CanonicalName { get; } = canonicalName;

        /// <inheritdoc />
        public string ResultingFormatString => "<" + BuildInnerText() + ">";

        /// <inheritdoc />
        public abstract string BuildInnerText();

        /// <inheritdoc />
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName is null or nameof(ResultingFormatString))
            {
                return;
            }

            OnPropertyChanged(nameof(ResultingFormatString));
        }
    }
}
