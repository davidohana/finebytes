using Mfr.Filters;
using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.Tags;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Verifies the typed sample-preset catalog and its curated filter chains.
    /// </summary>
    public sealed class SamplePresetCatalogTests
    {
        /// <summary>
        /// Verifies the catalog exposes the complete locked set of samples.
        /// </summary>
        [Fact]
        public void Catalog_contains_all_13_samples()
        {
            Assert.Equal(13, SamplePresetCatalog.Presets.Count);
            Assert.All(SamplePresetCatalog.Presets, preset => Assert.NotEmpty(preset.Chain.Steps));
        }

        /// <summary>
        /// Verifies every sample ships Rename List columns with known catalog field keys and positive widths.
        /// </summary>
        [Fact]
        public void Catalog_visible_columns_are_present_and_known()
        {
            Assert.All(
                SamplePresetCatalog.Presets,
                preset =>
                {
                    Assert.NotNull(preset.VisibleColumns);
                    Assert.NotEmpty(preset.VisibleColumns);
                    Assert.All(
                        preset.VisibleColumns,
                        column =>
                        {
                            Assert.True(RenameListFieldCatalog.TryGetField(column.Key, out _), column.Key.ToString());
                            Assert.True(column.Width is > 0, column.Key.ToString());
                        }
                    );
                }
            );
        }

        /// <summary>
        /// Verifies domain samples expose the fields their chains read or write.
        /// </summary>
        [Fact]
        public void Catalog_visible_columns_include_relevant_domain_fields()
        {
            Assert.Contains(
                _Preset("Tags from Filename").VisibleColumns!,
                column => column.Key == RenameListFieldKey.Preview(AudioTagRenameListFields.Group, "Title")
            );
            Assert.Contains(
                _Preset("Artist - Track - Title").VisibleColumns!,
                column => column.Key == RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Performers")
            );
            Assert.Contains(
                _Preset("Date Taken Prefix").VisibleColumns!,
                column =>
                    column.Key == RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*36867")
                    && column.Width == 140
            );
            Assert.Contains(
                _Preset("Name from Image").VisibleColumns!,
                column => column.Key == RenameListFieldKey.Original(ImageRenameListFields.Group, "Width")
            );
            Assert.Contains(
                _Preset("Flatten Path").VisibleColumns!,
                column =>
                    column.Key
                    == RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath)
            );
            Assert.Contains(
                _Preset("Date Taken Folders").VisibleColumns!,
                column =>
                    column.Key
                    == RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)
            );
        }

        /// <summary>
        /// Verifies sample names and identifiers are unique.
        /// </summary>
        [Fact]
        public void Catalog_names_and_ids_are_unique()
        {
            Assert.Equal(
                SamplePresetCatalog.Presets.Count,
                SamplePresetCatalog.Presets.Select(preset => preset.Name).Distinct(StringComparer.Ordinal).Count()
            );
            Assert.Equal(
                SamplePresetCatalog.Presets.Count,
                SamplePresetCatalog.Presets.Select(preset => preset.Id).Distinct().Count()
            );
        }

        /// <summary>
        /// Verifies every sample filter type is currently shipped in the filter catalog.
        /// </summary>
        [Fact]
        public void Catalog_filter_types_are_known()
        {
            var knownTypes = FilterCatalog.Entries.Select(entry => entry.Type).ToHashSet(StringComparer.Ordinal);
            var sampleFilters = SamplePresetCatalog.Presets.SelectMany(preset =>
                preset.Chain.Steps.Select(step => step.Filter)
            );

            Assert.All(sampleFilters, filter => Assert.Contains(filter.Type, knownTypes));
        }

        /// <summary>
        /// Verifies every enabled portable sample filter accepts its shipped options and formatter templates.
        /// </summary>
        [Fact]
        public void Catalog_enabled_portable_filters_complete_setup()
        {
            var filters = SamplePresetCatalog
                .Presets.SelectMany(preset => preset.Chain.Steps)
                .Where(step => step.Enabled)
                .Select(step => step.Filter)
                .Where(filter => filter is not PathMoverFilter);

            Assert.All(filters, filter => filter.Setup());
        }

        /// <summary>
        /// Verifies every enabled Path Mover accepts its shipped Windows destination and formatter template.
        /// </summary>
        [WindowsFact]
        public void Catalog_enabled_path_movers_complete_setup()
        {
            var pathMovers = SamplePresetCatalog
                .Presets.SelectMany(preset => preset.Chain.Steps)
                .Where(step => step.Enabled)
                .Select(step => step.Filter)
                .OfType<PathMoverFilter>();

            Assert.All(pathMovers, pathMover => pathMover.Setup());
        }

        /// <summary>
        /// Verifies the Beautify Names sample keeps its ordered cleanup and casing workflow.
        /// </summary>
        [Fact]
        public void Beautify_Names_has_locked_chain_and_common_words()
        {
            var preset = _Preset("Beautify Names");
            string[] expectedTypes =
            [
                "SpaceCharacter",
                "SpaceAround",
                "SpaceAfter",
                "ShrinkSpaces",
                "StripSpacesRight",
                "StripSpacesLeft",
                "LettersCase",
                "CapitalizeAfter",
                "UppercaseInitials",
                "CasingList",
            ];

            Assert.Equal(expectedTypes, preset.Chain.Steps.Select(step => step.Filter.Type));
            var casingList = Assert.IsType<CasingListFilter>(preset.Chain.Steps[^1].Filter);
            string[] expectedWords = ["a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with"];
            Assert.Equal(expectedWords, casingList.Options.Words);
            Assert.True(casingList.Options.UppercaseSentenceInitial);
        }

        /// <summary>
        /// Verifies the Date Taken Folders sample uses the editable photo root and date hierarchy.
        /// </summary>
        [Fact]
        public void Date_Taken_Folders_has_locked_path_mover()
        {
            var step = Assert.Single(_Preset("Date Taken Folders").Chain.Steps);
            Assert.True(step.Enabled);
            var pathMover = Assert.IsType<PathMoverFilter>(step.Filter);
            Assert.Equal(@"C:\Photos", pathMover.Options.RootFolder);
            Assert.Equal(@"<exif-date:yyyy>\<exif-date:MM>\<exif-date:dd>", pathMover.Options.SubFolder);
        }

        /// <summary>
        /// Verifies the Swap Around Hyphen sample moves the second spaced-hyphen token left.
        /// </summary>
        [Fact]
        public void Swap_Around_Hyphen_has_locked_token_move()
        {
            var step = Assert.Single(_Preset("Swap Around Hyphen").Chain.Steps);
            var tokenMover = Assert.IsType<TokenMoverFilter>(step.Filter);
            Assert.Equal(" - ", tokenMover.Options.Delimiter);
            Assert.Equal(2, tokenMover.Options.TokenNumber);
            Assert.Equal(-1, tokenMover.Options.MoveBy);
        }

        /// <summary>
        /// Verifies the Safe Filename sample replaces every illegal Windows name character.
        /// </summary>
        [Fact]
        public void Safe_Filename_has_locked_regex_replacer()
        {
            var step = Assert.Single(_Preset("Safe Filename").Chain.Steps);
            var replacer = Assert.IsType<ReplacerFilter>(step.Filter);
            Assert.Equal(@"[\\/:*?""<>|]", replacer.Options.Find);
            Assert.Equal("-", replacer.Options.Replacement);
            Assert.Equal(ReplacerMode.Regex, replacer.Options.Match.Mode);
            Assert.True(replacer.Options.Match.ReplaceAll);
            replacer.Setup();
        }

        /// <summary>
        /// Verifies the Tags from Filename sample wipes ID3 blocks, maps named tokens, and writes track count.
        /// </summary>
        [Fact]
        public void Tags_from_Filename_has_locked_audio_chain()
        {
            var preset = _Preset("Tags from Filename");
            Assert.Equal(
                ["TagRemover", "AudioTagSetter", "Id3v2FieldSetter"],
                preset.Chain.Steps.Select(s => s.Filter.Type)
            );

            var remover = Assert.IsType<TagRemoverFilter>(preset.Chain.Steps[0].Filter);
            Assert.Equal([AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2], remover.Options.Blocks);

            var setter = Assert.IsType<AudioTagSetterFilter>(preset.Chain.Steps[1].Filter);
            Assert.Contains("tokenNumber=2", setter.Options.Title!.Text, StringComparison.Ordinal);
            Assert.Contains("source=<file-name>", setter.Options.Title.Text, StringComparison.Ordinal);
            Assert.Contains("source=<parent-folder:1>", setter.Options.Performers!.Text, StringComparison.Ordinal);
            setter.Setup();

            var trackCount = Assert.IsType<Id3v2FieldSetterFilter>(preset.Chain.Steps[2].Filter);
            Assert.Equal("TRCK", trackCount.Options.FrameId);
            Assert.Equal("<audio-track>/<item-count>", trackCount.Options.Text);
            trackCount.Setup();
        }

        private static FilterPreset _Preset(string name)
        {
            return Assert.Single(SamplePresetCatalog.Presets, preset => preset.Name == name);
        }
    }
}
