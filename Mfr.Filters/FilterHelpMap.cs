namespace Mfr.Filters
{
    /// <summary>
    /// Maps catalog filter type discriminators to MFR7 Help HTML file names.
    /// <para>
    /// finebytes does not ship Help yet; the UI opens these files from an installed or source
    /// MFR7 <c>Help</c> folder when present. Values match MFR7 <c>[FilterInfo]</c> <c>helpFileName</c>
    /// (see <c>.agents/skills/mfr7-reference/filter-map.md</c>).
    /// </para>
    /// </summary>
    public static class FilterHelpMap
    {
        private static readonly Dictionary<string, string> TypeToHelpFileName = new(StringComparer.Ordinal)
        {
            ["CapitalizeAfter"] = "capafterfilter.html",
            ["CasingList"] = "casinglistfilter.html",
            ["LettersCase"] = "letterscasefilter.html",
            ["SentenceEndCharacters"] = "sentendcharsfilter.html",
            ["UppercaseInitials"] = "uppercaseinitialsfilter.html",
            ["RemoveSpaces"] = "remspacesfilter.html",
            ["SeparateCapitalizedText"] = "sepcaptextfilter.html",
            ["ShrinkSpaces"] = "shrinkspacesfilter.html",
            ["SpaceAfter"] = "spaceafterfilter.html",
            ["SpaceAround"] = "spacearoundfilter.html",
            ["SpaceCharacter"] = "spacecharfilter.html",
            ["ExtractLeft"] = "leftextractfilter.html",
            ["ExtractRight"] = "rightextractfilter.html",
            ["ShrinkDuplicateCharacters"] = "remdupsfilter.html",
            ["TrimBetween"] = "trimbetweenfilter.html",
            ["TrimLeft"] = "lefttrimfilter.html",
            ["TrimRight"] = "righttrimfilter.html",
            ["StripSpacesLeft"] = "lefttrimspacesfilter.html",
            ["StripSpacesRight"] = "righttrimspacesfilter.html",
            ["Cleaner"] = "cleanerfilter.html",
            ["ReplaceList"] = "replacelistfilter.html",
            ["Replacer"] = "replacerfilter.html",
            ["Counter"] = "counterfilter.html",
            ["Formatter"] = "formatterfilter.html",
            ["Inserter"] = "inserterfilter.html",
            ["NameList"] = "namelistfilter.html",
            ["TokenMover"] = "tokenmoverfilter.html",
            ["TagRemover"] = "id3tagremoverfilter.html",
            ["AudioTagSetter"] = "id3tagsetterfilter.html",
            ["Id3v2FieldSetter"] = "id3v2fieldsetterfilter.html",
            ["AttributesSetter"] = "attrfilter.html",
            // Combined MFR7 Date + Time Setter; primary page is Date Setter help.
            ["DateTimeSetter"] = "datefilter.html",
            ["TimeShifter"] = "timeshiftfilter.html",
            ["FixLeadingZeros"] = "fixlzfilter.html",
            ["PathMover"] = "moverfilter.html",
            ["StripParentheses"] = "stripparfilter.html",
        };

        /// <summary>
        /// Tries to resolve the MFR7 Help HTML file name for a catalog type discriminator.
        /// </summary>
        /// <param name="catalogType">
        /// <see cref="FilterCatalogEntry.Type"/> / <see cref="BaseFilter.Type"/> string.
        /// </param>
        /// <param name="helpFileName">Help file name when known (e.g. <c>spacecharfilter.html</c>).</param>
        /// <returns><see langword="true"/> when <paramref name="catalogType"/> has a mapping.</returns>
        public static bool TryGetHelpFileName(string catalogType, out string helpFileName)
        {
            ArgumentNullException.ThrowIfNull(catalogType);

            if (TypeToHelpFileName.TryGetValue(catalogType, out helpFileName!))
            {
                return true;
            }

            helpFileName = string.Empty;
            return false;
        }
    }
}
