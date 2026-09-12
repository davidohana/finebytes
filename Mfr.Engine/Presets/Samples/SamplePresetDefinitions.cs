using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.Tags;

namespace Mfr.Engine.Presets.Samples
{
    /// <summary>
    /// Typed builders for the curated sample presets shipped with the application.
    /// </summary>
    internal static class SamplePresetDefinitions
    {
        private static readonly string[] CommonSmallWords =
        [
            "a",
            "an",
            "the",
            "and",
            "or",
            "of",
            "to",
            "in",
            "on",
            "for",
            "with",
        ];

        private static readonly FilePrefixTarget FilePrefix = new();

        /// <summary>
        /// Builds the locked 13-sample catalog (unsorted).
        /// </summary>
        /// <returns>All sample presets.</returns>
        public static IReadOnlyList<FilterPreset> CreateAll()
        {
            return
            [
                _BeautifyNames(),
                _CounterPrefix(),
                _TagsFromFilename(),
                _ArtistTrackTitle(),
                _DateTakenPrefix(),
                _NameFromImage(),
                _FlattenPath(),
                _DateTakenFolders(),
                _ArtistAlbumFolders(),
                _SwapAroundHyphen(),
                _SafeFilename(),
                _StripBracketJunk(),
                _YearTitleFromTags(),
            ];
        }

        private static FilterPreset _BeautifyNames()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000001",
                name: "Beautify Names",
                description: "Beautify file names by fixing casing and spaces",
                chain: _Chain(
                    _On(
                        new SpaceCharacterFilter(
                            FilePrefix,
                            new SpaceCharacterOptions(
                                ' ',
                                [
                                    SpaceCharacterOptions.Percent20Replacement,
                                    SpaceCharacterOptions.UnderscoreReplacement,
                                ]
                            )
                        )
                    ),
                    _On(
                        new SpaceAroundFilter(
                            FilePrefix,
                            new SpaceAroundOptions(AroundChars: "-", OnlyWhenNeighboringAreLettersOrDigits: true)
                        )
                    ),
                    _On(
                        new SpaceAfterFilter(
                            FilePrefix,
                            new SpaceAfterOptions(AfterChars: ",;!", OnlyWhenNextIsLetterOrDigit: true)
                        )
                    ),
                    _On(new ShrinkSpacesFilter(FilePrefix)),
                    _On(new StripSpacesRightFilter(FilePrefix)),
                    _On(new StripSpacesLeftFilter(FilePrefix)),
                    _On(
                        new LettersCaseFilter(
                            FilePrefix,
                            new LettersCaseOptions(LettersCaseMode.Capitalize, CommonSmallWords)
                        )
                    ),
                    _On(
                        new CapitalizeAfterFilter(
                            FilePrefix,
                            new CapitalizeAfterOptions(CapitalizeAfterChars: ",!()[]{};-")
                        )
                    ),
                    _On(new UppercaseInitialsFilter(FilePrefix)),
                    _On(
                        new CasingListFilter(
                            FilePrefix,
                            new CasingListOptions(Words: CommonSmallWords, UppercaseSentenceInitial: true)
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 320),
                ]
            );
        }

        private static FilterPreset _CounterPrefix()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000002",
                name: "Counter Prefix",
                description: "Adds a number before each item in Rename List.",
                chain: _Chain(
                    _On(
                        new CounterFilter(
                            FilePrefix,
                            new CounterOptions(
                                Start: 1,
                                Step: 1,
                                LeadingZerosMode: CounterLeadingZerosMode.Custom,
                                CustomLength: 2,
                                Position: CounterPosition.Prepend,
                                Separator: " - ",
                                ResetPerFolder: false
                            )
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 360),
                ]
            );
        }

        private static FilterPreset _TagsFromFilename()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000003",
                name: "Tags from Filename",
                description: "Set ID3 tags for MP3 files according to filenames.\nFilename format should be: Track - Title.mp3\nParent folder name format should be: Artist - Album",
                chain: _Chain(
                    _On(
                        new TagRemoverFilter(
                            new TagRemoverOptions(Blocks: [AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2])
                        )
                    ),
                    _On(
                        new AudioTagSetterFilter(
                            new AudioTagSetterOptions(
                                Performers: new AudioTagStringFieldOptions(
                                    Text: "<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<parent-folder:1>>"
                                ),
                                Album: new AudioTagStringFieldOptions(
                                    Text: "<token:tokenNumber=2,separator=-,includeNext=true,includePrev=false,source=<parent-folder:1>>"
                                ),
                                Title: new AudioTagStringFieldOptions(
                                    Text: "<token:tokenNumber=2,separator=-,includeNext=true,includePrev=false,source=<file-name>>"
                                ),
                                Track: new AudioTagStringFieldOptions(
                                    Text: "<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<file-name>>"
                                ),
                                TrackAutoIncrement: false
                            )
                        )
                    ),
                    _On(
                        new Id3v2FieldSetterFilter(
                            new Id3v2FieldSetterOptions(
                                FrameId: "TRCK",
                                Text: "<audio-track>/<item-count>",
                                OnlyIfEmpty: false
                            )
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 280),
                    _MediaTag("Performers", preview: true, width: 120),
                    _MediaTag("Album", preview: true, width: 140),
                    _MediaTag("Title", preview: true, width: 160),
                    _MediaTag("Track", preview: true, width: 45),
                    _MediaTag("TrackCount", preview: true, width: 50),
                ]
            );
        }

        private static FilterPreset _ArtistTrackTitle()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000004",
                name: "Artist - Track - Title",
                description: "Set track titles for MP3 files from ID3 tag.\nFormat: Artist - Track Number - Track Title.mp3",
                chain: _Chain(
                    _On(new FormatterFilter(FilePrefix, new FormatterOptions("<audio-track>")), "Track Number"),
                    _On(
                        new FixLeadingZerosFilter(
                            FilePrefix,
                            new FixLeadingZerosOptions(
                                Width: 2,
                                RemoveExtraZeros: true,
                                MaxCount: 1,
                                WholeWordOnly: true
                            )
                        ),
                        "Pad Track Number"
                    ),
                    _On(
                        new InserterFilter(
                            FilePrefix,
                            new InserterOptions(
                                Text: " - <audio-title>",
                                Position: 1,
                                StartFrom: InserterOrigin.End,
                                Overwrite: false
                            )
                        ),
                        "Append Title"
                    ),
                    _On(
                        new InserterFilter(
                            FilePrefix,
                            new InserterOptions(
                                Text: "<audio-artist> - ",
                                Position: 1,
                                StartFrom: InserterOrigin.Beginning,
                                Overwrite: false
                            )
                        ),
                        "Prepend Artist"
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 160),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 340),
                    _MediaTag("Performers", preview: false, width: 120),
                    _MediaTag("Track", preview: false, width: 45),
                    _MediaTag("Title", preview: false, width: 160),
                ]
            );
        }

        private static FilterPreset _DateTakenPrefix()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000005",
                name: "Date Taken Prefix",
                description: "Rename JPEG digital images according to date picture taken.",
                chain: _Chain(
                    _On(
                        new FormatterFilter(
                            FilePrefix,
                            new FormatterOptions("<exif-date:yyyy-MM-dd HH-mm-ss> - <file-name>")
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 160),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 300),
                    _Jpeg("ExifDirectory*36867", preview: false, width: 140),
                ]
            );
        }

        private static FilterPreset _NameFromImage()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000006",
                name: "Name from Image",
                description: "Set filename according to basic image properties",
                chain: _Chain(
                    _On(
                        new FormatterFilter(
                            FilePrefix,
                            new FormatterOptions(
                                "<counter:initial=1,step=1,padding=fixed,length=2,resetScope=global>.<image-width>x<image-height>.<image-format>.<image-bit-depth>bpp.<image-horz-res>dpi"
                            )
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 160),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 300),
                    _Image("Format", preview: false, width: 60),
                    _Image("Width", preview: false, width: 55),
                    _Image("Height", preview: false, width: 55),
                    _Image("BitDepth", preview: false, width: 60),
                    _Image("HorzRes", preview: false, width: 70),
                ]
            );
        }

        private static FilterPreset _FlattenPath()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000007",
                name: "Flatten Path",
                description: "Use path name as file name.\nPath hierarchies (\\) are separated by periods (.)\nE.g: c:\\pictures\\David\\sep04\\pic1.jpg -->\npictures.David.sep04.pic1.jpg",
                chain: _Chain(
                    _On(
                        new FormatterFilter(
                            FilePrefix,
                            new FormatterOptions("<parent-folder:3>.<parent-folder:2>.<parent-folder:1>.<file-name>")
                        )
                    ),
                    _Off(new PathMoverFilter(new PathMoverOptions(RootFolder: @"C:\", SubFolder: "Renamed Files")))
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 320),
                    _Basic(BasicRenameListFields.Key.FullPath, preview: true, width: 300),
                ]
            );
        }

        private static FilterPreset _DateTakenFolders()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000008",
                name: "Date Taken Folders",
                description: "Move photos into folders by date taken (year\\month\\day).\nEdit the Path Mover root (default C:\\Photos) before Apply.",
                chain: _Chain(
                    _On(
                        new PathMoverFilter(
                            new PathMoverOptions(
                                RootFolder: @"C:\Photos",
                                SubFolder: @"<exif-date:yyyy>\<exif-date:MM>\<exif-date:dd>"
                            )
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.Folder, preview: true, width: 240),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 180),
                    _Jpeg("ExifDirectory*36867", preview: false, width: 140),
                ]
            );
        }

        private static FilterPreset _ArtistAlbumFolders()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000009",
                name: "Artist Album Folders",
                description: "Move audio files into Artist\\Album folders from tags.\nEdit the Path Mover root (default C:\\Music) before Apply.",
                chain: _Chain(
                    _On(
                        new PathMoverFilter(
                            new PathMoverOptions(RootFolder: @"C:\Music", SubFolder: @"<audio-artist>\<audio-album>")
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.Folder, preview: true, width: 260),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 200),
                    _MediaTag("Performers", preview: false, width: 120),
                    _MediaTag("Album", preview: false, width: 140),
                ]
            );
        }

        private static FilterPreset _SwapAroundHyphen()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000010",
                name: "Swap Around Hyphen",
                description: "Swap the two parts of a name separated by \" - \".\nE.g: Title - Artist --> Artist - Title",
                chain: _Chain(
                    _On(
                        new TokenMoverFilter(
                            FilePrefix,
                            new TokenMoverOptions(Delimiter: " - ", TokenNumber: 2, MoveBy: -1)
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 320),
                ]
            );
        }

        private static FilterPreset _SafeFilename()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000011",
                name: "Safe Filename",
                description: "Replace Windows-illegal filename characters \\ / : * ? \" < > | with a hyphen.",
                chain: _Chain(
                    _On(
                        new ReplacerFilter(
                            FilePrefix,
                            new ReplacerOptions(
                                Find: @"[\\/:*?""<>|]",
                                Replacement: "-",
                                Match: new ReplacerMatchOptions(
                                    Mode: ReplacerMode.Regex,
                                    CaseSensitive: true,
                                    ReplaceAll: true,
                                    WholeWord: false
                                )
                            )
                        )
                    )
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 320),
                ]
            );
        }

        private static FilterPreset _StripBracketJunk()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000012",
                name: "Strip Bracket Junk",
                description: "Remove (…) and […] segments from the name, then shrink leftover spaces.\nE.g: Song (Official Video) [HD] --> Song",
                chain: _Chain(
                    _On(
                        new StripParenthesesFilter(
                            FilePrefix,
                            new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true)
                        ),
                        "Strip Round Parentheses"
                    ),
                    _On(
                        new StripParenthesesFilter(
                            FilePrefix,
                            new StripParenthesesOptions(Type: ParenthesisType.Square, RemoveContents: true)
                        ),
                        "Strip Square Brackets"
                    ),
                    _On(new ShrinkSpacesFilter(FilePrefix)),
                    _On(new StripSpacesRightFilter(FilePrefix)),
                    _On(new StripSpacesLeftFilter(FilePrefix))
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 200),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 220),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 320),
                ]
            );
        }

        private static FilterPreset _YearTitleFromTags()
        {
            return _Preset(
                id: "10000000-0000-4000-8000-000000000013",
                name: "Year - Title from Tags",
                description: "Set the name from audio tags as Year - Title.",
                chain: _Chain(
                    _On(new FormatterFilter(FilePrefix, new FormatterOptions("<audio-year> - <audio-title>")))
                ),
                columns:
                [
                    _Basic(BasicRenameListFields.Key.ItemType, preview: false, width: 50),
                    _Basic(BasicRenameListFields.Key.Folder, preview: false, width: 160),
                    _Basic(BasicRenameListFields.Key.FullName, preview: false, width: 180),
                    _Basic(BasicRenameListFields.Key.FullName, preview: true, width: 280),
                    _MediaTag("Year", preview: false, width: 50),
                    _MediaTag("Title", preview: false, width: 180),
                ]
            );
        }

        private static FilterPreset _Preset(
            string id,
            string name,
            string description,
            FilterChain chain,
            IReadOnlyList<RenameListVisibleColumnSpec> columns
        )
        {
            return new FilterPreset
            {
                Id = Guid.Parse(id),
                Name = name,
                Description = description,
                Chain = chain,
                VisibleColumns = columns,
            };
        }

        private static FilterChain _Chain(params FilterChainStep[] steps)
        {
            return new FilterChain { Steps = steps };
        }

        private static FilterChainStep _On(BaseFilter filter, string? name = null)
        {
            return new FilterChainStep(Enabled: true, Filter: filter, Name: name);
        }

        private static FilterChainStep _Off(BaseFilter filter, string? name = null)
        {
            return new FilterChainStep(Enabled: false, Filter: filter, Name: name);
        }

        private static RenameListVisibleColumnSpec _Basic(string propertyKey, bool preview, int width)
        {
            return _Column(BasicRenameListField.Group, propertyKey, preview, width);
        }

        private static RenameListVisibleColumnSpec _MediaTag(string propertyKey, bool preview, int width)
        {
            return _Column(AudioTagRenameListFields.Group, propertyKey, preview, width);
        }

        private static RenameListVisibleColumnSpec _Image(string propertyKey, bool preview, int width)
        {
            return _Column(ImageRenameListFields.Group, propertyKey, preview, width);
        }

        private static RenameListVisibleColumnSpec _Jpeg(string propertyKey, bool preview, int width)
        {
            return _Column(JpegRenameListFields.Group, propertyKey, preview, width);
        }

        private static RenameListVisibleColumnSpec _Column(string groupId, string propertyKey, bool preview, int width)
        {
            var key = preview
                ? RenameListFieldKey.Preview(groupId, propertyKey)
                : RenameListFieldKey.Original(groupId, propertyKey);
            return new RenameListVisibleColumnSpec(key, width);
        }
    }
}
