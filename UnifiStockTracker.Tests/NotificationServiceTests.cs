using FluentAssertions;
using Moq;
using System.Runtime.InteropServices;
using UnifiStockTracker.Services;
using Xunit;

namespace UnifiStockTracker.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void ShowNotification_ShouldNotThrowExceptions()
    {
        // Act
        var exception = Record.Exception(() =>
            NotificationService.ShowNotification("Test Title", "Test Message"));

        // Assert
        exception.Should().BeNull();
    }
}