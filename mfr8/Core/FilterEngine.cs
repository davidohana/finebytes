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
        String OriginalPath,
        String ResultPath,
        RenameStatus Status,
        String? Error);

    public sealed record RenameBatchResult(
        String PresetName,
        Int32 TotalFiles,
        Int32 Renamed,
        Int32 Skipped,
        Int32 Conflicts,
        Int32 Errors,
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
            Boolean continueOnErrors)
        {
            // Phase 1: Conflict strategy is `Skip` (no auto-number, no overwrite).
            var previewResults = new List<RenameResultItem>(files.Count);
            var pending = new List<(FileEntryLite file, String destPath)>(files.Count);

            // 1) Preview and compute destinations (or preview errors).
            foreach (FileEntryLite file in files)
            {
                try
                {
                    (String? prefix, String? extension) = _ApplyFiltersToName(preset.Filters, file);
                    String finalFileName = prefix + extension;
                    String destPath = Path.Combine(file.DirectoryPath, finalFileName);

                    if (String.Equals(destPath, file.FullPath, StringComparison.OrdinalIgnoreCase))
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

            var conflictDestinations = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach ((FileEntryLite? file, String? destPath) in pending)
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
            Int32 renamedCount = 0;
            var resultIndex = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
            for (Int32 i = 0; i < previewResults.Count; i++)
            {
                resultIndex[previewResults[i].OriginalPath] = i;
            }

            foreach ((FileEntryLite? file, String? destPath) in pending)
            {
                Int32 idx = resultIndex[file.FullPath];
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
            String presetName,
            Int32 totalFiles,
            IReadOnlyList<RenameResultItem> results,
            Int32 renamedCount = 0)
        {
            Int32 renamed = results.Count(r => r.Status == RenameStatus.Ok);
            Int32 skipped = results.Count(r => r.Status == RenameStatus.Skipped);
            Int32 conflicts = results.Count(r => r.Status == RenameStatus.ConflictSkipped);
            Int32 errors = results.Count(r => r.Status == RenameStatus.Error);
            return new RenameBatchResult(presetName, totalFiles, renamed, skipped, conflicts, errors, results);
        }

        private static (String prefix, String extension) _ApplyFiltersToName(IReadOnlyList<Filter> filters, FileEntryLite file)
        {
            String prefix = file.Prefix;
            String extension = file.Extension;

            foreach (Filter filter in filters)
            {
                if (!filter.Enabled)
                {
                    continue;
                }

                if (filter.Target is not FileNameTarget fileTarget)
                {
                    throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filter.Type}' got '{filter.Target.Family}'.");
                }

                FileNameTargetMode mode = fileTarget.FileNameMode;
                String segment = mode switch
                {
                    FileNameTargetMode.Prefix => prefix,
                    FileNameTargetMode.Extension => extension,
                    FileNameTargetMode.Full => prefix + extension,
                    _ => throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.")
                };

                String transformed = _ApplySingleFilter(filter, segment, file, prefix, extension);

                switch (mode)
                {
                    case FileNameTargetMode.Prefix:
                        prefix = transformed;
                        break;
                    case FileNameTargetMode.Extension:
                        extension = transformed;
                        break;
                    case FileNameTargetMode.Full:
                        String fullName = Path.GetFileName(transformed);
                        extension = Path.GetExtension(fullName);
                        prefix = Path.GetFileNameWithoutExtension(fullName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.");
                }
            }

            return (prefix, extension);
        }

        private static String _ApplySingleFilter(Filter filter, String segment, FileEntryLite file, String currentPrefix, String currentExtension)
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

        private static String _ApplyLettersCase(String input, LettersCaseOptions options)
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

        private static String _ApplyTitleCase(String input, IReadOnlyList<String> skipWords)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var skip = new HashSet<String>(skipWords, StringComparer.OrdinalIgnoreCase);

            // Capitalize word starts and lowercase the rest.
            return Regex.Replace(input, @"\b[0-9A-Za-z']+\b", m =>
            {
                String word = m.Value;
                return skip.Contains(word)
                    ? word.ToLowerInvariant()
                    : word.Length == 1 ? word.ToUpperInvariant() : Char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
            });
        }

        private static String _ApplySentenceCase(String input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            String lower = input.ToLowerInvariant();
            return Regex.Replace(lower, @"(^|[.!?]\s+)([a-z])", m =>
            {
                // Keep punctuation group as-is, uppercase the following letter.
                String prefix = m.Groups[1].Value;
                String ch = m.Groups[2].Value;
                return prefix + ch.ToUpperInvariant();
            });
        }

        private static String _InvertCase(String input)
        {
            Char[] chars = input.ToCharArray();
            for (Int32 i = 0; i < chars.Length; i++)
            {
                if (Char.IsUpper(chars[i]))
                {
                    chars[i] = Char.ToLowerInvariant(chars[i]);
                }
                else if (Char.IsLower(chars[i]))
                {
                    chars[i] = Char.ToUpperInvariant(chars[i]);
                }
            }
            return new String(chars);
        }

        private static String _ApplyReplacer(String input, ReplacerOptions options)
        {
            RegexOptions regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            String pattern = options.Mode switch
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

        private static String _WildcardToRegex(String wildcard)
        {
            // Convert '*' -> '.*', '?' -> '.', and escape everything else.
            var sb = new System.Text.StringBuilder();
            foreach (Char ch in wildcard)
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

        private static String _ApplyCounter(String currentSegment, FileEntryLite file, CounterOptions options)
        {
            Int32 n = options.ResetPerFolder ? file.FolderOccurrenceIndex : file.GlobalIndex;
            Int64 value = options.Start + ((Int64)options.Step * n);

            Char pad = options.PadChar switch
            {
                "0" => '0',
                "1" => ' ',
                _ => String.IsNullOrEmpty(options.PadChar) ? '0' : options.PadChar[0]
            };

            String raw = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            String formatted = options.Width > 0 ? raw.PadLeft(options.Width, pad) : raw;

            return options.Position switch
            {
                CounterPosition.Replace => formatted,
                CounterPosition.Prepend => formatted + options.Separator + currentSegment,
                CounterPosition.Append => currentSegment + options.Separator + formatted,
                _ => currentSegment
            };
        }

        private static String _ApplyCleaner(String input, CleanerOptions options)
        {
            String res = input;
            if (options.RemoveIllegalChars)
            {
                foreach (Char c in Path.GetInvalidFileNameChars())
                {
                    res = res.Replace(c.ToString(), options.IllegalCharReplacement);
                }
            }

            if (!String.IsNullOrEmpty(options.CustomCharsToRemove))
            {
                foreach (Char c in options.CustomCharsToRemove)
                {
                    res = res.Replace(c.ToString(), options.CustomReplacement);
                }
            }

            return res;
        }

        private static String _FixLeadingZeros(String input, FixLeadingZerosOptions options)
        {
            return options.Width <= 0
                ? input
                : Regex.Replace(input, @"\d+", m =>
            {
                String digits = m.Value;
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

        private static String _StripParentheses(String input, StripParenthesesOptions options)
        {
            var pairs = new List<(Char open, Char close)>();
            foreach (String token in options.Types.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
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

            String res = input;
            foreach ((Char open, Char close) in pairs)
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

        private static void _CommitMove(String sourcePath, String destPath)
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

        private sealed class TokenFormatter
        {
            private static readonly Regex TokenRegex = new(@"<([^<>]+)>", RegexOptions.Compiled);

            /// <summary>
            /// Expands formatter tokens in <paramref name="template"/> for a given <paramref name="file"/>.
            /// </summary>
            /// <param name="template">The formatter template containing tokens like <c>&lt;file-name&gt;</c>.</param>
            /// <param name="file">The file being renamed (provides token values).</param>
            /// <returns>The template with all recognized tokens replaced.</returns>
            public static String Format(String template, FileEntryLite file)
            {
                return TokenRegex.Replace(template, m => _ResolveToken(m.Groups[1].Value, file));
            }

            private static String _ResolveToken(String tokenInner, FileEntryLite file)
            {
                String[] parts = tokenInner.Split(':', 2);
                String name = parts[0];
                String arg = parts.Length == 2 ? parts[1] : "";

                return name switch
                {
                    "file-name" => file.Prefix,
                    "file-ext" => file.Extension,
                    "ext" => file.Extension,
                    "full-name" => file.Prefix + file.Extension,
                    "parent-folder" => Path.GetFileName(file.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    "full-path" => file.FullPath,
                    "now" => String.IsNullOrWhiteSpace(arg) ? DateTimeOffset.UtcNow.ToString("o") : DateTimeOffset.UtcNow.ToString(arg),
                    "counter" => _ResolveCounterToken(arg, file),
                    _ => throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.")
                };
            }

            private static String _ResolveCounterToken(String arg, FileEntryLite file)
            {
                // Syntax: start,step,reset,width,pad
                String[] parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5)
                {
                    throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");
                }

                Int32 start = Int32.Parse(parts[0]);
                Int32 step = Int32.Parse(parts[1]);
                Int32 reset = Int32.Parse(parts[2]);
                Int32 width = Int32.Parse(parts[3]);
                Int32 pad = Int32.Parse(parts[4]);

                Int32 n = reset == 1 ? file.FolderOccurrenceIndex : file.GlobalIndex;
                Int64 value = start + ((Int64)step * n);
                String raw = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (width <= 0)
                {
                    return raw;
                }

                Char padChar = pad == 0 ? '0' : ' ';
                return raw.PadLeft(width, padChar);
            }
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex();
    }

}
