using Common.Models;
using System.Text;
using Xunit;

namespace Common.Tests.Models;

public class ParsedLineTests
{
    private readonly Encoding _defaultEncoding = Encoding.UTF8;

    [Fact]
    public void ConstructorAssignsPropertiesCorrectly()
    {
        var parsedLine = new ParsedLine(123, "Test Text");
        Assert.Equal(123, parsedLine.Number);
        Assert.Equal("Test Text", parsedLine.Text);
    }

    [Theory]
    [InlineData(1L, "Apple", 1L, "Apple", true)]
    [InlineData(1L, "Apple", 2L, "Apple", false)]
    [InlineData(1L, "Apple", 1L, "Banana", false)]
    [InlineData(1L, "Apple", 2L, "Banana", false)]
    public void EqualsParsedLineReturnsCorrectly(long num1, string text1, long num2, string text2, bool expected)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        Assert.Equal(expected, line1.Equals(line2));
    }

    [Fact]
    public void EqualsObjectReturnsCorrectly()
    {
        var line1 = new ParsedLine(1L, "Apple");
        var line2 = new ParsedLine(1L, "Apple");
        var line3 = new ParsedLine(2L, "Banana");

        Assert.True(line1.Equals((object)line2));
        Assert.False(line1.Equals((object)line3));
        Assert.False(line1.Equals(null));
        Assert.False(line1.Equals("Not a ParsedLine"));
    }

    [Theory]
    [InlineData(1L, "Apple", 1L, "Apple", true)]
    [InlineData(1L, "Apple", 2L, "Apple", false)]
    public void EqualityOperatorsReturnCorrectly(long num1, string text1, long num2, string text2, bool expectedEqual)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        Assert.Equal(expectedEqual, line1 == line2);
        Assert.Equal(!expectedEqual, line1 != line2);
    }

    [Fact]
    public void GetHashCodeReturnsSameForEqualObjects()
    {
        var line1 = new ParsedLine(10L, "Same Text");
        var line2 = new ParsedLine(10L, "Same Text");
        Assert.Equal(line1.GetHashCode(), line2.GetHashCode());
    }

    [Fact]
    public void GetHashCodeReturnsDifferentForUnequalObjectsLikely()
    {
        var line1 = new ParsedLine(1L, "Text A");
        var line2 = new ParsedLine(2L, "Text B");
        Assert.NotEqual(line1.GetHashCode(), line2.GetHashCode());
    }

    [Theory]
    [InlineData(1L, "Apple", 1L, "Apple", 0)]
    [InlineData(1L, "Apple", 1L, "Banana", -1)]
    [InlineData(1L, "Banana", 1L, "Apple", 1)]
    [InlineData(1L, "Apple", 2L, "Apple", -1)]
    [InlineData(2L, "Apple", 1L, "Apple", 1)]
    [InlineData(10L, "Cherry", 2L, "Cherry", 1)]
    public void CompareToReturnsCorrectOrder(long num1, string text1, long num2, string text2, int expectedSign)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        int comparisonResult = line1.CompareTo(line2);

        Assert.Equal(expectedSign, comparisonResult);
    }

    [Theory]
    [InlineData(1L, "Apple", 1L, "Banana")]
    public void ComparisonOperatorsLessThanReturnsCorrectly(long num1, string text1, long num2, string text2)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        Assert.True(line1 < line2);
        Assert.True(line1 <= line2);
        Assert.False(line1 > line2);
        Assert.False(line1 >= line2);
    }

    [Theory]
    [InlineData(1L, "Banana", 1L, "Apple")]
    public void ComparisonOperatorsGreaterThanReturnsCorrectly(long num1, string text1, long num2, string text2)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        Assert.False(line1 < line2);
        Assert.False(line1 <= line2);
        Assert.True(line1 > line2);
        Assert.True(line1 >= line2);
    }

    [Theory]
    [InlineData(1L, "Apple", 1L, "Apple")]
    public void ComparisonOperatorsEqualToReturnsCorrectly(long num1, string text1, long num2, string text2)
    {
        var line1 = new ParsedLine(num1, text1);
        var line2 = new ParsedLine(num2, text2);
        Assert.False(line1 < line2);
        Assert.True(line1 <= line2);
        Assert.False(line1 > line2);
        Assert.True(line1 >= line2);
    }


    [Fact]
    public void ToStringReturnsCorrectFormat()
    {
        var parsedLine = new ParsedLine(42L, "The Answer");
        Assert.Equal("42. The Answer", parsedLine.ToString());
    }

    [Theory]
    [InlineData("123. Some Text", true, 123L, "Some Text")]
    [InlineData("456.  Leading Space Text", true, 456L, "Leading Space Text")]
    [InlineData(" 789 .  Spaces around dot ", true, 789L, "Spaces around dot")]
    public void TryParseValidInputReturnsTrueAndCorrectParsedLine(string input, bool expectedSuccess, long expectedNumber, string expectedText)
    {
        bool success = ParsedLine.TryParse(input, _defaultEncoding, out ParsedLine result, out string error);
        Assert.Equal(expectedSuccess, success);
        Assert.True(success, $"TryParse failed for valid input '{input}', error: {error}");
        if (success)
        {
            Assert.Equal(expectedNumber, result.Number);
            Assert.Equal(expectedText, result.Text);
        }
    }

    [Theory]
    [InlineData(null, "Malformed line: is null or empty")]
    [InlineData("", "Malformed line: is null or empty")]
    [InlineData("123 NoDotSeparator", "Malformed line: no dot or no text after dot. Value: 123 NoDotSeparator")]
    [InlineData("123.", "Malformed line: no dot or no text after dot. Value: 123.")]
    [InlineData("abc. InvalidNumberPart", "Malformed line (invalid number part \'abc\'). Full line: abc. InvalidNumberPart")]
    [InlineData(". TextStartsWithDot", "Malformed line (invalid number part ''). Full line: . TextStartsWithDot")]
    public void TryParseInvalidInputReturnsFalseAndErrorMessage(string? input, string expectedErrorFragment)
    {
        bool success = ParsedLine.TryParse(input!, _defaultEncoding, out ParsedLine result, out string error);
        Assert.False(success);
        Assert.Contains(expectedErrorFragment, error, System.StringComparison.Ordinal);
        Assert.Equal(0L, result.Number);
        Assert.Equal(string.Empty, result.Text);
    }
}
