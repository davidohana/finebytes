using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Media;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;exif&gt;</c> escape-hatch format token.
    /// </summary>
    internal sealed partial class ExifFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        private const string DefaultSource = "ExifSub";
        private const string DefaultName = "36867";

        /// <summary>
        /// Initializes the editor from existing <c>source,name</c> arguments.
        /// </summary>
        /// <param name="args">Positional source alias and tag name/id.</param>
        public ExifFormatTokenEditorViewModel(string? args)
            : base("Metadata Field", "exif")
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return;
            }

            var trimmed = args.Trim();
            var firstComma = trimmed.IndexOf(',');
            if (firstComma < 0)
            {
                Source = _ResolveSource(trimmed) ?? DefaultSource;
                return;
            }

            var sourcePart = trimmed[..firstComma].Trim();
            var namePart = trimmed[(firstComma + 1)..].Trim();
            Source = _ResolveSource(sourcePart) ?? DefaultSource;
            if (namePart.Length > 0)
            {
                Name = namePart;
            }
        }

        /// <summary>
        /// Gets known EXIF directory aliases.
        /// </summary>
        public IReadOnlyList<string> SourceAliases => ExifData.SourceAliases;

        /// <summary>
        /// Gets or sets the directory source alias.
        /// </summary>
        [ObservableProperty]
        private string _source = DefaultSource;

        /// <summary>
        /// Gets or sets the tag name or decimal id.
        /// </summary>
        [ObservableProperty]
        private string _name = DefaultName;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var source = string.IsNullOrWhiteSpace(Source) ? DefaultSource : Source.Trim();
            var name = string.IsNullOrWhiteSpace(Name) ? DefaultName : Name.Trim();
            return CanonicalName + ":" + source + "," + name;
        }

        private static string? _ResolveSource(string raw)
        {
            foreach (var alias in ExifData.SourceAliases)
            {
                if (string.Equals(alias, raw, StringComparison.OrdinalIgnoreCase))
                {
                    return alias;
                }
            }

            return null;
        }
    }
}
