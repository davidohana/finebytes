using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.Filters;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Guards <see cref="FilterOptionsEditorFactory"/> against silent missing editors.
    /// </summary>
    public sealed class FilterOptionsEditorFactoryTests
    {
        /// <summary>
        /// Palette filters that only expose Target / Apply Scope (no Filter Configuration options body).
        /// </summary>
        private static readonly HashSet<string> OptionlessTypes = new(StringComparer.Ordinal)
        {
            "ShrinkSpaces",
            "RemoveSpaces",
            "StripSpacesLeft",
            "StripSpacesRight",
            "SeparateCapitalizedText",
            "UppercaseInitials",
        };

        /// <summary>
        /// Verifies every option-bearing catalog filter gets a factory editor, and optionless types stay null.
        /// </summary>
        [Fact]
        public void Create_covers_every_option_bearing_catalog_filter()
        {
            var missingEditors = new List<string>();
            var unexpectedEditors = new List<string>();

            foreach (var entry in FilterCatalog.Entries)
            {
                var filter = FilterCatalog.CreateDefault(entry);
                var step = new AppliedFilterStepViewModel(entry.DisplayName, filter);
                var editor = FilterOptionsEditorFactory.Create(step);
                var expectsEditor = !OptionlessTypes.Contains(entry.Type);

                if (expectsEditor && editor is null)
                {
                    missingEditors.Add(entry.Type);
                }
                else if (!expectsEditor && editor is not null)
                {
                    unexpectedEditors.Add($"{entry.Type} → {editor.GetType().Name}");
                }
            }

            Assert.True(
                missingEditors.Count == 0,
                "Missing Filter Configuration editors for: " + string.Join(", ", missingEditors)
            );
            Assert.True(
                unexpectedEditors.Count == 0,
                "Unexpected editors for optionless filters: " + string.Join(", ", unexpectedEditors)
            );
        }

        /// <summary>
        /// Verifies the optionless allowlist stays a subset of the live catalog (no stale type names).
        /// </summary>
        [Fact]
        public void Optionless_allowlist_matches_catalog_types()
        {
            var catalogTypes = FilterCatalog.Entries.Select(e => e.Type).ToHashSet(StringComparer.Ordinal);
            var unknown = OptionlessTypes.Where(t => !catalogTypes.Contains(t)).OrderBy(t => t).ToList();
            Assert.Empty(unknown);
        }

        /// <summary>
        /// Verifies every factory-produced editor resolves a convention-matched view.
        /// </summary>
        [AvaloniaFact]
        public void ViewLocator_resolves_view_for_every_factory_editor()
        {
            var locator = new FilterEditorViewLocator();
            foreach (var entry in FilterCatalog.Entries.Where(e => !OptionlessTypes.Contains(e.Type)))
            {
                var filter = FilterCatalog.CreateDefault(entry);
                var step = new AppliedFilterStepViewModel(entry.DisplayName, filter);
                var editor = FilterOptionsEditorFactory.Create(step);
                Assert.NotNull(editor);

                Assert.True(locator.Match(editor));
                var view = locator.Build(editor);
                Assert.NotNull(view);
                Assert.Same(editor, view.DataContext);

                var expectedViewName = editor.GetType().Name[..^"ViewModel".Length] + "View";
                Assert.Equal(expectedViewName, view.GetType().Name);
            }
        }
    }
}
