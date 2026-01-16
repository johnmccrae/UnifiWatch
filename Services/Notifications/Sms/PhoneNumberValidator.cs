using System.Text.RegularExpressions;

namespace UnifiWatch.Services.Notifications.Sms;

/// <summary>
/// Validates and normalizes phone numbers to E.164 format
/// E.164 format: +[country code][number], e.g., +12125551234
/// </summary>
public static class PhoneNumberValidator
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);
    // Match patterns: 2125551234, (212) 555-1234, 212-555-1234, 212.555.1234, +12125551234
    private static readonly Regex UsPhoneRegex = new(@"^(\+?1)?[-\s.]?\(?(\d{3})\)?[-\s.]?(\d{3})[-\s.]?(\d{4})$", RegexOptions.Compiled);
    private static readonly Regex PlainDigitsRegex = new(@"^\d{7,15}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates if a phone number is in valid E.164 format
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>True if valid E.164 format, false otherwise</returns>
    public static bool IsValidE164(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return E164Regex.IsMatch(phoneNumber);
    }

    /// <summary>
    /// Attempts to normalize a phone number to E.164 format
    /// Handles various formats: (212) 555-1234, 212-555-1234, 2125551234, +12125551234, etc.
    /// </summary>
    /// <param name="phoneNumber">Phone number in any common format</param>
    /// <param name="countryCode">Default country code to use if not provided (default: +1 for USA/Canada)</param>
    /// <returns>Normalized E.164 phone number, or null if unable to parse</returns>
    public static string? NormalizeToE164(string phoneNumber, string countryCode = "1")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Already in E.164 format
        if (IsValidE164(phoneNumber))
            return phoneNumber;

        // Try US/Canada phone number format
        var usMatch = UsPhoneRegex.Match(phoneNumber);
        if (usMatch.Success)
        {
            var areaCode = usMatch.Groups[2].Value;    // Area code (212)
            var exchange = usMatch.Groups[3].Value;     // Exchange (555)
            var lineNumber = usMatch.Groups[4].Value;   // Line number (1234)
            var digits = areaCode + exchange + lineNumber;
            return $"+{countryCode}{digits}";
        }

        // Try plain digits (7-15 digits)
        var digitsMatch = PlainDigitsRegex.Match(phoneNumber);
        if (digitsMatch.Success)
        {
            // If country code is 1 (US/Canada) and we have 11 digits starting with 1, assume it already includes country code
            if (countryCode == "1" && phoneNumber.Length == 11 && phoneNumber[0] == '1')
            {
                return $"+{phoneNumber}";
            }

            // Otherwise prepend the provided country code
            return $"+{countryCode}{phoneNumber}";
        }

        // If starts with + but not valid E.164, try removing and re-validating
        if (phoneNumber.StartsWith('+'))
        {
            var digitsOnly = Regex.Replace(phoneNumber.Substring(1), @"[^\d]", "");
            if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= 7 && digitsOnly.Length <= 15)
            {
                return $"+{digitsOnly}";
            }
        }

        // Last attempt: extract all digits and try
        var allDigits = Regex.Replace(phoneNumber, @"[^\d+]", "");
        if (allDigits.StartsWith('+'))
        {
            return NormalizeToE164(allDigits);
        }

        var digitsOnly2 = Regex.Replace(phoneNumber, @"[^\d]", "");
        if (PlainDigitsRegex.IsMatch(digitsOnly2))
        {
            return $"+{countryCode}{digitsOnly2}";
        }

        return null;
    }

    /// <summary>
    /// Validates and normalizes a list of phone numbers to E.164 format
    /// </summary>
    /// <param name="phoneNumbers">List of phone numbers in any format</param>
    /// <param name="countryCode">Default country code (default: +1)</param>
    /// <returns>List of valid normalized E.164 phone numbers (invalid ones excluded)</returns>
    public static List<string> NormalizePhoneNumbers(List<string> phoneNumbers, string countryCode = "1")
    {
        var normalized = new List<string>();

        foreach (var number in phoneNumbers)
        {
            var normalizedNumber = NormalizeToE164(number, countryCode);
            if (normalizedNumber != null)
            {
                normalized.Add(normalizedNumber);
            }
        }

        return normalized;
    }

    /// <summary>
    /// Gets a friendly description of phone number validation errors
    /// </summary>
    /// <param name="phoneNumber">Phone number that failed validation</param>
    /// <returns>Error description for user display</returns>
    public static string GetValidationError(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "Phone number cannot be empty";

        if (phoneNumber.Length < 7)
            return $"Phone number too short (minimum 7 digits, got {phoneNumber.Length})";

        if (phoneNumber.Length > 20)
            return $"Phone number too long (maximum 20 characters, got {phoneNumber.Length})";

        if (Regex.IsMatch(phoneNumber, @"[a-zA-Z]"))
            return "Phone number contains invalid letters";

        return "Phone number format not recognized. Use E.164 format (+1234567890) or standard US format";
    }
}
