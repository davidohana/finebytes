using System.Text.Json;

namespace Mfr8.Core
{
    public static class FilterParser
    {
        /// <summary>
        /// Parses a single JSON filter element into a typed <see cref="Filter"/>.
        /// </summary>
        /// <param name="filterEl">JSON object representing a filter.</param>
        /// <returns>The parsed typed filter instance.</returns>
        public static Filter ParseFilter(JsonElement filterEl)
        {
            var type = _GetStringRequired(filterEl, "type");
            var enabled = (filterEl.TryGetProperty("enabled", out var enabledEl) && enabledEl.ValueKind == JsonValueKind.True) || !filterEl.TryGetProperty("enabled", out enabledEl) || enabledEl.ValueKind != JsonValueKind.False;

            if (!filterEl.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Filter '{type}' missing target object.");
            }

            var target = _ParseTarget(targetEl, type);

            JsonElement optionsEl = default;
            if (filterEl.TryGetProperty("options", out var optionsCandidate) && optionsCandidate.ValueKind == JsonValueKind.Object)
            {
                optionsEl = optionsCandidate;
            }

            return type switch
            {
                "LettersCase" => _ParseLettersCase(enabled, targetEl, target, optionsEl),
                "SpaceCharacter" => _ParseSpaceCharacter(enabled, target, optionsEl),
                "RemoveSpaces" => _ParseRemoveSpaces(enabled, target),
                "ShrinkSpaces" => _ParseShrinkSpaces(enabled, target),
                "TrimLeft" => _ParseTrimLeft(enabled, target, optionsEl),
                "TrimRight" => _ParseTrimRight(enabled, target, optionsEl),
                "ExtractLeft" => _ParseExtractLeft(enabled, target, optionsEl),
                "ExtractRight" => _ParseExtractRight(enabled, target, optionsEl),
                "Replacer" => _ParseReplacer(enabled, target, optionsEl),
                "Formatter" => _ParseFormatter(enabled, target, optionsEl),
                "Counter" => _ParseCounter(enabled, target, optionsEl),
                "Cleaner" => _ParseCleaner(enabled, target, optionsEl),
                "FixLeadingZeros" => _ParseFixLeadingZeros(enabled, target, optionsEl),
                "StripParentheses" => _ParseStripParentheses(enabled, target, optionsEl),
                _ => throw new NotSupportedException($"Phase 1 does not support filter type '{type}'."),
            };
        }

        private static FilterTarget _ParseTarget(JsonElement targetEl, string filterType)
        {
            var familyStr = _GetStringRequired(targetEl, "family");
            if (!string.Equals(familyStr, "FileName", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filterType}' got '{familyStr}'.");
            }

            var modeStr = _GetStringRequired(targetEl, "fileNameMode");
            return !Enum.TryParse(modeStr, ignoreCase: true, out FileNameTargetMode mode)
                ? throw new InvalidOperationException($"Invalid fileNameMode '{modeStr}' for filter '{filterType}'.")
                : (FilterTarget)new FileNameTarget(mode);
        }

        private static Filter _ParseLettersCase(bool enabled, JsonElement targetEl, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("LettersCase target must be FileNameTarget.");

            var modeStr = _GetStringRequired(optionsEl, "mode");
            if (!Enum.TryParse(modeStr, ignoreCase: true, out LettersCaseMode mode))
            {
                throw new InvalidOperationException($"Invalid LettersCase mode '{modeStr}'.");
            }

            var skipWords = new List<string>();
            if (optionsEl.TryGetProperty("skipWords", out var skipEl) && skipEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in skipEl.EnumerateArray())
                {
                    if (w.ValueKind == JsonValueKind.String && w.GetString() is { } s)
                    {
                        skipWords.Add(s);
                    }
                }
            }

            var opts = new LettersCaseOptions(mode, skipWords);
            return new LettersCaseFilter(enabled, t, opts);
        }

        private static Filter _ParseSpaceCharacter(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("SpaceCharacter target must be FileNameTarget.");
            var replaceSpaceWith = _GetStringRequired(optionsEl, "replaceSpaceWith");
            var replaceCharWithSpace = _GetStringRequired(optionsEl, "replaceCharWithSpace");
            var opts = new SpaceCharacterOptions(replaceSpaceWith, replaceCharWithSpace);
            return new SpaceCharacterFilter(enabled, t, opts);
        }

        private static Filter _ParseRemoveSpaces(bool enabled, FilterTarget target)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("RemoveSpaces target must be FileNameTarget.");
            return new RemoveSpacesFilter(enabled, t);
        }

        private static Filter _ParseShrinkSpaces(bool enabled, FilterTarget target)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("ShrinkSpaces target must be FileNameTarget.");
            return new ShrinkSpacesFilter(enabled, t);
        }

        private static Filter _ParseTrimLeft(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("TrimLeft target must be FileNameTarget.");
            var count = _GetIntRequired(optionsEl, "count");
            return new TrimLeftFilter(enabled, t, count);
        }

        private static Filter _ParseTrimRight(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("TrimRight target must be FileNameTarget.");
            var count = _GetIntRequired(optionsEl, "count");
            return new TrimRightFilter(enabled, t, count);
        }

        private static Filter _ParseExtractLeft(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractLeft target must be FileNameTarget.");
            var count = _GetIntRequired(optionsEl, "count");
            return new ExtractLeftFilter(enabled, t, count);
        }

        private static Filter _ParseExtractRight(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractRight target must be FileNameTarget.");
            var count = _GetIntRequired(optionsEl, "count");
            return new ExtractRightFilter(enabled, t, count);
        }

        private static Filter _ParseReplacer(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("Replacer target must be FileNameTarget.");
            var find = _GetStringRequired(optionsEl, "find");
            var replacement = _GetStringRequired(optionsEl, "replacement");
            var modeStr = _GetStringRequired(optionsEl, "mode");
            if (!Enum.TryParse(modeStr, ignoreCase: true, out ReplacerMode mode))
            {
                throw new InvalidOperationException($"Invalid Replacer mode '{modeStr}'.");
            }

            var caseSensitive = _GetBoolOrDefault(optionsEl, "caseSensitive", false);
            var replaceAll = _GetBoolOrDefault(optionsEl, "replaceAll", true);
            var wholeWord = _GetBoolOrDefault(optionsEl, "wholeWord", false);

            var opts = new ReplacerOptions(find, replacement, mode, caseSensitive, replaceAll, wholeWord);
            return new ReplacerFilter(enabled, t, opts);
        }

        private static Filter _ParseFormatter(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("Formatter target must be FileNameTarget.");
            var template = _GetStringRequired(optionsEl, "template");
            var opts = new FormatterOptions(template);
            return new FormatterFilter(enabled, t, opts);
        }

        private static Filter _ParseCounter(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("Counter target must be FileNameTarget.");

            var start = _GetIntRequired(optionsEl, "start");
            var step = _GetIntRequired(optionsEl, "step");
            var width = _GetIntRequired(optionsEl, "width");
            var padChar = _GetStringRequired(optionsEl, "padChar");
            var posStr = _GetStringRequired(optionsEl, "position");
            if (!Enum.TryParse(posStr, ignoreCase: true, out CounterPosition position))
            {
                throw new InvalidOperationException($"Invalid Counter position '{posStr}'.");
            }

            var separator = _GetStringRequired(optionsEl, "separator");
            var resetPerFolder = _GetBoolOrDefault(optionsEl, "resetPerFolder", false);

            var opts = new CounterOptions(start, step, width, padChar, position, separator, resetPerFolder);
            return new CounterFilter(enabled, t, opts);
        }

        private static Filter _ParseCleaner(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("Cleaner target must be FileNameTarget.");
            var removeIllegalChars = _GetBoolOrDefault(optionsEl, "removeIllegalChars", true);
            var illegalCharReplacement = _GetStringRequired(optionsEl, "illegalCharReplacement");
            var customCharsToRemove = _GetStringRequired(optionsEl, "customCharsToRemove");
            var customReplacement = _GetStringRequired(optionsEl, "customReplacement");
            var opts = new CleanerOptions(removeIllegalChars, illegalCharReplacement, customCharsToRemove, customReplacement);
            return new CleanerFilter(enabled, t, opts);
        }

        private static Filter _ParseFixLeadingZeros(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("FixLeadingZeros target must be FileNameTarget.");
            var width = _GetIntRequired(optionsEl, "width");
            var removeExtraZeros = _GetBoolOrDefault(optionsEl, "removeExtraZeros", false);
            var opts = new FixLeadingZerosOptions(width, removeExtraZeros);
            return new FixLeadingZerosFilter(enabled, t, opts);
        }

        private static Filter _ParseStripParentheses(bool enabled, FilterTarget target, JsonElement optionsEl)
        {
            var t = target as FileNameTarget ?? throw new InvalidOperationException("StripParentheses target must be FileNameTarget.");
            var types = _GetStringRequired(optionsEl, "types");
            var removeContents = _GetBoolOrDefault(optionsEl, "removeContents", true);
            var opts = new StripParenthesesOptions(types, removeContents);
            return new StripParenthesesFilter(enabled, t, opts);
        }

        private static string _GetStringRequired(JsonElement el, string propertyName)
        {
            if (!el.TryGetProperty(propertyName, out var p) || p.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"Missing required string '{propertyName}'.");
            }

            var v = p.GetString();
            return string.IsNullOrEmpty(v) ? throw new InvalidOperationException($"Property '{propertyName}' cannot be empty.") : v;
        }

        private static int _GetIntRequired(JsonElement el, string propertyName)
        {
            return !el.TryGetProperty(propertyName, out var p) || (p.ValueKind != JsonValueKind.Number && p.ValueKind != JsonValueKind.String)
                ? throw new InvalidOperationException($"Missing required int '{propertyName}'.")
                : p.ValueKind == JsonValueKind.Number ? p.GetInt32() : int.Parse(p.GetString()!);
        }

        private static bool _GetBoolOrDefault(JsonElement el, string propertyName, bool defaultValue)
        {
            return !el.TryGetProperty(propertyName, out var p)
                ? defaultValue
                : p.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => p.GetInt32() != 0,
                    JsonValueKind.String => bool.TryParse(p.GetString(), out var b) ? b : defaultValue,
                    JsonValueKind.Undefined => throw new NotImplementedException(),
                    JsonValueKind.Object => throw new NotImplementedException(),
                    JsonValueKind.Array => throw new NotImplementedException(),
                    JsonValueKind.Null => throw new NotImplementedException(),
                    _ => defaultValue
                };
        }
    }

}
