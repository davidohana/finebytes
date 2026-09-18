using System.Text.RegularExpressions;
using Mfr.Filters;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Id3v1;
using Mfr.Models.RenameList.Fields.Id3v2;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mp3;
using Mfr.Models.RenameList.Fields.Pdf;
using Mfr.Models.RenameList.Fields.Xiph;

namespace Mfr.Tests.Help
{
    /// <summary>
    /// Ensures Help under repo-root <c>help/</c> stays linked and catalog-aligned: every
    /// <c>href</c> targets an existing file (and fragment id when present), public format tokens
    /// appear from <c>fp.html</c>, the filters hub matches <see cref="FilterCatalog"/>,
    /// every HTML page is reachable from Index, and <c>fields.html</c> Write/Preview markers
    /// match <see cref="RenameListFieldCatalog"/> for documented groups.
    /// </summary>
    public sealed partial class HelpLinkIntegrityTests
    {
        /// <summary>
        /// Relative paths under <c>help/</c> that may exist without a link path from
        /// <c>index.html</c> (intentional orphans only).
        /// </summary>
        private static readonly HashSet<string> _IndexReachabilityOrphanAllowlist = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Catalog group ids that intentionally have no Write/Preview table in
        /// <c>fields.html</c> (prose or Field/Description only).
        /// </summary>
        private static readonly HashSet<string> _GroupsWithoutWritePreviewTable = new(StringComparer.Ordinal)
        {
            AudioTagRenameListFields.Group,
            Id3v1RenameListFields.Group,
            Id3v2RenameListFields.Group,
            XiphRenameListFields.Group,
            MediaRenameListFields.Group,
            Mp3RenameListFields.Group,
            ImageRenameListFields.Group,
            JpegRenameListFields.Group,
            PdfRenameListFields.Group,
        };

        /// <summary>
        /// Help <c>h2</c> section id → catalog group id for Write/Preview tables in
        /// <c>fields.html</c>.
        /// </summary>
        private static readonly Dictionary<string, string> _FieldsWritePreviewSectionToGroup = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["basic"] = BasicRenameListField.Group,
            ["extended"] = ExtendedRenameListFields.Group,
        };

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

        [GeneratedRegex(
            """<a\s+href\s*=\s*["'](?<href>[^"']+)["'][^>]*>(?<text>.*?)</a>""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline
        )]
        private static partial Regex _AnchorRegex();

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
        /// Verifies <c>filters/filters.html</c> lists every <see cref="FilterCatalog"/> entry under
        /// the matching group section (and has no stale filter links).
        /// </summary>
        [Fact]
        public void Filters_Hub_Links_Match_FilterCatalog_By_Group_And_Type()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var hubPath = Path.Combine(helpRoot, "filters", "filters.html");
            Assert.True(File.Exists(hubPath), "Expected help/filters/filters.html");

            var sectionIdToLinks = _ParseFilterHubSections(File.ReadAllText(hubPath));
            var issues = new List<string>();

            var knownGroupIds = Enum.GetValues<FilterGroup>()
                .Select(group => group.ToString().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var sectionId in sectionIdToLinks.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
            {
                if (!knownGroupIds.Contains(sectionId))
                {
                    issues.Add($"unknown hub section #{sectionId} (not a FilterGroup)");
                }
            }

            foreach (var entry in FilterCatalog.Entries.OrderBy(e => e.Type, StringComparer.Ordinal))
            {
                var sectionId = entry.Group.ToString().ToLowerInvariant();
                var expectedHref = $"{sectionId}/{entry.HelpFileName}";
                if (!sectionIdToLinks.TryGetValue(sectionId, out var links))
                {
                    issues.Add($"{entry.Type}: missing hub section #{sectionId}");
                    continue;
                }

                var (Href, Text) = links.Find(link =>
                    string.Equals(link.Href, expectedHref, StringComparison.OrdinalIgnoreCase)
                );
                if (Href is null)
                {
                    issues.Add($"{entry.Type}: missing hub link '{expectedHref}'");
                    continue;
                }

                if (!string.Equals(Text, entry.DisplayName, StringComparison.Ordinal))
                {
                    issues.Add($"{entry.Type}: hub label '{Text}' != catalog DisplayName '{entry.DisplayName}'");
                }
            }

            var catalogTypeToEntry = FilterCatalog.Entries.ToDictionary(entry => entry.Type, StringComparer.Ordinal);
            foreach (var (sectionId, links) in sectionIdToLinks)
            {
                foreach (var (Href, Text) in links)
                {
                    var typeName = Path.GetFileNameWithoutExtension(Href.Replace('\\', '/'));
                    if (typeName.Length == 0)
                    {
                        issues.Add($"#{sectionId}: empty filter href '{Href}'");
                        continue;
                    }

                    if (!catalogTypeToEntry.TryGetValue(typeName, out var entry))
                    {
                        issues.Add($"#{sectionId}: stale hub link '{Href}' (not in FilterCatalog)");
                        continue;
                    }

                    var expectedSection = entry.Group.ToString().ToLowerInvariant();
                    if (!string.Equals(sectionId, expectedSection, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add($"{entry.Type}: hub section #{sectionId} != catalog group '{entry.Group}'");
                    }

                    var expectedHref = $"{expectedSection}/{entry.HelpFileName}";
                    if (!string.Equals(Href, expectedHref, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add($"{entry.Type}: hub href '{Href}' != '{expectedHref}'");
                    }
                }
            }

            Assert.True(
                issues.Count == 0,
                "filters.html hub vs FilterCatalog:" + Environment.NewLine + string.Join(Environment.NewLine, issues)
            );
        }

        /// <summary>
        /// Verifies every shipped Help HTML page is reachable by following relative links from
        /// <c>index.html</c> (or is listed in the intentional orphan allowlist).
        /// </summary>
        [Fact]
        public void Every_Html_Page_Is_Reachable_From_Index()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var indexPath = Path.GetFullPath(Path.Combine(helpRoot, "index.html"));
            Assert.True(File.Exists(indexPath), "Expected help/index.html");

            var allHtmlPaths = Directory
                .EnumerateFiles(helpRoot, "*.html", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .ToList();
            Assert.NotEmpty(allHtmlPaths);

            var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { indexPath };
            var queue = new Queue<string>();
            queue.Enqueue(indexPath);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (Match match in _HrefRegex().Matches(File.ReadAllText(current)))
                {
                    var href = match.Groups["url"].Value.Trim();
                    if (href.Length == 0 || _IsAllowedAbsoluteUrl(href))
                    {
                        continue;
                    }

                    if (
                        !_TryResolveLocalTarget(helpRoot, current, href, out var targetPath) || !File.Exists(targetPath)
                    )
                    {
                        continue;
                    }

                    if (!targetPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fullTarget = Path.GetFullPath(targetPath);
                    if (!reachable.Add(fullTarget))
                    {
                        continue;
                    }

                    queue.Enqueue(fullTarget);
                }
            }

            var orphans = allHtmlPaths
                .Where(path => !reachable.Contains(path))
                .Select(path => _RelPath(helpRoot, path))
                .Where(rel => !_IndexReachabilityOrphanAllowlist.Contains(rel))
                .OrderBy(rel => rel, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.True(
                orphans.Count == 0,
                "Help HTML not reachable from index.html:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, orphans)
            );
        }

        /// <summary>
        /// Verifies <c>fields.html</c> Write/Preview markers match
        /// <see cref="RenameListFieldCatalog"/> for documented groups, and that every other
        /// catalog group is explicitly allowlisted as lacking a Write/Preview table.
        /// </summary>
        [Fact]
        public void Fields_Html_Write_Preview_Markers_Match_Documented_Catalog_Groups()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var fieldsPath = Path.Combine(helpRoot, "reference", "fields.html");
            Assert.True(File.Exists(fieldsPath), "Expected help/reference/fields.html");

            var html = File.ReadAllText(fieldsPath);
            var sectionIdToRows = _ParseWritePreviewSections(html);
            var mismatches = new List<string>();

            var catalogGroupIds = RenameListFieldCatalog
                .All.Select(field => field.GroupId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            var documentedGroupIds = _FieldsWritePreviewSectionToGroup.Values.ToHashSet(StringComparer.Ordinal);

            foreach (var groupId in catalogGroupIds)
            {
                var isDocumented = documentedGroupIds.Contains(groupId);
                var isAllowlisted = _GroupsWithoutWritePreviewTable.Contains(groupId);
                if (isDocumented && isAllowlisted)
                {
                    mismatches.Add($"group '{groupId}' is both Write/Preview-documented and allowlisted");
                    continue;
                }

                if (!isDocumented && !isAllowlisted)
                {
                    mismatches.Add(
                        $"catalog group '{groupId}' has no fields.html Write/Preview mapping and is not allowlisted"
                    );
                }
            }

            foreach (var sectionId in sectionIdToRows.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
            {
                if (!_FieldsWritePreviewSectionToGroup.ContainsKey(sectionId))
                {
                    mismatches.Add($"fields.html#{sectionId} has Write/Preview rows but is not in the documented map");
                }
            }

            foreach (var (sectionId, groupId) in _FieldsWritePreviewSectionToGroup)
            {
                if (!sectionIdToRows.ContainsKey(sectionId))
                {
                    mismatches.Add($"Expected fields.html#{sectionId} Write/Preview table for '{groupId}'");
                    continue;
                }

                _AssertGroupMatchesHelp(mismatches, groupId, sectionId, sectionIdToRows);
            }

            Assert.True(
                mismatches.Count == 0,
                "fields.html Write/Preview drift vs RenameListFieldCatalog:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, mismatches)
            );
        }

        private static Dictionary<string, List<(string Href, string Text)>> _ParseFilterHubSections(string html)
        {
            var sectionIdToLinks = new Dictionary<string, List<(string Href, string Text)>>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (Match section in _FieldsSectionRegex().Matches(html))
            {
                var sectionId = section.Groups["id"].Value;
                var links = new List<(string Href, string Text)>();
                foreach (Match anchor in _AnchorRegex().Matches(section.Groups["body"].Value))
                {
                    var href = anchor.Groups["href"].Value.Trim();
                    if (href.Length == 0 || _IsAllowedAbsoluteUrl(href) || href.StartsWith('#'))
                    {
                        continue;
                    }

                    var hashIndex = href.IndexOf('#');
                    if (hashIndex >= 0)
                    {
                        href = href[..hashIndex];
                    }

                    if (href.Length == 0 || !href.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = _StripHtml(anchor.Groups["text"].Value).Trim();
                    links.Add((href.Replace('\\', '/'), text));
                }

                sectionIdToLinks[sectionId] = links;
            }

            return sectionIdToLinks;
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

        private static string _MarkerLabel(bool supported)
        {
            return supported ? "+" : "empty";
        }

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

        private static bool _CellHasPlus(string cellHtml)
        {
            return _StripHtml(cellHtml).Contains('+', StringComparison.Ordinal);
        }

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

            return _DecodeBasicHtmlEntities(result);
        }

        private static string _DecodeBasicHtmlEntities(string value)
        {
            return value
                .Replace("&amp;", "&", StringComparison.Ordinal)
                .Replace("&lt;", "<", StringComparison.Ordinal)
                .Replace("&gt;", ">", StringComparison.Ordinal)
                .Replace("&quot;", "\"", StringComparison.Ordinal)
                .Replace("&#39;", "'", StringComparison.Ordinal)
                .Replace("&#x27;", "'", StringComparison.OrdinalIgnoreCase)
                .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase)
                .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
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
