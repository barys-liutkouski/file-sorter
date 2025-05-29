using System.Text;

namespace Common.Helpers;

public static class EncodingHelper
{
    public static Encoding ParseEncoding(string encodingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodingName);

        return encodingName.ToLowerInvariant() switch
        {
            "utf8" or "utf-8" => Encoding.UTF8,
            "ascii" => Encoding.ASCII,
            "utf16" or "utf-16" or "unicode" => Encoding.Unicode,
            "utf32" or "utf-32" => Encoding.UTF32,
            "latin1" or "iso-8859-1" => Encoding.GetEncoding("iso-8859-1"),
            _ => Encoding.GetEncoding(encodingName),
        };
    }
}
