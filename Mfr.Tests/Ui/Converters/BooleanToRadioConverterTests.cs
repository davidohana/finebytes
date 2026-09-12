using System.Globalization;
using Avalonia.Data;
using Mfr.App.Ui.Converters;

namespace Mfr.Tests.Ui.Converters
{
    /// <summary>
    /// Unit tests for <see cref="BooleanToRadioConverter"/>.
    /// </summary>
    public sealed class BooleanToRadioConverterTests
    {
        /// <summary>
        /// Verifies convert is true only when the source matches the parameter.
        /// </summary>
        [Fact]
        public void Convert_is_true_only_for_matching_bool()
        {
            var converter = BooleanToRadioConverter.Instance;

            Assert.Equal(
                true,
                converter.Convert(true, typeof(bool), BooleanToRadioConverter.True, CultureInfo.InvariantCulture)
            );
            Assert.Equal(
                false,
                converter.Convert(false, typeof(bool), BooleanToRadioConverter.True, CultureInfo.InvariantCulture)
            );
            Assert.Equal(
                true,
                converter.Convert(false, typeof(bool), BooleanToRadioConverter.False, CultureInfo.InvariantCulture)
            );
        }

        /// <summary>
        /// Verifies convert-back writes the parameter when checked and ignores uncheck.
        /// </summary>
        [Fact]
        public void ConvertBack_writes_parameter_only_when_checked()
        {
            var converter = BooleanToRadioConverter.Instance;

            Assert.Equal(
                true,
                converter.ConvertBack(true, typeof(bool), BooleanToRadioConverter.True, CultureInfo.InvariantCulture)
            );
            Assert.Equal(
                false,
                converter.ConvertBack(true, typeof(bool), BooleanToRadioConverter.False, CultureInfo.InvariantCulture)
            );
            Assert.Same(
                BindingOperations.DoNothing,
                converter.ConvertBack(false, typeof(bool), BooleanToRadioConverter.True, CultureInfo.InvariantCulture)
            );
        }
    }
}
