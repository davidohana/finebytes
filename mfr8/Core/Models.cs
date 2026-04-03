using System.Text.Json.Serialization;

namespace Mfr8.Core
{
    public sealed record FilterPreset
    {
        public required Guid Id { get; init; }
        public required String Name { get; init; }
        public String? Description { get; init; }
        public required IReadOnlyList<Filter> Filters { get; init; }
    }

    public enum FilterTargetFamily
    {
        FileName,
        FileExtension,
        FullFileName,
        FullFilePath,
        DirectorySegment,
        FileContents,
        Attributes,
        CreationDate,
        LastWriteDate,
        LastAccessDate,
        AudioTag,
        Id3v1,
        Id3v2,
        ImageTag
    }

    public enum FileNameTargetMode
    {
        Prefix,
        Extension,
        Full
    }

    public abstract record FilterTarget
    {
        [JsonIgnore]
        public abstract FilterTargetFamily Family { get; }
    }

    public sealed record FileNameTarget(FileNameTargetMode FileNameMode) : FilterTarget
    {
        public override FilterTargetFamily Family => FilterTargetFamily.FileName;
    }

    public abstract record Filter(Boolean Enabled, FilterTarget Target)
    {
        public abstract String Type { get; }
    }

    public sealed record LettersCaseOptions(
        LettersCaseMode Mode,
        IReadOnlyList<String> SkipWords);

    public enum LettersCaseMode
    {
        UpperCase,
        LowerCase,
        TitleCase,
        SentenceCase,
        InvertCase
    }

    public sealed record LettersCaseFilter(
        Boolean Enabled,
        FilterTarget Target,
        LettersCaseOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "LettersCase";
    }

    public sealed record SpaceCharacterOptions(
        String ReplaceSpaceWith,
        String ReplaceCharWithSpace);

    public sealed record SpaceCharacterFilter(
        Boolean Enabled,
        FilterTarget Target,
        SpaceCharacterOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "SpaceCharacter";
    }

    public sealed record RemoveSpacesFilter(
        Boolean Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        public override String Type => "RemoveSpaces";
    }

    public sealed record ShrinkSpacesFilter(
        Boolean Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        public override String Type => "ShrinkSpaces";
    }

    public sealed record TrimLeftFilter(
        Boolean Enabled,
        FilterTarget Target,
        Int32 Count) : Filter(Enabled, Target)
    {
        public override String Type => "TrimLeft";
    }

    public sealed record TrimRightFilter(
        Boolean Enabled,
        FilterTarget Target,
        Int32 Count) : Filter(Enabled, Target)
    {
        public override String Type => "TrimRight";
    }

    public sealed record ExtractLeftFilter(
        Boolean Enabled,
        FilterTarget Target,
        Int32 Count) : Filter(Enabled, Target)
    {
        public override String Type => "ExtractLeft";
    }

    public sealed record ExtractRightFilter(
        Boolean Enabled,
        FilterTarget Target,
        Int32 Count) : Filter(Enabled, Target)
    {
        public override String Type => "ExtractRight";
    }

    public enum ReplacerMode
    {
        Literal,
        Wildcard,
        Regex
    }

    public sealed record ReplacerOptions(
        String Find,
        String Replacement,
        ReplacerMode Mode,
        Boolean CaseSensitive,
        Boolean ReplaceAll,
        Boolean WholeWord);

    public sealed record ReplacerFilter(
        Boolean Enabled,
        FilterTarget Target,
        ReplacerOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "Replacer";
    }

    public sealed record FormatterOptions(String Template);

    public sealed record FormatterFilter(
        Boolean Enabled,
        FilterTarget Target,
        FormatterOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "Formatter";
    }

    public enum CounterPosition
    {
        Prepend,
        Append,
        Replace
    }

    public sealed record CounterOptions(
        Int32 Start,
        Int32 Step,
        Int32 Width,
        String PadChar,
        CounterPosition Position,
        String Separator,
        Boolean ResetPerFolder);

    public sealed record CounterFilter(
        Boolean Enabled,
        FilterTarget Target,
        CounterOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "Counter";
    }

    public sealed record CleanerOptions(
        Boolean RemoveIllegalChars,
        String IllegalCharReplacement,
        String CustomCharsToRemove,
        String CustomReplacement);

    public sealed record CleanerFilter(
        Boolean Enabled,
        FilterTarget Target,
        CleanerOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "Cleaner";
    }

    public sealed record FixLeadingZerosOptions(
        Int32 Width,
        Boolean RemoveExtraZeros);

    public sealed record FixLeadingZerosFilter(
        Boolean Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "FixLeadingZeros";
    }

    public sealed record StripParenthesesOptions(
        String Types,
        Boolean RemoveContents);

    public sealed record StripParenthesesFilter(
        Boolean Enabled,
        FilterTarget Target,
        StripParenthesesOptions Options) : Filter(Enabled, Target)
    {
        public override String Type => "StripParentheses";
    }

    public enum OutputFormat { Table, Json, Csv }

    public sealed record FileEntryLite(
        Int32 GlobalIndex,
        Int32 FolderOccurrenceIndex,
        String FullPath,
        String DirectoryPath,
        String Prefix,
        String Extension);

}
