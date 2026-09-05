using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.Filters.Attributes;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Unit tests for <see cref="AttributesSetterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class AttributesSetterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Attributes Setter On/Off/Keep radio edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Attributes_setter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Attributes Setter", new AttributesSetterFilter());
            var editor = new AttributesSetterFilterEditorViewModel(step);

            Assert.Equal(AttributeTriState.Keep, editor.ReadOnly);
            Assert.Equal(AttributeTriState.Keep, editor.Hidden);
            Assert.Equal(AttributeTriState.Keep, editor.Archive);
            Assert.Equal(AttributeTriState.Keep, editor.System);

            var defaults = ((AttributesSetterFilter)step.Filter).Options;
            Assert.Equal(AttributeTriState.Keep, defaults.ReadOnly);
            Assert.Equal(AttributeTriState.Keep, defaults.Hidden);
            Assert.Equal(AttributeTriState.Keep, defaults.Archive);
            Assert.Equal(AttributeTriState.Keep, defaults.System);

            editor.Hidden = AttributeTriState.Set;
            editor.Archive = AttributeTriState.Clear;
            editor.ReadOnly = AttributeTriState.Set;
            editor.System = AttributeTriState.Keep;

            var options = ((AttributesSetterFilter)step.Filter).Options;
            Assert.Equal(AttributeTriState.Set, options.ReadOnly);
            Assert.Equal(AttributeTriState.Set, options.Hidden);
            Assert.Equal(AttributeTriState.Clear, options.Archive);
            Assert.Equal(AttributeTriState.Keep, options.System);
        }
    }
}
