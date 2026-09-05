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
        /// <summary>
        /// Creation / Last Write / Last Access rows shared by Attributes timestamp editors.
        /// </summary>
        public static IReadOnlyList<TimestampFieldChoice> All { get; } =
        [
            new(TimestampField.Creation, "Creation"),
            new(TimestampField.LastWrite, "Last Write"),
            new(TimestampField.LastAccess, "Last Access"),
        ];

        /// <summary>
        /// Returns the combo row for <paramref name="field"/>, or Last Write when unknown.
        /// </summary>
        /// <param name="field">Filesystem timestamp field.</param>
        /// <returns>Matching choice, or the Last Write row as fallback.</returns>
        public static TimestampFieldChoice For(TimestampField field)
        {
            foreach (var choice in All)
            {
                if (choice.Field == field)
                {
                    return choice;
                }
            }

            return All.First(c => c.Field == TimestampField.LastWrite);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return DisplayName;
        }
    }
}
