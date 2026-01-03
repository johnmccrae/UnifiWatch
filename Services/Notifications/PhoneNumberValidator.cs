using System.Text.RegularExpressions;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Validates and normalizes phone numbers to E.164 format
/// E.164 format: +[country code][number], e.g., +12125552368
/// </summary>
public static class PhoneNumberValidator
{
    /// <summary>
    /// Validates phone number and normalizes to E.164 format
    /// </summary>
    /// <param name="phoneNumber">Phone number in any common format</param>
    /// <returns>Normalized E.164 format phone number, or null if invalid</returns>
    public static string? NormalizeToE164(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters except leading +
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");

        // Remove leading + if present (we'll add it back)
        if (cleaned.StartsWith("+"))
            cleaned = cleaned[1..];

        // Must be 10-15 digits (E.164 standard)
        if (!Regex.IsMatch(cleaned, @"^\d{10,15}$"))
            return null;

        // Add + prefix for E.164 format
        return $"+{cleaned}";
    }

    /// <summary>
    /// Validates if a phone number is in valid E.164 format
    /// </summary>
    /// <param name="e164PhoneNumber">Phone number in E.164 format</param>
    /// <returns>True if valid E.164 format</returns>
    public static bool IsValidE164(string? e164PhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(e164PhoneNumber))
            return false;

        return Regex.IsMatch(e164PhoneNumber, @"^\+\d{10,15}$");
    }

    /// <summary>
    /// Validates phone number in any common format
    /// </summary>
    /// <param name="phoneNumber">Phone number in any format</param>
    /// <returns>True if phone number can be validated and normalized</returns>
    public static bool IsValid(string? phoneNumber)
    {
        return NormalizeToE164(phoneNumber) != null;
    }
}
