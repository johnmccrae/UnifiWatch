using FluentAssertions;
using Moq;
using System.Runtime.InteropServices;
using UnifiWatch.Services;
using Xunit;

namespace UnifiWatch.Tests;

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