using Mfr.Models.Media;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Attributes
{
    /// <summary>
    /// ComboBox row for a <see cref="TimestampField"/> with the MFR7 display label.
    /// </summary>
    /// <param name="Field">Filesystem timestamp field.</param>
    /// <param name="DisplayName">User-visible label (e.g. <c>Last Write</c>).</param>
    internal sealed record TimestampFieldChoice(TimestampField Field, string DisplayName)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return DisplayName;
        }
    }
}
