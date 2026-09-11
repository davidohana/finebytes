namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Content for a text input dialog (title, styled prompt, optional note, optional multiline).
    /// </summary>
    public sealed class TextInputPrompt
    {
        /// <summary>
        /// Gets the window title.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Gets the primary prompt above the text box.
        /// </summary>
        public required StyledTextDisplay Prompt { get; init; }

        /// <summary>
        /// Gets an optional secondary note under the primary prompt.
        /// </summary>
        public StyledTextDisplay? Note { get; init; }

        /// <summary>
        /// Gets the initial text box value.
        /// </summary>
        public required string DefaultValue { get; init; }

        /// <summary>
        /// Gets whether the value box accepts multiple lines.
        /// </summary>
        public bool Multiline { get; init; }

        /// <summary>
        /// Concatenates prompt and note text for tests and logging.
        /// </summary>
        /// <returns>Plain text with a blank line between prompt and note when both are present.</returns>
        public string ToPlainText()
        {
            var prompt = Prompt.ToPlainText();
            if (Note is null || Note.IsEmpty)
            {
                return prompt;
            }

            return prompt + "\n\n" + Note.ToPlainText();
        }
    }
}
