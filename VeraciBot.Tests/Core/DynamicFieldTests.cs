using FluentAssertions;
using VeraciBot.Core.Entities;
using VeraciBot.Core.Enums;

namespace VeraciBot.Tests.Core
{
    public class DynamicFieldTests
    {
        [Theory]
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData(null, false)]
        public void BoolValue_ShouldReadYesNoCompatibleValues(string value, bool expected)
        {
            var field = new TestDynamicField
            {
                Type = EFieldType.YesNo,
                Value = value
            };

            field.BoolValue.Should().Be(expected);
        }

        [Fact]
        public void BoolValue_ShouldWriteOnlyWhenFieldTypeIsYesNo()
        {
            var yesNoField = new TestDynamicField { Type = EFieldType.YesNo };
            var textField = new TestDynamicField { Type = EFieldType.SmallText, Value = "original" };

            yesNoField.BoolValue = true;
            textField.BoolValue = true;

            yesNoField.Value.Should().Be("1");
            textField.Value.Should().Be("original");
        }

        [Fact]
        public void TypeDescription_ShouldUseEnumDescriptionWhenAvailable()
        {
            var field = new TestDynamicField
            {
                Type = EFieldType.YesNo
            };

            field.TypeDescription.Should().Be("Yes/No");
        }

        private sealed class TestDynamicField : DynamicField
        {
        }
    }
}
