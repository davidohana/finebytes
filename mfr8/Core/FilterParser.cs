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
            String type = _GetStringRequired(filterEl, "type");
            Boolean enabled = (filterEl.TryGetProperty("enabled", out JsonElement enabledEl) && enabledEl.ValueKind == JsonValueKind.True) || !filterEl.TryGetProperty("enabled", out enabledEl) || enabledEl.ValueKind != JsonValueKind.False;

            if (!filterEl.TryGetProperty("target", out JsonElement targetEl) || targetEl.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Filter '{type}' missing target object.");
            }

            FilterTarget target = _ParseTarget(targetEl, type);

            JsonElement optionsEl = default;
            if (filterEl.TryGetProperty("options", out JsonElement optionsCandidate) && optionsCandidate.ValueKind == JsonValueKind.Object)
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

        private static FilterTarget _ParseTarget(JsonElement targetEl, String filterType)
        {
            String familyStr = _GetStringRequired(targetEl, "family");
            if (!String.Equals(familyStr, "FileName", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filterType}' got '{familyStr}'.");
            }

            String modeStr = _GetStringRequired(targetEl, "fileNameMode");
            return !Enum.TryParse<FileNameTargetMode>(modeStr, ignoreCase: true, out FileNameTargetMode mode)
                ? throw new InvalidOperationException($"Invalid fileNameMode '{modeStr}' for filter '{filterType}'.")
                : (FilterTarget)new FileNameTarget(mode);
        }

        private static Filter _ParseLettersCase(Boolean enabled, JsonElement targetEl, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("LettersCase target must be FileNameTarget.");

            String modeStr = _GetStringRequired(optionsEl, "mode");
            if (!Enum.TryParse<LettersCaseMode>(modeStr, ignoreCase: true, out LettersCaseMode mode))
            {
                throw new InvalidOperationException($"Invalid LettersCase mode '{modeStr}'.");
            }

            var skipWords = new List<String>();
            if (optionsEl.TryGetProperty("skipWords", out JsonElement skipEl) && skipEl.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement w in skipEl.EnumerateArray())
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

        private static Filter _ParseSpaceCharacter(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("SpaceCharacter target must be FileNameTarget.");
            String replaceSpaceWith = _GetStringRequired(optionsEl, "replaceSpaceWith");
            String replaceCharWithSpace = _GetStringRequired(optionsEl, "replaceCharWithSpace");
            var opts = new SpaceCharacterOptions(replaceSpaceWith, replaceCharWithSpace);
            return new SpaceCharacterFilter(enabled, t, opts);
        }

        private static Filter _ParseRemoveSpaces(Boolean enabled, FilterTarget target)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("RemoveSpaces target must be FileNameTarget.");
            return new RemoveSpacesFilter(enabled, t);
        }

        private static Filter _ParseShrinkSpaces(Boolean enabled, FilterTarget target)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("ShrinkSpaces target must be FileNameTarget.");
            return new ShrinkSpacesFilter(enabled, t);
        }

        private static Filter _ParseTrimLeft(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("TrimLeft target must be FileNameTarget.");
            Int32 count = _GetIntRequired(optionsEl, "count");
            return new TrimLeftFilter(enabled, t, count);
        }

        private static Filter _ParseTrimRight(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("TrimRight target must be FileNameTarget.");
            Int32 count = _GetIntRequired(optionsEl, "count");
            return new TrimRightFilter(enabled, t, count);
        }

        private static Filter _ParseExtractLeft(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractLeft target must be FileNameTarget.");
            Int32 count = _GetIntRequired(optionsEl, "count");
            return new ExtractLeftFilter(enabled, t, count);
        }

        private static Filter _ParseExtractRight(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("ExtractRight target must be FileNameTarget.");
            Int32 count = _GetIntRequired(optionsEl, "count");
            return new ExtractRightFilter(enabled, t, count);
        }

        private static Filter _ParseReplacer(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("Replacer target must be FileNameTarget.");
            String find = _GetStringRequired(optionsEl, "find");
            String replacement = _GetStringRequired(optionsEl, "replacement");
            String modeStr = _GetStringRequired(optionsEl, "mode");
            if (!Enum.TryParse<ReplacerMode>(modeStr, ignoreCase: true, out ReplacerMode mode))
            {
                throw new InvalidOperationException($"Invalid Replacer mode '{modeStr}'.");
            }

            Boolean caseSensitive = _GetBoolOrDefault(optionsEl, "caseSensitive", false);
            Boolean replaceAll = _GetBoolOrDefault(optionsEl, "replaceAll", true);
            Boolean wholeWord = _GetBoolOrDefault(optionsEl, "wholeWord", false);

            var opts = new ReplacerOptions(find, replacement, mode, caseSensitive, replaceAll, wholeWord);
            return new ReplacerFilter(enabled, t, opts);
        }

        private static Filter _ParseFormatter(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("Formatter target must be FileNameTarget.");
            String template = _GetStringRequired(optionsEl, "template");
            var opts = new FormatterOptions(template);
            return new FormatterFilter(enabled, t, opts);
        }

        private static Filter _ParseCounter(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("Counter target must be FileNameTarget.");

            Int32 start = _GetIntRequired(optionsEl, "start");
            Int32 step = _GetIntRequired(optionsEl, "step");
            Int32 width = _GetIntRequired(optionsEl, "width");
            String padChar = _GetStringRequired(optionsEl, "padChar");
            String posStr = _GetStringRequired(optionsEl, "position");
            if (!Enum.TryParse<CounterPosition>(posStr, ignoreCase: true, out CounterPosition position))
            {
                throw new InvalidOperationException($"Invalid Counter position '{posStr}'.");
            }

            String separator = _GetStringRequired(optionsEl, "separator");
            Boolean resetPerFolder = _GetBoolOrDefault(optionsEl, "resetPerFolder", false);

            var opts = new CounterOptions(start, step, width, padChar, position, separator, resetPerFolder);
            return new CounterFilter(enabled, t, opts);
        }

        private static Filter _ParseCleaner(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("Cleaner target must be FileNameTarget.");
            Boolean removeIllegalChars = _GetBoolOrDefault(optionsEl, "removeIllegalChars", true);
            String illegalCharReplacement = _GetStringRequired(optionsEl, "illegalCharReplacement");
            String customCharsToRemove = _GetStringRequired(optionsEl, "customCharsToRemove");
            String customReplacement = _GetStringRequired(optionsEl, "customReplacement");
            var opts = new CleanerOptions(removeIllegalChars, illegalCharReplacement, customCharsToRemove, customReplacement);
            return new CleanerFilter(enabled, t, opts);
        }

        private static Filter _ParseFixLeadingZeros(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("FixLeadingZeros target must be FileNameTarget.");
            Int32 width = _GetIntRequired(optionsEl, "width");
            Boolean removeExtraZeros = _GetBoolOrDefault(optionsEl, "removeExtraZeros", false);
            var opts = new FixLeadingZerosOptions(width, removeExtraZeros);
            return new FixLeadingZerosFilter(enabled, t, opts);
        }

        private static Filter _ParseStripParentheses(Boolean enabled, FilterTarget target, JsonElement optionsEl)
        {
            FileNameTarget t = target as FileNameTarget ?? throw new InvalidOperationException("StripParentheses target must be FileNameTarget.");
            String types = _GetStringRequired(optionsEl, "types");
            Boolean removeContents = _GetBoolOrDefault(optionsEl, "removeContents", true);
            var opts = new StripParenthesesOptions(types, removeContents);
            return new StripParenthesesFilter(enabled, t, opts);
        }

        private static String _GetStringRequired(JsonElement el, String propertyName)
        {
            if (!el.TryGetProperty(propertyName, out JsonElement p) || p.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"Missing required string '{propertyName}'.");
            }

            String? v = p.GetString();
            return String.IsNullOrEmpty(v) ? throw new InvalidOperationException($"Property '{propertyName}' cannot be empty.") : v;
        }

        private static Int32 _GetIntRequired(JsonElement el, String propertyName)
        {
            return !el.TryGetProperty(propertyName, out JsonElement p) || (p.ValueKind != JsonValueKind.Number && p.ValueKind != JsonValueKind.String)
                ? throw new InvalidOperationException($"Missing required int '{propertyName}'.")
                : p.ValueKind == JsonValueKind.Number ? p.GetInt32() : Int32.Parse(p.GetString()!);
        }

        private static Boolean _GetBoolOrDefault(JsonElement el, String propertyName, Boolean defaultValue)
        {
            return !el.TryGetProperty(propertyName, out JsonElement p)
                ? defaultValue
                : p.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => p.GetInt32() != 0,
                    JsonValueKind.String => Boolean.TryParse(p.GetString(), out Boolean b) ? b : defaultValue,
                    JsonValueKind.Undefined => throw new NotImplementedException(),
                    JsonValueKind.Object => throw new NotImplementedException(),
                    JsonValueKind.Array => throw new NotImplementedException(),
                    JsonValueKind.Null => throw new NotImplementedException(),
                    _ => defaultValue
                };
        }
    }

}
