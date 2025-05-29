#pragma warning disable CA1051 // Do not declare visible instance fields

using System.Globalization;
using System.Text;
using Common.Interfaces;

namespace Common.Models;

public readonly struct ParsedLine(long number, string text) : IComparable<ParsedLine>, IStringSerializable<ParsedLine>, IEquatable<ParsedLine>
{
    public readonly long Number = number;
    public readonly string Text = text;

    public readonly bool Equals(ParsedLine other)
    {
        return Number == other.Number && Text == other.Text;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is ParsedLine other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Number, Text);
    }

    public static bool operator ==(ParsedLine left, ParsedLine right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ParsedLine left, ParsedLine right)
    {
        return !(left == right);
    }

    public static bool operator <(ParsedLine left, ParsedLine right) => left.CompareTo(right) < 0;
    public static bool operator <=(ParsedLine left, ParsedLine right) => left.CompareTo(right) <= 0;
    public static bool operator >(ParsedLine left, ParsedLine right) => left.CompareTo(right) > 0;
    public static bool operator >=(ParsedLine left, ParsedLine right) => left.CompareTo(right) >= 0;

    public readonly int CompareTo(ParsedLine other)
    {
        int stringCompare = string.Compare(Text, other.Text, StringComparison.Ordinal);
        if (stringCompare != 0)
        {
            return stringCompare;
        }
        return Number.CompareTo(other.Number);
    }

    public override string ToString()
    {
        return $"{Number}. {Text}";
    }

    public static bool TryParse(string value, Encoding encoding, out ParsedLine result, out string parseError)
    {
        parseError = string.Empty;
        result = new ParsedLine(0, string.Empty);
        if (string.IsNullOrEmpty(value))
        {
            parseError = "Malformed line: is null or empty";
            return false;
        }

        int dotIndex = value.IndexOf('.', StringComparison.InvariantCulture);
        if (dotIndex == -1 || dotIndex + 1 >= value.Length)
        {
            parseError = $"Malformed line: no dot or no text after dot. Value: {value}";
            return false;
        }

        var numberPart = value.Substring(0, dotIndex).Trim();
        var stringPart = value.Substring(dotIndex + 1).Trim();

        if (!long.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number))
        {
            parseError = $"Malformed line (invalid number part \'{numberPart}\'). Full line: {value}";
            return false;
        }

        result = new ParsedLine(number, stringPart);
        return true;
    }
}
