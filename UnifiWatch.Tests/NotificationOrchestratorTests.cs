using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnifiWatch.Models;
using UnifiWatch.Services.Localization;
using UnifiWatch.Services.Notifications;
using Xunit;

namespace UnifiWatch.Tests;

public class NotificationOrchestratorTests
{
    private class TestEmailProvider : IEmailProvider
    {
        public ConcurrentBag<(string recipient, string subject, string body)> Sent { get; } = new();
        public Task<bool> SendAsync(string recipient, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default)
        {
            Sent.Add((recipient, subject, plainBody));
            return Task.FromResult(true);
        }
        public Task<Dictionary<string, bool>> SendBatchAsync(List<string> recipients, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default)
        {
            var dict = recipients.ToDictionary(r => r, r => true);
            foreach (var r in recipients)
            {
                Sent.Add((r, subject, plainBody));
            }
            return Task.FromResult(dict);
        }
    }

    private class TestSmsProvider : ISmsProvider
    {
        public ConcurrentBag<(string recipient, string message)> Sent { get; } = new();
        public Task<bool> SendAsync(string recipient, string message, CancellationToken cancellationToken = default)
        {
            Sent.Add((recipient, message));
            return Task.FromResult(true);
        }
        public Task<Dictionary<string, bool>> SendBatchAsync(IList<string> recipients, string message, CancellationToken cancellationToken = default)
        {
            var dict = recipients.ToDictionary(r => r, r => true);
            foreach (var r in recipients)
            {
                Sent.Add((r, message));
            }
            return Task.FromResult(dict);
        }
    }

    [Fact]
    public async Task NotifyInStock_SendsEmailAndSms_WithDedupe()
    {
        // Arrange
        var emailProvider = new TestEmailProvider();
        var smsProvider = new TestSmsProvider();
        var loc = ResourceLocalizer.Load(System.Globalization.CultureInfo.GetCultureInfo("en-CA"));
        var emailSvc = new EmailNotificationService(emailProvider, loc, new LoggerFactory().CreateLogger<EmailNotificationService>());
        var smsSettings = new SmsNotificationSettings
        {
            Enabled = true,
            AllowMessageShortening = true,
            MaxMessageLength = 160,
            ServiceType = "twilio",
            Recipients = new List<string> { "+12125551234" }
        };
        var smsSvc = new SmsNotificationService(smsProvider, loc, new LoggerFactory().CreateLogger<SmsNotificationService>(), smsSettings);

        var emailOptions = Options.Create(new EmailNotificationSettings
        {
            Enabled = true,
            FromAddress = "noreply@example.com",
            Recipients = new List<string> { "user@example.com" },
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            UseTls = true,
            CredentialKey = "email-smtp"
        });
        var smsOptions = Options.Create(new SmsNotificationSettings
        {
            Enabled = true,
            AllowMessageShortening = true,
            MaxMessageLength = 160,
            ServiceType = "twilio",
            Recipients = new List<string> { "+12125551234" },
            AuthTokenKeyName = "sms:auth-token"
        });

        var orchestrator = new NotificationOrchestrator(
            emailSvc,
            smsSvc,
            emailOptions,
            smsOptions,
            new LoggerFactory().CreateLogger<NotificationOrchestrator>(),
            dedupeWindow: TimeSpan.FromMinutes(5));

        var product = new UnifiProduct { Name = "Test Product", SKU = "TEST-001", Available = true, Price = 9999 };
        var store = "USA";

        // Act: First notify should send both channels
        await orchestrator.NotifyInStockAsync(new[] { product }, store);
        // Act: Second notify within dedupe window should not send
        await orchestrator.NotifyInStockAsync(new[] { product }, store);

        // Assert
        Assert.Equal(1, emailProvider.Sent.Count);
        Assert.Equal(1, smsProvider.Sent.Count);
    }
}
