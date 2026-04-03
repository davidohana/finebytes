using System.Text.Json;
using Mfr8.Core;

namespace mfr8.Tests;

public class Phase1UnitTests
{
    [Fact]
    public void FilterParser_Parses_LettersCase()
    {
        var json = """
        {
          "type": "LettersCase",
          "enabled": true,
          "target": { "family": "FileName", "fileNameMode": "Full" },
          "options": { "mode": "UpperCase", "skipWords": ["a", "the"] }
        }
        """;

        var el = JsonDocument.Parse(json).RootElement;
        var filter = FilterParser.ParseFilter(el);

        Assert.IsType<LettersCaseFilter>(filter);
        var typed = (LettersCaseFilter)filter;
        Assert.Equal(LettersCaseMode.UpperCase, typed.Options.Mode);
        Assert.Contains("the", typed.Options.SkipWords);
    }

    [Fact]
    public void FilterParser_Rejects_NonFileNameFamily()
    {
        var json = """
        {
          "type": "LettersCase",
          "enabled": true,
          "target": { "family": "AudioTag", "field": "Title" },
          "options": { "mode": "UpperCase" }
        }
        """;

        var el = JsonDocument.Parse(json).RootElement;
        var ex = Assert.Throws<NotSupportedException>(() => FilterParser.ParseFilter(el));
        Assert.Contains("Phase 1 only supports target.family='FileName'", ex.Message);
    }

    [Fact]
    public void FilterEngine_ConflictSkipped_ForDuplicateDestinations()
    {
        var dir = CreateTempDir();

        try
        {
            var a = Path.Combine(dir, "a.mp3");
            var b = Path.Combine(dir, "b.mp3");
            File.WriteAllText(a, "x");
            File.WriteAllText(b, "y");

            var files = new List<FileEntryLite>
            {
                new FileEntryLite(GlobalIndex: 0, FolderOccurrenceIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "a", Extension: ".mp3"),
                new FileEntryLite(GlobalIndex: 1, FolderOccurrenceIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "b", Extension: ".mp3")
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "duplicate",
                Description = null,
                Filters = new List<Filter>
                {
                    new FormatterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Full),
                        Options: new FormatterOptions("same.mp3"))
                }
            };

            var result = FilterEngine.PreviewAndCommit(preset, files, continueOnErrors: false);

            Assert.Equal(2, result.Conflicts);
            Assert.Equal(RenameStatus.ConflictSkipped, result.Results[0].Status);
            Assert.Equal(RenameStatus.ConflictSkipped, result.Results[1].Status);
            Assert.True(File.Exists(a), "source file 'a' should remain on conflict skip");
            Assert.True(File.Exists(b), "source file 'b' should remain on conflict skip");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FilterEngine_Renames_WithCounter()
    {
        var dir = CreateTempDir();

        try
        {
            var a = Path.Combine(dir, "track01.mp3");
            var b = Path.Combine(dir, "track02.mp3");
            File.WriteAllText(a, "x");
            File.WriteAllText(b, "y");

            var files = new List<FileEntryLite>
            {
                new FileEntryLite(GlobalIndex: 0, FolderOccurrenceIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "track01", Extension: ".mp3"),
                new FileEntryLite(GlobalIndex: 1, FolderOccurrenceIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "track02", Extension: ".mp3"),
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters = new List<Filter>
                {
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                }
            };

            var result = FilterEngine.PreviewAndCommit(preset, files, continueOnErrors: false);

            Assert.Equal(2, result.Renamed);
            Assert.Equal(RenameStatus.Ok, result.Results[0].Status);
            Assert.Equal(RenameStatus.Ok, result.Results[1].Status);

            Assert.False(File.Exists(a));
            Assert.False(File.Exists(b));
            Assert.True(File.Exists(Path.Combine(dir, "001.mp3")));
            Assert.True(File.Exists(Path.Combine(dir, "002.mp3")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mfr8_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
