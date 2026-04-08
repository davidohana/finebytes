using Mfr.Core;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Cli
{
    /// <summary>
    /// Provides the command-line entry point and output formatting for the Mfr application.
    /// </summary>
    public static class CliApp
    {
        /// <summary>
        /// Runs the CLI entry point for <c>Mfr</c>.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The process exit code.</returns>
        public static CliExitCode Run(string[] args)
        {
            CliOptions? options;
            try
            {
                options = CliArgParser.ParseArgs(args);
            }
            catch (UserException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return CliExitCode.UserError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return CliExitCode.AppError;
            }

            // help or version requested
            if (options is null)
            {
                return CliExitCode.Success;
            }

            using var loggerSession = CliLogging.Start(options.LogLevel, options.LogDirectoryPath);
            var logger = loggerSession.Logger;
            try
            {
                return _Execute(options);
            }
            catch (UserException ex)
            {
                logger.Error("{Text}", ex.Message);
                return CliExitCode.UserError;
            }
            catch (Exception ex)
            {
                logger.Error("{Text}", ex.ToString());
                return CliExitCode.AppError;
            }
        }

        private static CliExitCode _Execute(CliOptions options)
        {
            if (options.PresetsFilePath.IsBlank())
            {
                throw new UserException("Presets file path is required.");
            }

            var presetManager = new PresetManager(options.PresetsFilePath);
            if (options.PresetName.IsBlank())
            {
                throw new UserException("Preset name is required.");
            }
            presetManager.LoadPresets();

            var preset = presetManager.NameToPreset.TryGetValue(options.PresetName, out var loadedPreset)
                ? loadedPreset
                : throw new UserException($"Preset not found: '{options.PresetName}'.");

            var renameList = new RenameList(includeHidden: options.IncludeHidden);
            renameList.AddSources(options.Sources);
            var renameItems = renameList.RenameItems;

            if (renameItems.Count == 0)
            {
                throw new UserException("No files matched the provided sources.");
            }

            renameList.Preview(preset: preset);
            var hasPreviewErrors = renameItems.Any(item => item.Status == RenameStatus.PreviewError);
            if (hasPreviewErrors)
            {
                return CliExitCode.UserError;
            }

            var commitFailFast = !options.ContinueOnRenameError;
            var renameResults = renameList.Commit(failFast: commitFailFast);

            if (!options.OutputFilePath.IsBlank())
            {
                RenameResultSummary.WriteJsonFile(
                    outputFilePath: options.OutputFilePath,
                    presetName: preset.Name,
                    results: renameResults);
            }

            var hasCommitErrors = renameResults.Any(item => item.Status is RenameStatus.PreviewError or RenameStatus.CommitError);
            return hasCommitErrors ? CliExitCode.UserError : CliExitCode.Success;
        }

    }

}
