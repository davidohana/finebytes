using System.Text.Json.Serialization;

namespace Mfr8.Core
{
    public sealed record FilterPreset
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
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

    public abstract record Filter(bool Enabled, FilterTarget Target)
    {
        public abstract string Type { get; }
    }

    public sealed record LettersCaseOptions(
        LettersCaseMode Mode,
        IReadOnlyList<string> SkipWords);

    public enum LettersCaseMode
    {
        UpperCase,
        LowerCase,
        TitleCase,
        SentenceCase,
        InvertCase
    }

    public sealed record LettersCaseFilter(
        bool Enabled,
        FilterTarget Target,
        LettersCaseOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "LettersCase";
    }

    public sealed record SpaceCharacterOptions(
        string ReplaceSpaceWith,
        string ReplaceCharWithSpace);

    public sealed record SpaceCharacterFilter(
        bool Enabled,
        FilterTarget Target,
        SpaceCharacterOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "SpaceCharacter";
    }

    public sealed record RemoveSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        public override string Type => "RemoveSpaces";
    }

    public sealed record ShrinkSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        public override string Type => "ShrinkSpaces";
    }

    public sealed record TrimLeftFilter(
        bool Enabled,
        FilterTarget Target,
        int Count) : Filter(Enabled, Target)
    {
        public override string Type => "TrimLeft";
    }

    public sealed record TrimRightFilter(
        bool Enabled,
        FilterTarget Target,
        int Count) : Filter(Enabled, Target)
    {
        public override string Type => "TrimRight";
    }

    public sealed record ExtractLeftFilter(
        bool Enabled,
        FilterTarget Target,
        int Count) : Filter(Enabled, Target)
    {
        public override string Type => "ExtractLeft";
    }

    public sealed record ExtractRightFilter(
        bool Enabled,
        FilterTarget Target,
        int Count) : Filter(Enabled, Target)
    {
        public override string Type => "ExtractRight";
    }

    public enum ReplacerMode
    {
        Literal,
        Wildcard,
        Regex
    }

    public sealed record ReplacerOptions(
        string Find,
        string Replacement,
        ReplacerMode Mode,
        bool CaseSensitive,
        bool ReplaceAll,
        bool WholeWord);

    public sealed record ReplacerFilter(
        bool Enabled,
        FilterTarget Target,
        ReplacerOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "Replacer";
    }

    public sealed record FormatterOptions(string Template);

    public sealed record FormatterFilter(
        bool Enabled,
        FilterTarget Target,
        FormatterOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "Formatter";
    }

    public enum CounterPosition
    {
        Prepend,
        Append,
        Replace
    }

    public sealed record CounterOptions(
        int Start,
        int Step,
        int Width,
        string PadChar,
        CounterPosition Position,
        string Separator,
        bool ResetPerFolder);

    public sealed record CounterFilter(
        bool Enabled,
        FilterTarget Target,
        CounterOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "Counter";
    }

    public sealed record CleanerOptions(
        bool RemoveIllegalChars,
        string IllegalCharReplacement,
        string CustomCharsToRemove,
        string CustomReplacement);

    public sealed record CleanerFilter(
        bool Enabled,
        FilterTarget Target,
        CleanerOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "Cleaner";
    }

    public sealed record FixLeadingZerosOptions(
        int Width,
        bool RemoveExtraZeros);

    public sealed record FixLeadingZerosFilter(
        bool Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "FixLeadingZeros";
    }

    public sealed record StripParenthesesOptions(
        string Types,
        bool RemoveContents);

    public sealed record StripParenthesesFilter(
        bool Enabled,
        FilterTarget Target,
        StripParenthesesOptions Options) : Filter(Enabled, Target)
    {
        public override string Type => "StripParentheses";
    }

    public enum OutputFormat { Table, Json, Csv }

    public sealed record FileEntryLite(
        int GlobalIndex,
        int FolderOccurrenceIndex,
        string FullPath,
        string DirectoryPath,
        string Prefix,
        string Extension);

}
