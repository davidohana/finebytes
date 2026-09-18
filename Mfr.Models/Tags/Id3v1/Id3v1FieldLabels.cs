namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// User-visible labels for every <see cref="Id3v1Field"/>.
    /// </summary>
    public static class Id3v1FieldLabels
    {
        /// <summary>
        /// Returns the unqualified Apply-To label for <paramref name="field"/>.
        /// </summary>
        /// <param name="field">ID3v1 scalar.</param>
        /// <returns>User-visible label (enum name).</returns>
        public static string For(Id3v1Field field)
        {
            return field.ToString();
        }
    }
}
