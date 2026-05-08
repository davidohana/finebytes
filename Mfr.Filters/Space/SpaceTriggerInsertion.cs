namespace Mfr.Filters.Space
{
    /// <summary>
    /// Shared rules for inserting the pipeline word separator next to trigger characters.
    /// </summary>
    internal static class SpaceTriggerInsertion
    {
        /// <summary>
        /// Returns whether a word separator should be inserted beside a trigger, given the adjacent character on that side.
        /// </summary>
        /// <param name="neighbor">Character adjacent to the trigger (before or after in the original string).</param>
        /// <param name="separator">Current word separator character.</param>
        /// <param name="onlyWhenNeighborIsLetterOrDigit">
        /// When <c>true</c>, insertion is allowed only if <paramref name="neighbor"/> is a Unicode letter or digit.
        /// </param>
        internal static bool ShouldInsertBeside(char neighbor, char separator, bool onlyWhenNeighborIsLetterOrDigit)
        {
            if (neighbor == separator)
                return false;
            if (onlyWhenNeighborIsLetterOrDigit && !char.IsLetterOrDigit(neighbor))
                return false;

            return true;
        }
    }
}
