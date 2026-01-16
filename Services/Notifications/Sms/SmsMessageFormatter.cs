using System.Text;

namespace UnifiWatch.Services.Notifications.Sms;

/// <summary>
/// Handles SMS message formatting, length validation, and intelligent shortening
/// Respects SMS segment limit (160 characters) with support for shortening/ellipsis
/// </summary>
public static class SmsMessageFormatter
{
    private const int StandardSmsLength = 160;
    private const string Ellipsis = "...";

    /// <summary>
    /// Validates if a message fits within SMS length limit
    /// </summary>
    /// <param name="message">The SMS message to check</param>
    /// <param name="maxLength">Maximum allowed length (default: 160)</param>
    /// <returns>True if message fits, false otherwise</returns>
    public static bool IsWithinLimit(string message, int maxLength = StandardSmsLength)
    {
        return !string.IsNullOrEmpty(message) && message.Length <= maxLength;
    }

    /// <summary>
    /// Shortens a message to fit within SMS length limit
    /// Attempts to break at word boundaries to preserve meaning
    /// </summary>
    /// <param name="message">The message to shorten</param>
    /// <param name="maxLength">Maximum allowed length (default: 160, reduced to 157 to fit ellipsis)</param>
    /// <returns>Shortened message with ellipsis, or original if already fits</returns>
    public static string ShortenToLimit(string message, int maxLength = StandardSmsLength)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        if (IsWithinLimit(message, maxLength))
            return message;

        // Reserve space for ellipsis
        var targetLength = maxLength - Ellipsis.Length;

        if (targetLength <= 0)
            return message.Substring(0, maxLength);

        // Try to break at word boundary (last space before limit)
        var truncated = message.Substring(0, targetLength);
        var lastSpaceIndex = truncated.LastIndexOf(' ');

        if (lastSpaceIndex > targetLength * 0.7) // At least 70% of target length for last space
        {
            return message.Substring(0, lastSpaceIndex) + Ellipsis;
        }

        // No good word boundary found, just truncate
        return truncated + Ellipsis;
    }

    /// <summary>
    /// Validates SMS message content (no null, not empty)
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <returns>Tuple of (isValid, errorMessage)</returns>
    public static (bool isValid, string errorMessage) ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return (false, "Message cannot be empty");

        if (message.Length > 160 * 10) // Allow up to 10 SMS segments
            return (false, $"Message too long ({message.Length} characters, maximum 1600)");

        return (true, string.Empty);
    }

    /// <summary>
    /// Calculates the number of SMS segments needed to send a message
    /// </summary>
    /// <param name="message">The message to analyze</param>
    /// <returns>Number of SMS segments needed (1-10 typically)</returns>
    public static int CalculateSegments(string message)
    {
        if (string.IsNullOrEmpty(message))
            return 0;

        // Standard SMS: 160 chars per segment
        // Multi-part SMS: 153 chars per segment (7 bytes for UDH header)
        if (message.Length <= StandardSmsLength)
            return 1;

        // Multiple segments needed
        return (int)Math.Ceiling((double)message.Length / 153);
    }

    /// <summary>
    /// Estimates the cost in terms of SMS segments
    /// Useful for informing users or rate limiting
    /// </summary>
    /// <param name="message">Message to analyze</param>
    /// <returns>Segment count and total character count</returns>
    public static (int segments, int characters) EstimateCost(string message)
    {
        if (string.IsNullOrEmpty(message))
            return (0, 0);

        return (CalculateSegments(message), message.Length);
    }

    /// <summary>
    /// Sanitizes message content for SMS delivery
    /// Removes problematic characters that may not render in SMS
    /// </summary>
    /// <param name="message">Message to sanitize</param>
    /// <returns>Sanitized message safe for SMS transmission</returns>
    public static string Sanitize(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sb = new StringBuilder();

        foreach (var c in message)
        {
            // Replace newlines with spaces first (before checking IsWhiteSpace)
            if (c == '\n' || c == '\r')
            {
                sb.Append(' ');
            }
            // Keep alphanumeric, spaces, and common punctuation
            else if (char.IsLetterOrDigit(c) || 
                c == ' ' || c == '\t' ||
                c is '.' or ',' or '!' or '?' or '-' or ':' or ';' or '(' or ')' or 
                    '&' or '@' or '#' or '$' or '%' or '*' or '+' or '=' or '"' or '\'' or '/')
            {
                sb.Append(c);
            }
            // Skip other problematic characters
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes/replaces Unicode emoji and special symbols that may fail in SMS
    /// </summary>
    /// <param name="message">Message potentially containing emoji</param>
    /// <returns>Message with emoji removed or replaced with text alternatives</returns>
    public static string RemoveEmoji(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sb = new StringBuilder();

        foreach (var c in message)
        {
            // Keep only BMP (Basic Multilingual Plane) characters
            // Skip emoji ranges: supplementary planes, etc.
            if (c < '\ud800' || c > '\udbff')
            {
                sb.Append(c);
            }
            // For emoji surrogates, skip both parts
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prepares a message for SMS transmission with all sanitization
    /// </summary>
    /// <param name="message">Raw message text</param>
    /// <param name="allowShortening">Whether to shorten if exceeds SMS length</param>
    /// <returns>Prepared message ready for SMS transmission</returns>
    public static string PrepareForSms(string message, bool allowShortening = true)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Remove emoji
        var cleaned = RemoveEmoji(message);

        // Sanitize
        cleaned = Sanitize(cleaned);

        // Shorten if needed
        if (allowShortening && !IsWithinLimit(cleaned))
        {
            cleaned = ShortenToLimit(cleaned);
        }

        return cleaned;
    }
}
