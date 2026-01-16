using FluentAssertions;
using Xunit;
using UnifiWatch.Services.Notifications.Sms;

namespace UnifiWatch.Tests;

/// <summary>
/// Tests for SMS message formatting, length handling, and content preparation
/// </summary>
public class SmsMessageFormatterTests
{
    #region IsWithinLimit Tests

    [Fact]
    public void IsWithinLimit_WithMessageUnder160Chars_ShouldReturnTrue()
    {
        // Arrange
        var message = "This is a short SMS message";

        // Act
        var result = SmsMessageFormatter.IsWithinLimit(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinLimit_WithMessageExactly160Chars_ShouldReturnTrue()
    {
        // Arrange
        var message = new string('A', 160);

        // Act
        var result = SmsMessageFormatter.IsWithinLimit(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinLimit_WithMessageOver160Chars_ShouldReturnFalse()
    {
        // Arrange
        var message = new string('A', 161);

        // Act
        var result = SmsMessageFormatter.IsWithinLimit(message);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsWithinLimit_WithNullOrEmpty_ShouldReturnFalse(string message)
    {
        // Act
        var result = SmsMessageFormatter.IsWithinLimit(message);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ShortenToLimit Tests

    [Fact]
    public void ShortenToLimit_WithMessageUnderLimit_ShouldReturnOriginal()
    {
        // Arrange
        var message = "Short message";

        // Act
        var result = SmsMessageFormatter.ShortenToLimit(message);

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void ShortenToLimit_WithLongMessage_ShouldShortenWithEllipsis()
    {
        // Arrange
        var message = new string('A', 200);

        // Act
        var result = SmsMessageFormatter.ShortenToLimit(message);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(160);
        result.Should().EndWith("...");
    }

    [Fact]
    public void ShortenToLimit_ShouldBreakAtWordBoundary()
    {
        // Arrange - create a message that will exceed 160 characters
        var message = "The quick brown fox jumps over the lazy dog and continues with more text to exceed the limit significantly and keep going with even more words to ensure we surpass the standard SMS length of one hundred sixty characters";

        // Act
        var result = SmsMessageFormatter.ShortenToLimit(message);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(160);
        result.Should().EndWith("...");
        // Should break at a word boundary (last space before limit)
        result.Should().NotEndWith(" ...");  // Should not have space before ellipsis
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShortenToLimit_WithNullOrEmpty_ShouldReturnAsIs(string message)
    {
        // Act
        var result = SmsMessageFormatter.ShortenToLimit(message);

        // Assert
        result.Should().Be(message);
    }

    #endregion

    #region ValidateMessage Tests

    [Fact]
    public void ValidateMessage_WithValidMessage_ShouldReturnValid()
    {
        // Arrange
        var message = "This is a valid SMS message";

        // Act
        var (isValid, errorMessage) = SmsMessageFormatter.ValidateMessage(message);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMessage_WithEmptyMessage_ShouldReturnInvalid()
    {
        // Act
        var (isValid, errorMessage) = SmsMessageFormatter.ValidateMessage("");

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("cannot be empty");
    }

    [Fact]
    public void ValidateMessage_WithExcessivelyLongMessage_ShouldReturnInvalid()
    {
        // Arrange
        var message = new string('A', 1700);

        // Act
        var (isValid, errorMessage) = SmsMessageFormatter.ValidateMessage(message);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("too long");
    }

    #endregion

    #region CalculateSegments Tests

    [Fact]
    public void CalculateSegments_WithShortMessage_ShouldReturnOne()
    {
        // Arrange
        var message = "Short SMS";

        // Act
        var result = SmsMessageFormatter.CalculateSegments(message);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void CalculateSegments_WithLongMessage_ShouldReturnMultiple()
    {
        // Arrange
        var message = new string('A', 500);

        // Act
        var result = SmsMessageFormatter.CalculateSegments(message);

        // Assert
        result.Should().Be(4); // 500 / 153 ≈ 3.27, rounded up to 4
    }

    [Fact]
    public void CalculateSegments_WithEmptyMessage_ShouldReturnZero()
    {
        // Act
        var result = SmsMessageFormatter.CalculateSegments("");

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region EstimateCost Tests

    [Fact]
    public void EstimateCost_WithShortMessage_ShouldReturnOneSgement()
    {
        // Arrange
        var message = "Hello";

        // Act
        var (segments, characters) = SmsMessageFormatter.EstimateCost(message);

        // Assert
        segments.Should().Be(1);
        characters.Should().Be(5);
    }

    [Fact]
    public void EstimateCost_WithLongMessage_ShouldCalculateCorrectly()
    {
        // Arrange
        var message = new string('A', 300);

        // Act
        var (segments, characters) = SmsMessageFormatter.EstimateCost(message);

        // Assert
        segments.Should().BeGreaterThan(1);
        characters.Should().Be(300);
    }

    #endregion

    #region Sanitize Tests

    [Fact]
    public void Sanitize_WithValidText_ShouldReturnUnchanged()
    {
        // Arrange
        var message = "Hello world! Test-123";

        // Act
        var result = SmsMessageFormatter.Sanitize(message);

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void Sanitize_WithNewlines_ShouldReplaceWithSpace()
    {
        // Arrange
        var message = "Line one\nLine two";

        // Act
        var result = SmsMessageFormatter.Sanitize(message);

        // Assert
        result.Should().Be("Line one Line two");
    }

    [Fact]
    public void Sanitize_WithProblematicChars_ShouldRemove()
    {
        // Arrange
        var message = "Test™ with© special® chars";

        // Act
        var result = SmsMessageFormatter.Sanitize(message);

        // Assert
        result.Should().NotContain("™");
        result.Should().NotContain("©");
        result.Should().NotContain("®");
    }

    #endregion

    #region RemoveEmoji Tests

    [Fact]
    public void RemoveEmoji_WithValidText_ShouldReturnUnchanged()
    {
        // Arrange
        var message = "Hello world!";

        // Act
        var result = SmsMessageFormatter.RemoveEmoji(message);

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void RemoveEmoji_WithEmoji_ShouldRemove()
    {
        // Arrange
        var message = "Stock alert 🎉 Product available 📱";

        // Act
        var result = SmsMessageFormatter.RemoveEmoji(message);

        // Assert
        result.Should().NotContain("🎉");
        result.Should().NotContain("📱");
    }

    #endregion

    #region PrepareForSms Tests

    [Fact]
    public void PrepareForSms_WithValidMessage_ShouldReturnPrepared()
    {
        // Arrange
        var message = "In stock alert for UDM-PRO at USA store";

        // Act
        var result = SmsMessageFormatter.PrepareForSms(message);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessThanOrEqualTo(160);
    }

    [Fact]
    public void PrepareForSms_WithLongMessageAndShortening_ShouldShorten()
    {
        // Arrange
        var message = new string('A', 200);

        // Act
        var result = SmsMessageFormatter.PrepareForSms(message, allowShortening: true);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(160);
        result.Should().EndWith("...");
    }

    [Fact]
    public void PrepareForSms_WithLongMessageNoShortening_ShouldNotShorten()
    {
        // Arrange
        var message = new string('A', 200);

        // Act
        var result = SmsMessageFormatter.PrepareForSms(message, allowShortening: false);

        // Assert
        result.Length.Should().Be(200);
        result.Should().NotEndWith("...");
    }

    #endregion
}
