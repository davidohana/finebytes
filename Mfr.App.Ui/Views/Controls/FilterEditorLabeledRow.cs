using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// One filter-editor form row: shared-width label column, gap, then content.
    /// <para>
    /// Place rows under a parent with <see cref="Grid.IsSharedSizeScopeProperty"/> so labels
    /// align via the <c>FilterEditorLabel</c> shared size group.
    /// </para>
    /// </summary>
    public sealed class FilterEditorLabeledRow : ContentControl
    {
        /// <summary>
        /// Shared size group name used by every labeled row's label column.
        /// </summary>
        public const string LabelSharedSizeGroup = "FilterEditorLabel";

        /// <summary>
        /// Defines the <see cref="Label"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<
            FilterEditorLabeledRow,
            string?
        >(nameof(Label));

        /// <summary>
        /// Gets or sets the row label text shown in the shared-width column.
        /// </summary>
        public string? Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FilterEditorLabeledRow);
    }
}
