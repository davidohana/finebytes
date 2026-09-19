using Mfr.Models;
using Mfr.Utils;

namespace Mfr.App.Ui
{
    /// <summary>
    /// Thin argv parser for desktop UI startup intents (no Spectre).
    /// </summary>
    internal static class UiStartupArgsParser
    {
        /// <summary>
        /// Parses command-line arguments into <see cref="UiStartupArgs"/>.
        /// </summary>
        /// <param name="args">Raw argv tokens (may be empty).</param>
        /// <returns>Parsed startup args; empty when argv has no product intents.</returns>
        /// <exception cref="UserException">Thrown for unknown flags, missing option values, or invalid yes|no.</exception>
        public static UiStartupArgs Parse(string[] args)
        {
            if (args.Length == 0)
            {
                return UiStartupArgs.Empty;
            }

            var sources = new List<string>();
            string? initialFolder = null;
            bool? includeFiles = null;
            bool? includeFolders = null;
            bool? includeSubdirs = null;
            bool? includeHidden = null;

            for (var i = 0; i < args.Length; i++)
            {
                var token = args[i];
                if (!_LooksLikeOption(token))
                {
                    var source = token.Trim();
                    if (!source.IsBlank())
                    {
                        sources.Add(source);
                    }

                    continue;
                }

                switch (token)
                {
                    case "--initial-folder":
                        initialFolder = _RequireOptionValue(optionName: "--initial-folder", args: args, index: ref i)
                            .Trim();
                        if (initialFolder.IsBlank())
                        {
                            throw new UserException("Missing value for --initial-folder.");
                        }

                        break;

                    case "--files":
                        includeFiles = _ParseYesNoOption(
                            optionName: "--files",
                            value: _RequireOptionValue(optionName: "--files", args: args, index: ref i)
                        );
                        break;

                    case "--folders":
                        includeFolders = _ParseYesNoOption(
                            optionName: "--folders",
                            value: _RequireOptionValue(optionName: "--folders", args: args, index: ref i)
                        );
                        break;

                    case "-r":
                    case "--recursive":
                        includeSubdirs = true;
                        break;

                    case "--include-hidden":
                        includeHidden = true;
                        break;

                    case "-l":
                    case "--log-level":
                        // Consumed here so Program can apply the level before LogSession.Start; value ignored.
                        _ = _RequireOptionValue(optionName: token, args: args, index: ref i);
                        break;

                    default:
                        throw new UserException($"Unknown option '{token}'.");
                }
            }

            var result = new UiStartupArgs(
                Sources: sources,
                InitialFolder: initialFolder,
                IncludeFiles: includeFiles,
                IncludeFolders: includeFolders,
                IncludeSubdirs: includeSubdirs,
                IncludeHidden: includeHidden
            );
            return result.HasAnyIntent ? result : UiStartupArgs.Empty;
        }

        /// <summary>
        /// Whether a token is treated as an option name (leading <c>-</c>), not a positional path.
        /// </summary>
        /// <param name="token">Raw argv token.</param>
        /// <returns><c>true</c> when the token starts with <c>-</c>.</returns>
        private static bool _LooksLikeOption(string token)
        {
            return token.StartsWith('-');
        }

        /// <summary>
        /// Reads the next argv token as a required option value and advances <paramref name="index"/>.
        /// </summary>
        /// <param name="optionName">Option name for error messages (e.g. <c>--files</c>).</param>
        /// <param name="args">Full argv array.</param>
        /// <param name="index">Current option index; updated to the value token on success.</param>
        /// <returns>The raw value token (not trimmed).</returns>
        /// <exception cref="UserException">Thrown when no value follows or the next token looks like an option.</exception>
        private static string _RequireOptionValue(string optionName, string[] args, ref int index)
        {
            var nextIndex = index + 1;
            if (nextIndex >= args.Length)
            {
                throw new UserException($"Missing value for {optionName}.");
            }

            var value = args[nextIndex];
            if (_LooksLikeOption(value))
            {
                throw new UserException($"Missing value for {optionName}.");
            }

            index = nextIndex;
            return value;
        }

        /// <summary>
        /// Parses a case-insensitive <c>yes</c>/<c>no</c> option value (same rules as console CLI).
        /// </summary>
        /// <param name="optionName">Option name for error messages.</param>
        /// <param name="value">Raw option value.</param>
        /// <returns><c>true</c> for yes; <c>false</c> for no.</returns>
        /// <exception cref="UserException">Thrown when the value is not yes or no.</exception>
        private static bool _ParseYesNoOption(string optionName, string value)
        {
            var normalizedValue = value.Trim().ToLowerInvariant();
            return normalizedValue switch
            {
                "yes" => true,
                "no" => false,
                _ => throw new UserException($"Invalid value for {optionName}: '{value}'. Expected yes or no."),
            };
        }
    }
}
