using System.Text.RegularExpressions;

namespace Mfr.Tests.Help
{
    /// <summary>
    /// Ensures every <c>href</c> under repo-root <c>help/*.html</c> targets an existing local file
    /// (or a fragment on one), or an absolute <c>http</c>/<c>https</c>/<c>mailto</c> URL.
    /// </summary>
    public sealed partial class HelpLinkIntegrityTests
    {
        [GeneratedRegex(
            """href\s*=\s*(?:["'](?<url>[^"']+)["']|(?<url>[^\s>]+))""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex _HrefRegex();

        /// <summary>
        /// Verifies relative Help <c>href</c> values resolve to files under <c>help/</c>.
        /// </summary>
        [Fact]
        public void Every_Html_Href_Resolves_To_Existing_File_Or_Allowed_Absolute_Url()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            var htmlFiles = Directory
                .EnumerateFiles(helpRoot, "*.html", SearchOption.TopDirectoryOnly)
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
                        broken.Add($"{Path.GetFileName(htmlPath)} → {href}");
                        continue;
                    }

                    if (!File.Exists(targetPath))
                    {
                        broken.Add($"{Path.GetFileName(htmlPath)} → {href} (missing '{targetPath}')");
                    }
                }
            }

            Assert.True(
                broken.Count == 0,
                "Broken Help href(s):" + Environment.NewLine + string.Join(Environment.NewLine, broken)
            );
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

        private static string _ResolveRepoHelpRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "help");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "filters.html")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate repo-root help/ (expected filters.html) by walking up from BaseDirectory."
            );
        }
    }
}
