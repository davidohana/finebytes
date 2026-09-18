using System.Text.RegularExpressions;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Tests.Help
{
    /// <summary>
    /// Ensures Help under repo-root <c>help/</c> stays linked and catalog-aligned: every
    /// <c>href</c> targets an existing file (and fragment id when present), public format tokens
    /// appear from <c>fp.html</c>, and <c>fields.html</c> Write/Preview markers match
    /// <see cref="RenameListFieldCatalog"/> for documented groups.
    /// </summary>
    public sealed partial class HelpLinkIntegrityTests
    {
        [GeneratedRegex(
            """href\s*=\s*(?:["'](?<url>[^"']+)["']|(?<url>[^\s>]+))""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex _HrefRegex();

        [GeneratedRegex("""id\s*=\s*["'](?<id>[^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex _HtmlIdRegex();

        [GeneratedRegex(
            """<h2\s+id\s*=\s*["'](?<id>[^"']+)["'][^>]*>.*?</h2>(?<body>.*?)(?=<h2\s|\z)""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline
        )]
        private static partial Regex _FieldsSectionRegex();

        [GeneratedRegex(
            """<tr>\s*<td>(?<name>.*?)</td>\s*<td\s+class\s*=\s*["']center["']\s*>(?<write>.*?)</td>\s*<td\s+class\s*=\s*["']center["']\s*>(?<preview>.*?)</td>""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline
        )]
        private static partial Regex _WritePreviewRowRegex();

        /// <summary>
        /// Verifies relative Help <c>href</c> values resolve to files under <c>help/</c>.
        /// </summary>
        [Fact]
        public void Every_Html_Href_Resolves_To_Existing_File_Or_Allowed_Absolute_Url()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var htmlFiles = Directory
                .EnumerateFiles(helpRoot, "*.html", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.NotEmpty(htmlFiles);

            var broken = new List<string>();
            foreach (var htmlPath in htmlFiles)
            {
                var html = File.ReadAllText(htmlPath);
                foreach (Match match in _HrefRegex().Matches(html))
                {
                    var href = match.Groups["url"].Value.Trim();
                    if (href.Length == 0 || _IsAllowedAbsoluteUrl(href))
                    {
                        continue;
                    }

                    if (!_TryResolveLocalTarget(helpRoot, htmlPath, href, out var targetPath))
                    {
                        broken.Add($"{_RelPath(helpRoot, htmlPath)} → {href}");
                        continue;
                    }

                    if (!File.Exists(targetPath))
                    {
                        broken.Add($"{_RelPath(helpRoot, htmlPath)} → {href} (missing '{targetPath}')");
                    }
                }
            }

            Assert.True(
                broken.Count == 0,
                "Broken Help href(s):" + Environment.NewLine + string.Join(Environment.NewLine, broken)
            );
        }

        /// <summary>
        /// Verifies every Help <c>href</c> fragment targets an existing <c>id</c> on the resolved page.
        /// </summary>
        [Fact]
        public void Every_Html_Href_Fragment_Resolves_To_Existing_Id()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var htmlFiles = Directory
                .EnumerateFiles(helpRoot, "*.html", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.NotEmpty(htmlFiles);

            var pathToIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var broken = new List<string>();
            foreach (var htmlPath in htmlFiles)
            {
                var html = File.ReadAllText(htmlPath);
                foreach (Match match in _HrefRegex().Matches(html))
                {
                    var href = match.Groups["url"].Value.Trim();
                    if (href.Length == 0 || _IsAllowedAbsoluteUrl(href))
                    {
                        continue;
                    }

                    var hashIndex = href.IndexOf('#');
                    if (hashIndex < 0)
                    {
                        continue;
                    }

                    var fragment = href[(hashIndex + 1)..];
                    if (fragment.Length == 0)
                    {
                        continue;
                    }

                    if (
                        !_TryResolveLocalTarget(helpRoot, htmlPath, href, out var targetPath)
                        || !File.Exists(targetPath)
                    )
                    {
                        // File existence is covered by Every_Html_Href_Resolves_To_Existing_File_Or_Allowed_Absolute_Url.
                        continue;
                    }

                    if (!_TryGetHtmlIds(pathToIds, targetPath, out var ids))
                    {
                        broken.Add($"{_RelPath(helpRoot, htmlPath)} → {href} (unreadable '{targetPath}')");
                        continue;
                    }

                    if (!ids.Contains(fragment))
                    {
                        broken.Add($"{_RelPath(helpRoot, htmlPath)} → {href} (missing id '{fragment}')");
                    }
                }
            }

            Assert.True(
                broken.Count == 0,
                "Broken Help fragment(s):" + Environment.NewLine + string.Join(Environment.NewLine, broken)
            );
        }

        /// <summary>
        /// Verifies each public Format Editor token's canonical name appears in the Formatting Tokens
        /// Help tree rooted at <c>fp.html</c>.
        /// </summary>
        [Fact]
        public void Every_Public_Format_Token_Is_Documented_From_Fp_Help_Tree()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var fpPath = Path.Combine(helpRoot, "tokens", "fp.html");
            Assert.True(File.Exists(fpPath), "Expected help/tokens/fp.html");

            var pageToIsIncluded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { [fpPath] = true };
            _CollectLinkedHelpPages(helpRoot, fpPath, pageToIsIncluded);

            var corpus = string.Join(
                '\n',
                pageToIsIncluded.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Select(File.ReadAllText)
            );

            var missing = FormatTokenCatalog
                .Entries.Select(entry => entry.CanonicalName)
                .Where(name => !_CorpusMentionsToken(corpus, name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.True(
                missing.Count == 0,
                "Format tokens missing from fp.html Help tree:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, missing)
            );
        }

        /// <summary>
        /// Verifies <c>fields.html</c> Write/Preview markers for File Name and File Properties match
        /// <see cref="RenameListFieldCatalog"/> (<c>+</c> vs empty cell).
        /// </summary>
        [Fact]
        public void Fields_Html_Write_Preview_Markers_Match_RenameListFieldCatalog_Basic_And_Extended()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var fieldsPath = Path.Combine(helpRoot, "reference", "fields.html");
            Assert.True(File.Exists(fieldsPath), "Expected help/reference/fields.html");

            var html = File.ReadAllText(fieldsPath);
            var sectionIdToRows = _ParseWritePreviewSections(html);
            Assert.True(sectionIdToRows.ContainsKey("basic"), "Expected fields.html#basic Write/Preview table");
            Assert.True(sectionIdToRows.ContainsKey("extended"), "Expected fields.html#extended Write/Preview table");

            var mismatches = new List<string>();
            _AssertGroupMatchesHelp(
                mismatches,
                groupId: BasicRenameListField.Group,
                sectionId: "basic",
                sectionIdToRows
            );
            _AssertGroupMatchesHelp(
                mismatches,
                groupId: ExtendedRenameListFields.Group,
                sectionId: "extended",
                sectionIdToRows
            );

            Assert.True(
                mismatches.Count == 0,
                "fields.html Write/Preview drift vs RenameListFieldCatalog:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, mismatches)
            );
        }

        private static void _AssertGroupMatchesHelp(
            List<string> mismatches,
            string groupId,
            string sectionId,
            Dictionary<string, Dictionary<string, (bool Write, bool Preview)>> sectionIdToRows
        )
        {
            var helpRows = sectionIdToRows[sectionId];
            var catalogFields = RenameListFieldCatalog.GetFieldsForGroup(groupId);
            var catalogNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in catalogFields)
            {
                catalogNames.Add(field.DisplayName);
                if (!helpRows.TryGetValue(field.DisplayName, out var markers))
                {
                    mismatches.Add($"{groupId}/{field.DisplayName}: missing from fields.html#{sectionId}");
                    continue;
                }

                if (markers.Write != field.SupportsWrite)
                {
                    mismatches.Add(
                        $"{groupId}/{field.DisplayName}: Write help={_MarkerLabel(markers.Write)} catalog={_MarkerLabel(field.SupportsWrite)}"
                    );
                }

                if (markers.Preview != field.SupportsPreview)
                {
                    mismatches.Add(
                        $"{groupId}/{field.DisplayName}: Preview help={_MarkerLabel(markers.Preview)} catalog={_MarkerLabel(field.SupportsPreview)}"
                    );
                }
            }

            foreach (var helpName in helpRows.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                if (!catalogNames.Contains(helpName))
                {
                    mismatches.Add($"{groupId}: fields.html#{sectionId} has unknown field '{helpName}'");
                }
            }
        }

        private static string _MarkerLabel(bool supported) => supported ? "+" : "empty";

        private static Dictionary<string, Dictionary<string, (bool Write, bool Preview)>> _ParseWritePreviewSections(
            string html
        )
        {
            var sectionIdToRows = new Dictionary<string, Dictionary<string, (bool Write, bool Preview)>>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (Match section in _FieldsSectionRegex().Matches(html))
            {
                var sectionId = section.Groups["id"].Value;
                var body = section.Groups["body"].Value;
                if (
                    !body.Contains("Write", StringComparison.OrdinalIgnoreCase)
                    || !body.Contains("Preview", StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                var nameToMarkers = new Dictionary<string, (bool Write, bool Preview)>(StringComparer.Ordinal);
                foreach (Match row in _WritePreviewRowRegex().Matches(body))
                {
                    var name = _StripHtml(row.Groups["name"].Value).Trim();
                    if (name.Length == 0)
                    {
                        continue;
                    }

                    var write = _CellHasPlus(row.Groups["write"].Value);
                    var preview = _CellHasPlus(row.Groups["preview"].Value);
                    nameToMarkers[name] = (write, preview);
                }

                if (nameToMarkers.Count > 0)
                {
                    sectionIdToRows[sectionId] = nameToMarkers;
                }
            }

            return sectionIdToRows;
        }

        private static bool _CellHasPlus(string cellHtml) =>
            _StripHtml(cellHtml).Contains('+', StringComparison.Ordinal);

        private static string _StripHtml(string value)
        {
            var result = value;
            while (true)
            {
                var start = result.IndexOf('<');
                if (start < 0)
                {
                    break;
                }

                var end = result.IndexOf('>', start + 1);
                if (end < 0)
                {
                    break;
                }

                result = result.Remove(start, end - start + 1);
            }

            return result;
        }

        private static bool _TryGetHtmlIds(
            Dictionary<string, HashSet<string>> pathToIds,
            string htmlPath,
            out HashSet<string> ids
        )
        {
            if (pathToIds.TryGetValue(htmlPath, out ids!))
            {
                return true;
            }

            try
            {
                var html = File.ReadAllText(htmlPath);
                ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match match in _HtmlIdRegex().Matches(html))
                {
                    ids.Add(match.Groups["id"].Value);
                }

                pathToIds[htmlPath] = ids;
                return true;
            }
            catch (IOException)
            {
                ids = [];
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                ids = [];
                return false;
            }
        }

        private static bool _CorpusMentionsToken(string corpus, string canonicalName)
        {
            // Pages encode angle brackets as entities in signatures: &lt;file-name&gt;
            return corpus.Contains("&lt;" + canonicalName, StringComparison.Ordinal)
                || corpus.Contains("<" + canonicalName, StringComparison.Ordinal);
        }

        private static void _CollectLinkedHelpPages(
            string helpRoot,
            string sourceHtmlPath,
            Dictionary<string, bool> pageToIsIncluded
        )
        {
            var html = File.ReadAllText(sourceHtmlPath);
            foreach (Match match in _HrefRegex().Matches(html))
            {
                var href = match.Groups["url"].Value.Trim();
                if (href.Length == 0 || _IsAllowedAbsoluteUrl(href))
                {
                    continue;
                }

                if (!_TryResolveLocalTarget(helpRoot, sourceHtmlPath, href, out var targetPath))
                {
                    continue;
                }

                if (!File.Exists(targetPath))
                {
                    continue;
                }

                if (!targetPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!pageToIsIncluded.TryAdd(targetPath, true))
                {
                    continue;
                }

                // Recurse into every linked *fp.html hub/leaf so nested token hubs stay in the corpus.
                if (!Path.GetFileName(targetPath).EndsWith("fp.html", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _CollectLinkedHelpPages(helpRoot, targetPath, pageToIsIncluded);
            }
        }

        private static bool _IsAllowedAbsoluteUrl(string href)
        {
            return href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool _TryResolveLocalTarget(
            string helpRoot,
            string sourceHtmlPath,
            string href,
            out string targetPath
        )
        {
            targetPath = string.Empty;
            var hashIndex = href.IndexOf('#');
            var pathPart = hashIndex >= 0 ? href[..hashIndex] : href;
            if (pathPart.Length == 0)
            {
                targetPath = sourceHtmlPath;
                return true;
            }

            // Reject scheme-like or rooted paths outside the Help tree.
            if (pathPart.Contains(':') || Path.IsPathRooted(pathPart))
            {
                return false;
            }

            var combined = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceHtmlPath)!, pathPart));
            var helpRootFull = Path.GetFullPath(helpRoot);
            if (
                !combined.StartsWith(helpRootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(combined, helpRootFull, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            targetPath = combined;
            return true;
        }

        private static string _RelPath(string helpRoot, string fullPath)
        {
            var rootFull = Path.GetFullPath(helpRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var pathFull = Path.GetFullPath(fullPath);
            if (
                pathFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || pathFull.StartsWith(rootFull + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            )
            {
                return pathFull[(rootFull.Length + 1)..].Replace('\\', '/');
            }

            return Path.GetFileName(fullPath);
        }

        private static string _ResolveRepoHelpRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "help");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "index.html")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate repo-root help/ (expected index.html) by walking up from BaseDirectory."
            );
        }
    }
}
