using System.Globalization;
using Avalonia.Data;
using Mfr.App.Ui.Converters;
using Mfr.Filters.Misc;

namespace Mfr.Tests.Ui.Converters
{
    /// <summary>
    /// Unit tests for <see cref="EnumToBooleanConverter"/>.
    /// </summary>
    public sealed class EnumToBooleanConverterTests
    {
        /// <summary>
        /// Verifies convert is true only for the matching enum member.
        /// </summary>
        [Fact]
        public void Convert_is_true_only_for_matching_member()
        {
            var converter = EnumToBooleanConverter.Instance;

            Assert.Equal(
                true,
                converter.Convert(
                    ParenthesisType.Square,
                    typeof(bool),
                    ParenthesisType.Square,
                    CultureInfo.InvariantCulture
                )
            );
            Assert.Equal(
                false,
                converter.Convert(
                    ParenthesisType.Round,
                    typeof(bool),
                    ParenthesisType.Square,
                    CultureInfo.InvariantCulture
                )
            );
        }

        /// <summary>
        /// Verifies convert-back writes the parameter when checked and ignores uncheck.
        /// </summary>
        [Fact]
        public void ConvertBack_writes_parameter_only_when_checked()
        {
            var converter = EnumToBooleanConverter.Instance;

            Assert.Equal(
                ParenthesisType.Square,
                converter.ConvertBack(
                    true,
                    typeof(ParenthesisType),
                    ParenthesisType.Square,
                    CultureInfo.InvariantCulture
                )
            );
            Assert.Same(
                BindingOperations.DoNothing,
                converter.ConvertBack(
                    false,
                    typeof(ParenthesisType),
                    ParenthesisType.Square,
                    CultureInfo.InvariantCulture
                )
            );
        }
    }
}
