using System.Text.RegularExpressions;

namespace Mfr8.Core
{
    public enum RenameStatus
    {
        Ok,
        Skipped,
        ConflictSkipped,
        Error
    }

    public sealed record RenameResultItem(
        string OriginalPath,
        string ResultPath,
        RenameStatus Status,
        string? Error);

    public sealed record RenameBatchResult(
        string PresetName,
        int TotalFiles,
        int Renamed,
        int Skipped,
        int Conflicts,
        int Errors,
        IReadOnlyList<RenameResultItem> Results);

    public static partial class FilterEngine
    {
        /// <summary>
        /// Previews rename outcomes for a batch and then commits non-conflicting moves.
        /// </summary>
        /// <param name="preset">The rename preset (sequence of enabled filters).</param>
        /// <param name="files">Candidate files to rename.</param>
        /// <param name="continueOnErrors">If <c>true</c>, continue when preview/commit errors occur.</param>
        /// <returns>Summary of rename outcomes (renamed, skipped, conflicts, errors).</returns>
        public static RenameBatchResult PreviewAndCommit(
            FilterPreset preset,
            IReadOnlyList<FileEntryLite> files,
            bool continueOnErrors)
        {
            // Phase 1: Conflict strategy is `Skip` (no auto-number, no overwrite).
            var previewResults = new List<RenameResultItem>(files.Count);
            var pending = new List<(FileEntryLite file, string destPath)>(files.Count);

            // 1) Preview and compute destinations (or preview errors).
            foreach (var file in files)
            {
                try
                {
                    (var prefix, var extension) = _ApplyFiltersToName(preset.Filters, file);
                    var finalFileName = prefix + extension;
                    var destPath = Path.Combine(file.DirectoryPath, finalFileName);

                    if (string.Equals(destPath, file.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        previewResults.Add(new RenameResultItem(file.FullPath, destPath, RenameStatus.Skipped, null));
                        continue;
                    }

                    previewResults.Add(new RenameResultItem(file.FullPath, destPath, RenameStatus.Skipped, null));
                    pending.Add((file, destPath));
                }
                catch (Exception ex)
                {
                    previewResults.Add(new RenameResultItem(file.FullPath, file.FullPath, RenameStatus.Error, ex.Message));
                    if (!continueOnErrors)
                    {
                        return _Summarize(preset.Name, files.Count, previewResults);
                    }
                }
            }

            // If there were preview errors and /COPE is not enabled, do not commit.
            if (previewResults.Any(r => r.Status == RenameStatus.Error))
            {
                return _Summarize(preset.Name, files.Count, previewResults);
            }

            // 2) Resolve conflicts among pending destinations and against disk.
            var destToFiles = pending.GroupBy(p => p.destPath, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var conflictDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((var file, var destPath) in pending)
            {
                if (File.Exists(destPath))
                {
                    _ = conflictDestinations.Add(destPath);
                }

                if (destToFiles.ContainsKey(destPath))
                {
                    _ = conflictDestinations.Add(destPath);
                }
            }

            // 3) Commit non-conflicting renames.
            var renamedCount = 0;
            var resultIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < previewResults.Count; i++)
            {
                resultIndex[previewResults[i].OriginalPath] = i;
            }

            foreach ((var file, var destPath) in pending)
            {
                var idx = resultIndex[file.FullPath];
                if (conflictDestinations.Contains(destPath))
                {
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.ConflictSkipped, null);
                    continue;
                }

                try
                {
                    _CommitMove(file.FullPath, destPath);
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.Ok, null);
                    renamedCount++;
                }
                catch (Exception ex)
                {
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.Error, ex.Message);
                    if (!continueOnErrors)
                    {
                        break;
                    }
                }
            }

            return _Summarize(preset.Name, files.Count, previewResults, renamedCount);
        }

        private static RenameBatchResult _Summarize(
            string presetName,
            int totalFiles,
            IReadOnlyList<RenameResultItem> results,
            int renamedCount = 0)
        {
            var renamed = results.Count(r => r.Status == RenameStatus.Ok);
            var skipped = results.Count(r => r.Status == RenameStatus.Skipped);
            var conflicts = results.Count(r => r.Status == RenameStatus.ConflictSkipped);
            var errors = results.Count(r => r.Status == RenameStatus.Error);
            return new RenameBatchResult(presetName, totalFiles, renamed, skipped, conflicts, errors, results);
        }

        private static (string prefix, string extension) _ApplyFiltersToName(IReadOnlyList<Filter> filters, FileEntryLite file)
        {
            var prefix = file.Prefix;
            var extension = file.Extension;

            foreach (var filter in filters)
            {
                if (!filter.Enabled)
                {
                    continue;
                }

                if (filter.Target is not FileNameTarget fileTarget)
                {
                    throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filter.Type}' got '{filter.Target.Family}'.");
                }

                var mode = fileTarget.FileNameMode;
                var segment = mode switch
                {
                    FileNameTargetMode.Prefix => prefix,
                    FileNameTargetMode.Extension => extension,
                    FileNameTargetMode.Full => prefix + extension,
                    _ => throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.")
                };

                var transformed = _ApplySingleFilter(filter, segment, file, prefix, extension);

                switch (mode)
                {
                    case FileNameTargetMode.Prefix:
                        prefix = transformed;
                        break;
                    case FileNameTargetMode.Extension:
                        extension = transformed;
                        break;
                    case FileNameTargetMode.Full:
                        var fullName = Path.GetFileName(transformed);
                        extension = Path.GetExtension(fullName);
                        prefix = Path.GetFileNameWithoutExtension(fullName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.");
                }
            }

            return (prefix, extension);
        }

        private static string _ApplySingleFilter(Filter filter, string segment, FileEntryLite file, string currentPrefix, string currentExtension)
        {
            return filter switch
            {
                LettersCaseFilter f => _ApplyLettersCase(segment, f.Options),
                SpaceCharacterFilter f => segment.Replace(" ", f.Options.ReplaceSpaceWith).Replace(f.Options.ReplaceCharWithSpace, " "),
                RemoveSpacesFilter => MyRegex().Replace(segment, ""),
                ShrinkSpacesFilter => MyRegex().Replace(segment, " "),
                TrimLeftFilter f => f.Count <= 0 ? segment : segment.Length <= f.Count ? "" : segment[f.Count..],
                TrimRightFilter f => f.Count <= 0 ? segment : segment.Length <= f.Count ? "" : segment[..^f.Count],
                ExtractLeftFilter f => f.Count <= 0 ? "" : segment.Length <= f.Count ? segment : segment[..f.Count],
                ExtractRightFilter f => f.Count <= 0 ? "" : segment.Length <= f.Count ? segment : segment[^f.Count..],
                ReplacerFilter f => _ApplyReplacer(segment, f.Options),
                FormatterFilter f => TokenFormatter.Format(f.Options.Template, file),
                CounterFilter f => _ApplyCounter(segment, file, f.Options),
                CleanerFilter f => _ApplyCleaner(segment, f.Options),
                FixLeadingZerosFilter f => _FixLeadingZeros(segment, f.Options),
                StripParenthesesFilter f => _StripParentheses(segment, f.Options),
                _ => throw new NotSupportedException($"Phase 1 does not support filter type '{filter.Type}'.")
            };
        }

        private static string _ApplyLettersCase(string input, LettersCaseOptions options)
        {
            // Phase 1 only requires reasonable correctness, not perfect linguistics.
            return options.Mode switch
            {
                LettersCaseMode.UpperCase => input.ToUpperInvariant(),
                LettersCaseMode.LowerCase => input.ToLowerInvariant(),
                LettersCaseMode.TitleCase => _ApplyTitleCase(input, options.SkipWords),
                LettersCaseMode.SentenceCase => _ApplySentenceCase(input),
                LettersCaseMode.InvertCase => _InvertCase(input),
                _ => input
            };
        }

        private static string _ApplyTitleCase(string input, IReadOnlyList<string> skipWords)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var skip = new HashSet<string>(skipWords, StringComparer.OrdinalIgnoreCase);

            // Capitalize word starts and lowercase the rest.
            return MyRegex1().Replace(input, m =>
            {
                var word = m.Value;
                return skip.Contains(word)
                    ? word.ToLowerInvariant()
                    : word.Length == 1 ? word.ToUpperInvariant() : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
            });
        }

        private static string _ApplySentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var lower = input.ToLowerInvariant();
            return Regex.Replace(lower, @"(^|[.!?]\s+)([a-z])", m =>
            {
                // Keep punctuation group as-is, uppercase the following letter.
                var prefix = m.Groups[1].Value;
                var ch = m.Groups[2].Value;
                return prefix + ch.ToUpperInvariant();
            });
        }

        private static string _InvertCase(string input)
        {
            var chars = input.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }
                else if (char.IsLower(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                }
            }
            return new string(chars);
        }

        private static string _ApplyReplacer(string input, ReplacerOptions options)
        {
            var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            var pattern = options.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(options.Find),
                ReplacerMode.Wildcard => _WildcardToRegex(options.Find),
                ReplacerMode.Regex => options.Find,
                _ => options.Find
            };

            if (options.WholeWord && options.Mode != ReplacerMode.Regex)
            {
                pattern = $@"\b(?:{pattern})\b";
            }
            else if (options.WholeWord && options.Mode == ReplacerMode.Regex)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            if (options.ReplaceAll)
            {
                return Regex.Replace(input, pattern, options.Replacement, regexOptions);
            }

            var regex = new Regex(pattern, regexOptions);
            return regex.Replace(input, options.Replacement, 1);
        }

        private static string _WildcardToRegex(string wildcard)
        {
            // Convert '*' -> '.*', '?' -> '.', and escape everything else.
            var sb = new System.Text.StringBuilder();
            foreach (var ch in wildcard)
            {
                _ = sb.Append(ch switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => Regex.Escape(ch.ToString())
                });
            }
            return sb.ToString();
        }

        private static string _ApplyCounter(string currentSegment, FileEntryLite file, CounterOptions options)
        {
            var n = options.ResetPerFolder ? file.FolderOccurrenceIndex : file.GlobalIndex;
            var value = options.Start + ((long)options.Step * n);

            var pad = options.PadChar switch
            {
                "0" => '0',
                "1" => ' ',
                _ => string.IsNullOrEmpty(options.PadChar) ? '0' : options.PadChar[0]
            };

            var raw = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var formatted = options.Width > 0 ? raw.PadLeft(options.Width, pad) : raw;

            return options.Position switch
            {
                CounterPosition.Replace => formatted,
                CounterPosition.Prepend => formatted + options.Separator + currentSegment,
                CounterPosition.Append => currentSegment + options.Separator + formatted,
                _ => currentSegment
            };
        }

        private static string _ApplyCleaner(string input, CleanerOptions options)
        {
            var res = input;
            if (options.RemoveIllegalChars)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    res = res.Replace(c.ToString(), options.IllegalCharReplacement);
                }
            }

            if (!string.IsNullOrEmpty(options.CustomCharsToRemove))
            {
                foreach (var c in options.CustomCharsToRemove)
                {
                    res = res.Replace(c.ToString(), options.CustomReplacement);
                }
            }

            return res;
        }

        private static string _FixLeadingZeros(string input, FixLeadingZerosOptions options)
        {
            return options.Width <= 0
                ? input
                : Regex.Replace(input, @"\d+", m =>
            {
                var digits = m.Value;
                if (options.RemoveExtraZeros)
                {
                    digits = digits.TrimStart('0');
                }

                if (digits.Length == 0)
                {
                    digits = "0";
                }

                return digits.PadLeft(options.Width, '0');
            });
        }

        private static string _StripParentheses(string input, StripParenthesesOptions options)
        {
            var pairs = new List<(char open, char close)>();
            foreach (var token in options.Types.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                switch (token.Trim())
                {
                    case "Round":
                        pairs.Add(('(', ')'));
                        break;
                    case "Square":
                        pairs.Add(('[', ']'));
                        break;
                    case "Curly":
                        pairs.Add(('{', '}'));
                        break;
                    case "Angle":
                        pairs.Add(('<', '>'));
                        break;
                    default:
                        break;
                }
            }

            var res = input;
            foreach ((var open, var close) in pairs)
            {
                if (open == '(' && close == ')')
                {
                    res = options.RemoveContents
                        ? Regex.Replace(res, @"\([^)]*\)", "")
                        : res.Replace("(", "").Replace(")", "");
                }
                else if (open == '[' && close == ']')
                {
                    res = options.RemoveContents
                        ? Regex.Replace(res, @"\[[^\]]*\]", "")
                        : res.Replace("[", "").Replace("]", "");
                }
                else if (open == '{' && close == '}')
                {
                    res = options.RemoveContents
                        ? Regex.Replace(res, @"\{[^}]*\}", "")
                        : res.Replace("{", "").Replace("}", "");
                }
                else if (open == '<' && close == '>')
                {
                    res = options.RemoveContents
                        ? Regex.Replace(res, @"<[^>]*>", "")
                        : res.Replace("<", "").Replace(">", "");
                }
            }

            return res;
        }

        private static void _CommitMove(string sourcePath, string destPath)
        {
            // Keep it simple for phase 1: try Move first, fallback to Copy+Delete.
            try
            {
                File.Move(sourcePath, destPath, overwrite: false);
            }
            catch (IOException)
            {
                // Cross-volume or other move failure -> fallback.
                File.Copy(sourcePath, destPath, overwrite: false);
                File.Delete(sourcePath);
            }
        }

        private sealed partial class TokenFormatter
        {
            private static readonly Regex TokenRegex = MyRegex();

            /// <summary>
            /// Expands formatter tokens in <paramref name="template"/> for a given <paramref name="file"/>.
            /// </summary>
            /// <param name="template">The formatter template containing tokens like <c>&lt;file-name&gt;</c>.</param>
            /// <param name="file">The file being renamed (provides token values).</param>
            /// <returns>The template with all recognized tokens replaced.</returns>
            public static string Format(string template, FileEntryLite file)
            {
                return TokenRegex.Replace(template, m => _ResolveToken(m.Groups[1].Value, file));
            }

            private static string _ResolveToken(string tokenInner, FileEntryLite file)
            {
                var parts = tokenInner.Split(':', 2);
                var name = parts[0];
                var arg = parts.Length == 2 ? parts[1] : "";

                return name switch
                {
                    "file-name" => file.Prefix,
                    "file-ext" => file.Extension,
                    "ext" => file.Extension,
                    "full-name" => file.Prefix + file.Extension,
                    "parent-folder" => Path.GetFileName(file.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    "full-path" => file.FullPath,
                    "now" => string.IsNullOrWhiteSpace(arg) ? DateTimeOffset.UtcNow.ToString("o") : DateTimeOffset.UtcNow.ToString(arg),
                    "counter" => _ResolveCounterToken(arg, file),
                    _ => throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.")
                };
            }

            private static string _ResolveCounterToken(string arg, FileEntryLite file)
            {
                // Syntax: start,step,reset,width,pad
                var parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5)
                {
                    throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");
                }

                var start = int.Parse(parts[0]);
                var step = int.Parse(parts[1]);
                var reset = int.Parse(parts[2]);
                var width = int.Parse(parts[3]);
                var pad = int.Parse(parts[4]);

                var n = reset == 1 ? file.FolderOccurrenceIndex : file.GlobalIndex;
                var value = start + ((long)step * n);
                var raw = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (width <= 0)
                {
                    return raw;
                }

                var padChar = pad == 0 ? '0' : ' ';
                return raw.PadLeft(width, padChar);
            }

            [GeneratedRegex(@"<([^<>]+)>", RegexOptions.Compiled)]
            private static partial Regex MyRegex();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"\b[0-9A-Za-z']+\b")]
        private static partial Regex MyRegex1();
    }

}
