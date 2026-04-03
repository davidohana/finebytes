using System.Text.Json;

namespace Mfr8.Core;

public static class FilterParser
{
    public static Filter ParseFilter(JsonElement filterEl)
    {
        var type = GetStringRequired(filterEl, "type");
        var enabled = filterEl.TryGetProperty("enabled", out var enabledEl) && enabledEl.ValueKind == JsonValueKind.True
            ? true
            : filterEl.TryGetProperty("enabled", out enabledEl) && enabledEl.ValueKind == JsonValueKind.False
                ? false
                : true;

        if (!filterEl.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Filter '{type}' missing target object.");

        var target = ParseTarget(targetEl, type);

        JsonElement optionsEl = default;
        if (filterEl.TryGetProperty("options", out var optionsCandidate) && optionsCandidate.ValueKind == JsonValueKind.Object)
            optionsEl = optionsCandidate;

        return type switch
        {
            "LettersCase" => ParseLettersCase(enabled, targetEl, target, optionsEl),
            "SpaceCharacter" => ParseSpaceCharacter(enabled, target, optionsEl),
            "RemoveSpaces" => ParseRemoveSpaces(enabled, target),
            "ShrinkSpaces" => ParseShrinkSpaces(enabled, target),
            "TrimLeft" => ParseTrimLeft(enabled, target, optionsEl),
            "TrimRight" => ParseTrimRight(enabled, target, optionsEl),
            "ExtractLeft" => ParseExtractLeft(enabled, target, optionsEl),
            "ExtractRight" => ParseExtractRight(enabled, target, optionsEl),
            "Replacer" => ParseReplacer(enabled, target, optionsEl),
            "Formatter" => ParseFormatter(enabled, target, optionsEl),
            "Counter" => ParseCounter(enabled, target, optionsEl),
            "Cleaner" => ParseCleaner(enabled, target, optionsEl),
            "FixLeadingZeros" => ParseFixLeadingZeros(enabled, target, optionsEl),
            "StripParentheses" => ParseStripParentheses(enabled, target, optionsEl),
            _ => throw new NotSupportedException($"Phase 1 does not support filter type '{type}'."),
        };
    }

    private static FilterTarget ParseTarget(JsonElement targetEl, string filterType)
    {
        var familyStr = GetStringRequired(targetEl, "family");
        if (!string.Equals(familyStr, "FileName", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filterType}' got '{familyStr}'.");

        var modeStr = GetStringRequired(targetEl, "fileNameMode");
        if (!Enum.TryParse<FileNameTargetMode>(modeStr, ignoreCase: true, out var mode))
            throw new InvalidOperationException($"Invalid fileNameMode '{modeStr}' for filter '{filterType}'.");

        return new FileNameTarget(mode);
    }

    private static Filter ParseLettersCase(bool enabled, JsonElement targetEl, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("LettersCase target must be FileNameTarget.");

        var modeStr = GetStringRequired(optionsEl, "mode");
        if (!Enum.TryParse<LettersCaseMode>(modeStr, ignoreCase: true, out var mode))
            throw new InvalidOperationException($"Invalid LettersCase mode '{modeStr}'.");

        var skipWords = new List<string>();
        if (optionsEl.TryGetProperty("skipWords", out var skipEl) && skipEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in skipEl.EnumerateArray())
            {
                if (w.ValueKind == JsonValueKind.String && w.GetString() is { } s)
                    skipWords.Add(s);
            }
        }

        var opts = new LettersCaseOptions(mode, skipWords);
        return new LettersCaseFilter(enabled, t, opts);
    }

    private static Filter ParseSpaceCharacter(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("SpaceCharacter target must be FileNameTarget.");
        var replaceSpaceWith = GetStringRequired(optionsEl, "replaceSpaceWith");
        var replaceCharWithSpace = GetStringRequired(optionsEl, "replaceCharWithSpace");
        var opts = new SpaceCharacterOptions(replaceSpaceWith, replaceCharWithSpace);
        return new SpaceCharacterFilter(enabled, t, opts);
    }

    private static Filter ParseRemoveSpaces(bool enabled, FilterTarget target)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("RemoveSpaces target must be FileNameTarget.");
        return new RemoveSpacesFilter(enabled, t);
    }

    private static Filter ParseShrinkSpaces(bool enabled, FilterTarget target)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("ShrinkSpaces target must be FileNameTarget.");
        return new ShrinkSpacesFilter(enabled, t);
    }

    private static Filter ParseTrimLeft(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("TrimLeft target must be FileNameTarget.");
        var count = GetIntRequired(optionsEl, "count");
        return new TrimLeftFilter(enabled, t, count);
    }

    private static Filter ParseTrimRight(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("TrimRight target must be FileNameTarget.");
        var count = GetIntRequired(optionsEl, "count");
        return new TrimRightFilter(enabled, t, count);
    }

    private static Filter ParseExtractLeft(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractLeft target must be FileNameTarget.");
        var count = GetIntRequired(optionsEl, "count");
        return new ExtractLeftFilter(enabled, t, count);
    }

    private static Filter ParseExtractRight(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractRight target must be FileNameTarget.");
        var count = GetIntRequired(optionsEl, "count");
        return new ExtractRightFilter(enabled, t, count);
    }

    private static Filter ParseReplacer(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("Replacer target must be FileNameTarget.");
        var find = GetStringRequired(optionsEl, "find");
        var replacement = GetStringRequired(optionsEl, "replacement");
        var modeStr = GetStringRequired(optionsEl, "mode");
        if (!Enum.TryParse<ReplacerMode>(modeStr, ignoreCase: true, out var mode))
            throw new InvalidOperationException($"Invalid Replacer mode '{modeStr}'.");

        var caseSensitive = GetBoolOrDefault(optionsEl, "caseSensitive", false);
        var replaceAll = GetBoolOrDefault(optionsEl, "replaceAll", true);
        var wholeWord = GetBoolOrDefault(optionsEl, "wholeWord", false);

        var opts = new ReplacerOptions(find, replacement, mode, caseSensitive, replaceAll, wholeWord);
        return new ReplacerFilter(enabled, t, opts);
    }

    private static Filter ParseFormatter(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("Formatter target must be FileNameTarget.");
        var template = GetStringRequired(optionsEl, "template");
        var opts = new FormatterOptions(template);
        return new FormatterFilter(enabled, t, opts);
    }

    private static Filter ParseCounter(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("Counter target must be FileNameTarget.");

        var start = GetIntRequired(optionsEl, "start");
        var step = GetIntRequired(optionsEl, "step");
        var width = GetIntRequired(optionsEl, "width");
        var padChar = GetStringRequired(optionsEl, "padChar");
        var posStr = GetStringRequired(optionsEl, "position");
        if (!Enum.TryParse<CounterPosition>(posStr, ignoreCase: true, out var position))
            throw new InvalidOperationException($"Invalid Counter position '{posStr}'.");
        var separator = GetStringRequired(optionsEl, "separator");
        var resetPerFolder = GetBoolOrDefault(optionsEl, "resetPerFolder", false);

        var opts = new CounterOptions(start, step, width, padChar, position, separator, resetPerFolder);
        return new CounterFilter(enabled, t, opts);
    }

    private static Filter ParseCleaner(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("Cleaner target must be FileNameTarget.");
        var removeIllegalChars = GetBoolOrDefault(optionsEl, "removeIllegalChars", true);
        var illegalCharReplacement = GetStringRequired(optionsEl, "illegalCharReplacement");
        var customCharsToRemove = GetStringRequired(optionsEl, "customCharsToRemove");
        var customReplacement = GetStringRequired(optionsEl, "customReplacement");
        var opts = new CleanerOptions(removeIllegalChars, illegalCharReplacement, customCharsToRemove, customReplacement);
        return new CleanerFilter(enabled, t, opts);
    }

    private static Filter ParseFixLeadingZeros(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("FixLeadingZeros target must be FileNameTarget.");
        var width = GetIntRequired(optionsEl, "width");
        var removeExtraZeros = GetBoolOrDefault(optionsEl, "removeExtraZeros", false);
        var opts = new FixLeadingZerosOptions(width, removeExtraZeros);
        return new FixLeadingZerosFilter(enabled, t, opts);
    }

    private static Filter ParseStripParentheses(bool enabled, FilterTarget target, JsonElement optionsEl)
    {
        var t = target as FileNameTarget ?? throw new InvalidOperationException("StripParentheses target must be FileNameTarget.");
        var types = GetStringRequired(optionsEl, "types");
        var removeContents = GetBoolOrDefault(optionsEl, "removeContents", true);
        var opts = new StripParenthesesOptions(types, removeContents);
        return new StripParenthesesFilter(enabled, t, opts);
    }

    private static string GetStringRequired(JsonElement el, string propertyName)
    {
        if (!el.TryGetProperty(propertyName, out var p) || p.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Missing required string '{propertyName}'.");
        var v = p.GetString();
        if (string.IsNullOrEmpty(v))
            throw new InvalidOperationException($"Property '{propertyName}' cannot be empty.");
        return v;
    }

    private static int GetIntRequired(JsonElement el, string propertyName)
    {
        if (!el.TryGetProperty(propertyName, out var p) || (p.ValueKind != JsonValueKind.Number && p.ValueKind != JsonValueKind.String))
            throw new InvalidOperationException($"Missing required int '{propertyName}'.");
        return p.ValueKind == JsonValueKind.Number ? p.GetInt32() : int.Parse(p.GetString()!);
    }

    private static bool GetBoolOrDefault(JsonElement el, string propertyName, bool defaultValue)
    {
        if (!el.TryGetProperty(propertyName, out var p)) return defaultValue;
        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => p.GetInt32() != 0,
            JsonValueKind.String => bool.TryParse(p.GetString(), out var b) ? b : defaultValue,
            _ => defaultValue
        };
    }
}

