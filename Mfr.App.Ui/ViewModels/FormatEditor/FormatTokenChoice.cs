namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// ComboBox row for format-token option editors (<see cref="Value"/> binds to token args; <see cref="Label"/> is display text).
    /// </summary>
    /// <param name="Value">Canonical argument / keyword value written into the format string.</param>
    /// <param name="Label">Human-readable label shown in the combo.</param>
    internal sealed record FormatTokenChoice(string Value, string Label)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return Label;
        }
    }
}
