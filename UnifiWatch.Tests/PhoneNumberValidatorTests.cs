using FluentAssertions;
using Xunit;
using UnifiWatch.Services.Notifications.Sms;

namespace UnifiWatch.Tests;

/// <summary>
/// Tests for phone number validation and normalization to E.164 format
/// </summary>
public class PhoneNumberValidatorTests
{
    #region IsValidE164 Tests

    [Fact]
    public void IsValidE164_WithValidE164_ShouldReturnTrue()
    {
        // Arrange
        var phoneNumber = "+12125551234";

        // Act
        var result = PhoneNumberValidator.IsValidE164(phoneNumber);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("+1")]
    [InlineData("+")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidE164_WithInvalidFormats_ShouldReturnFalse(string phoneNumber)
    {
        // Act
        var result = PhoneNumberValidator.IsValidE164(phoneNumber);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("12125551234")]
    [InlineData("(212) 555-1234")]
    [InlineData("212-555-1234")]
    public void IsValidE164_WithoutPlusPrefix_ShouldReturnFalse(string phoneNumber)
    {
        // Act
        var result = PhoneNumberValidator.IsValidE164(phoneNumber);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region NormalizeToE164 Tests

    [Theory]
    [InlineData("+12125551234", "+12125551234")]
    [InlineData("+33123456789", "+33123456789")]
    [InlineData("+49301234567", "+49301234567")]
    public void NormalizeToE164_WithValidE164_ShouldReturnUnchanged(string input, string expected)
    {
        // Act
        var result = PhoneNumberValidator.NormalizeToE164(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2125551234", "+12125551234")]
    [InlineData("(212) 555-1234", "+12125551234")]
    [InlineData("212-555-1234", "+12125551234")]
    [InlineData("212.555.1234", "+12125551234")]
    [InlineData("(212)555-1234", "+12125551234")]
    public void NormalizeToE164_WithUsFormats_ShouldNormalizeCorrectly(string input, string expected)
    {
        // Act
        var result = PhoneNumberValidator.NormalizeToE164(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeToE164_WithPlainDigits_ShouldAddCountryCode()
    {
        // Arrange
        var phoneNumber = "2125551234";

        // Act
        var result = PhoneNumberValidator.NormalizeToE164(phoneNumber, "1");

        // Assert
        result.Should().Be("+12125551234");
    }

    [Fact]
    public void NormalizeToE164_WithCustomCountryCode_ShouldUseProvided()
    {
        // Arrange
        var phoneNumber = "33123456789";

        // Act
        var result = PhoneNumberValidator.NormalizeToE164(phoneNumber, "33");

        // Assert
        result.Should().Be("+3333123456789");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("123")]
    public void NormalizeToE164_WithInvalidInputs_ShouldReturnNull(string phoneNumber)
    {
        // Act
        var result = PhoneNumberValidator.NormalizeToE164(phoneNumber);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region NormalizePhoneNumbers Tests

    [Fact]
    public void NormalizePhoneNumbers_WithMixedFormats_ShouldNormalizeAll()
    {
        // Arrange
        var phoneNumbers = new List<string>
        {
            "+12125551234",
            "2015551234",
            "(301) 555-9876",
            "invalid-number"
        };

        // Act
        var result = PhoneNumberValidator.NormalizePhoneNumbers(phoneNumbers);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("+12125551234");
        result.Should().Contain("+12015551234");
        result.Should().Contain("+13015559876");
    }

    [Fact]
    public void NormalizePhoneNumbers_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var phoneNumbers = new List<string>();

        // Act
        var result = PhoneNumberValidator.NormalizePhoneNumbers(phoneNumbers);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetValidationError Tests

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetValidationError_WithEmptyInput_ShouldReturnAppropriateError(string phoneNumber)
    {
        // Act
        var result = PhoneNumberValidator.GetValidationError(phoneNumber);

        // Assert
        result.Should().Contain("cannot be empty");
    }

    [Fact]
    public void GetValidationError_WithTooShortNumber_ShouldReturnLengthError()
    {
        // Act
        var result = PhoneNumberValidator.GetValidationError("123");

        // Assert
        result.Should().Contain("too short");
    }

    [Fact]
    public void GetValidationError_WithLetters_ShouldReturnLetterError()
    {
        // Act
        var result = PhoneNumberValidator.GetValidationError("abc-def-ghij");

        // Assert
        result.Should().Contain("invalid letters");
    }

    #endregion
}
