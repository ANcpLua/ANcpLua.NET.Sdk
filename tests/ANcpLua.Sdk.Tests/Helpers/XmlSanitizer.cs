namespace ANcpLua.Sdk.Tests.Helpers;

/// <summary>
/// Utility to sanitize strings for XML 1.0 compatibility.
/// XML 1.0 does not allow certain control characters (like form feed 0x0C).
/// </summary>
internal static class XmlSanitizer
{
    /// <summary>
    /// Removes characters that are invalid in XML 1.0 documents.
    /// Valid characters: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
    /// </summary>
    public static string SanitizeForXml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        // Fast path: check if any invalid characters exist
        var hasInvalidChars = false;
        foreach (var ch in text)
        {
            if (!IsValidXmlChar(ch))
            {
                hasInvalidChars = true;
                break;
            }
        }

        if (!hasInvalidChars)
            return text;

        // Slow path: filter out invalid characters
        return new string(text.Where(IsValidXmlChar).ToArray());
    }

    private static bool IsValidXmlChar(char ch)
    {
        // XML 1.0 valid characters:
        // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
        // Note: Surrogate pairs (#x10000-#x10FFFF) are handled by .NET as two chars
        return ch == 0x9 ||
               ch == 0xA ||
               ch == 0xD ||
               (ch >= 0x20 && ch <= 0xD7FF) ||
               (ch >= 0xE000 && ch <= 0xFFFD);
    }
}
