# UnifiWatch - Architecture Overview

**Purpose**: Deep-dive into the codebase architecture, design patterns, and implementation details  
**Intended Audience**: C# developers who want to understand UnifiWatch internals and contribute to the project  
**Not for**: Getting started with building/testing (see platform-specific guides below)

---

## Getting Started with UnifiWatch

This document explains **how UnifiWatch works internally**. For hands-on build/test/deployment instructions, see:

- **Windows Development**: [WINDOWS_DEVELOPER_GUIDE.md](WINDOWS_DEVELOPER_GUIDE.md)
- **Linux Development**: [LINUX_DEVELOPER_GUIDE.md](LINUX_DEVELOPER_GUIDE.md)
- **macOS Development**: [MACOS_DEVELOPER_GUIDE.md](MACOS_DEVELOPER_GUIDE.md)
- **Windows Deployment**: [WINDOWS_END_USER_GUIDE.md](WINDOWS_END_USER_GUIDE.md)
- **Linux Deployment**: [LINUX_END_USER_GUIDE.md](LINUX_END_USER_GUIDE.md)
- **macOS Deployment**: [MACOS_END_USER_GUIDE.md](MACOS_END_USER_GUIDE.md)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Configuration System](#configuration-system)
3. [Credential Management](#credential-management)
4. [Stock Monitoring](#stock-monitoring)
5. [Notifications](#notifications)
6. [Background Service](#background-service)
7. [Dependency Injection](#dependency-injection)
8. [Localization](#localization)
9. [Testing](#testing)
10. [Common Workflows](#common-workflows)

---

## Architecture Overview

UnifiWatch follows a layered architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                     CLI Entry Point (Program.cs)            │
│  Handles: --configure, --show-config, --install-service    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────────────┐
        │  Dependency Injection Setup    │
        │  (Services registration)       │
        └────────────────────────────────┘
                     │
        ┌────────────┴────────────────────────────────────────┐
        │                                                      │
        ▼                                                      ▼
┌───────────────────────┐            ┌──────────────────────────────┐
│ Background Service    │            │    CLI Commands              │
│ (StockWatcher)        │            │ (Configuration, Testing)     │
└───────────────────────┘            └──────────────────────────────┘
        │                                         │
        ▼                                         ▼
┌───────────────────────────────────────────────────────────────┐
│              Configuration Management                         │
│  (ConfigurationProvider)                                      │
└─────────────────────────────┬─────────────────────────────────┘
                              │
        ┌─────────────────────┼──────────────────────┐
        │                     │                      │
        ▼                     ▼                      ▼
┌──────────────┐    ┌──────────────────┐   ┌─────────────────┐
│ Credentials  │    │ Stock APIs       │   │ Notifications   │
│ (Secure)     │    │ (Monitoring)     │   │ (Multi-channel) │
└──────────────┘    └──────────────────┘   └─────────────────┘
```

### Key Design Principles

1. **Interface-Based Design**: All major components use interfaces for testability and flexibility
2. **Dependency Injection**: All dependencies passed via constructors, registered in DI container
3. **Platform Abstraction**: Platform-specific code isolated in dedicated implementations
4. **Error Handling**: Graceful degradation (one failure doesn't stop entire system)
5. **Security-First**: Credentials never logged, encrypted at rest, secure in transit
6. **Testability**: High test coverage with mocking infrastructure

---

## Configuration System

Configuration management is the foundation for all other systems. Understanding it is key to understanding UnifiWatch.

### The Configuration Flow

```
[User runs: --configure]
           ▼
[ConfigurationWizard.cs]
  (asks interactive questions)
           ▼
[Store credentials securely]
  (via ICredentialProvider)
           ▼
[Save configuration JSON]
  (via ConfigurationProvider.SaveAsync)
           ▼
[Store at platform-specific path]
  (%APPDATA%\UnifiWatch on Windows,
   ~/.config/unifiwatch on Linux/macOS)
```

### ConfigurationProvider.cs

**Responsibility**: Load, save, validate, and backup configuration files.

**Key Methods**:
```csharp
// Load configuration from disk
public async Task<ServiceConfiguration> LoadAsync(CancellationToken ct)
{
    // 1. Check if config file exists
    // 2. Read JSON from platform-specific path
    // 3. Deserialize to ServiceConfiguration object
    // 4. Validate all required fields
    // 5. Return loaded configuration
}

// Save configuration to disk
public async Task SaveAsync(ServiceConfiguration config, CancellationToken ct)
{
    // 1. Validate configuration object
    // 2. Create automatic timestamped backup
    // 3. Serialize to JSON
    // 4. Write to platform-specific path
    // 5. Set file permissions (chmod 600 on Unix)
}

// Validate configuration
public IEnumerable<string> Validate(ServiceConfiguration config)
{
    // Check: Product ID not empty
    // Check: Check interval > 0
    // Check: Email SMTP settings valid if enabled
    // Check: Phone numbers valid if SMS enabled
    // Return list of validation errors (empty if valid)
}
```

### ServiceConfiguration.cs

**What it is**: A strongly-typed model representing all application settings.

**Structure**:
```csharp
public class ServiceConfiguration
{
    // Product monitoring
    public string ProductId { get; set; }          // "UDM-SE"
    public string ProductName { get; set; }        // "UniFi Dream Machine SE"
    
    // Check settings
    public ServiceSettings Service { get; set; }   // Interval, enabled flag
    
    // Monitoring settings
    public MonitoringSettings Monitoring { get; set; }  // Region, API selection
    
    // Notification settings
    public NotificationSettings Notifications { get; set; }  // Email, SMS, desktop config
}
```

### Practical Example: Loading Configuration

```csharp
// In Program.cs or a service constructor
var configProvider = serviceProvider.GetRequiredService<IConfigurationProvider>();

// Load configuration
var config = await configProvider.LoadAsync(cancellationToken);

// Check if configuration is valid
var errors = configProvider.Validate(config);
if (errors.Any())
{
    Console.WriteLine("Configuration errors:");
    foreach (var error in errors)
        Console.WriteLine($"  - {error}");
    return;
}

// Use configuration
Console.WriteLine($"Monitoring: {config.ProductName}");
Console.WriteLine($"Check interval: {config.Service.CheckIntervalSeconds}s");
```

### File Locations by Platform

| Platform | Location | Example |
|----------|----------|---------|
| Windows | `%APPDATA%\UnifiWatch\` | `C:\Users\John\AppData\Roaming\UnifiWatch\` |
| macOS | `~/.config/unifiwatch/` | `/Users/john/.config/unifiwatch/` |
| Linux | `~/.config/unifiwatch/` or `$XDG_CONFIG_HOME/unifiwatch/` | `/home/john/.config/unifiwatch/` |

### Configuration Watching

The background service watches for configuration file changes:

```csharp
// In StockWatcher.cs
var watcher = new FileSystemWatcher(configDirectory)
{
    Filter = "config.json"
};

watcher.Changed += async (s, e) =>
{
    // Configuration file changed - reload it
    var newConfig = await _configProvider.LoadAsync(stoppingToken);
    
    // Update internal state without restarting service
    _currentConfig = newConfig;
};
```

---

## Credential Management

Credentials (passwords, API keys) are sensitive data that must be stored securely. UnifiWatch uses platform-specific secure storage mechanisms.

### The Credential Problem

Why can't we just save passwords in the config file?

```csharp
// BAD - NEVER DO THIS
{
    "email": {
        "smtpPassword": "MySecurePassword123"  // ❌ Plain text!
    }
}
```

**Problems**:
- Anyone with file access can read the password
- Password appears in backups, version control, logs
- No way to revoke password without modifying file

**Solution**: Store passwords in platform-specific secure storage, reference them by key.

### How UnifiWatch Does It

```csharp
// Good approach
{
    "email": {
        "smtpPassword": null,  // Not in config file
        "credentialKey": "UnifiWatch:email-smtp"  // Reference to secure storage
    }
}

// Retrieve at runtime
var credentialProvider = serviceProvider.GetRequiredService<ICredentialProvider>();
var password = await credentialProvider.RetrieveAsync("UnifiWatch:email-smtp");
```

### ICredentialProvider Interface

All credential implementations follow this interface:

```csharp
public interface ICredentialProvider
{
    // Store a credential
    Task StoreAsync(string key, string value);
    
    // Retrieve a credential
    Task<string> RetrieveAsync(string key);
    
    // Check if credential exists
    Task<bool> ExistsAsync(string key);
    
    // Delete a credential
    Task DeleteAsync(string key);
    
    // List all stored credentials
    Task<IEnumerable<string>> ListAsync();
}
```

### Platform-Specific Implementations

#### Windows: CredentialManager (DPAPI)

```csharp
// File: Services/Credentials/WindowsCredentialManager.cs

public class WindowsCredentialManager : ICredentialProvider
{
    public async Task StoreAsync(string key, string value)
    {
        // Use Windows Credential Manager API
        // Key format: "UnifiWatch:email-smtp"
        // Value: encrypted by DPAPI automatically
        
        // Under the hood:
        // DPAPI uses Windows user's login password as key
        // So only that user can decrypt it
    }
    
    public async Task<string> RetrieveAsync(string key)
    {
        // Retrieve from Windows Credential Manager
        // DPAPI automatically decrypts it
        // Only possible if same user is logged in
    }
}
```

**Advantages**:
- Built into Windows
- Automatically encrypted with user's password
- Can't be stolen without user's Windows password
- Stored in Credential Manager (visible in Control Panel)

**How to verify**: Open Control Panel → Credential Manager → Windows Credentials → Look for "UnifiWatch:*" entries

#### macOS: Keychain

```csharp
// File: Services/Credentials/MacOsKeychain.cs

public class MacOsKeychain : ICredentialProvider
{
    public async Task StoreAsync(string key, string value)
    {
        // Use 'security' command-line tool
        // Stores in macOS Keychain
        // Key format: "UnifiWatch:email-smtp"
        
        // Command:
        // security add-generic-password -s "UnifiWatch:email-smtp" -a UnifiWatch -w "password"
    }
    
    public async Task<string> RetrieveAsync(string key)
    {
        // Retrieve from Keychain
        // Command:
        // security find-generic-password -s "UnifiWatch:email-smtp" -w
    }
}
```

**Advantages**:
- Built into macOS
- Syncs across iCloud if enabled
- User can manage access in Keychain Access app
- Prompts user for authorization if needed

#### Linux: Secret Service (with Fallback)

```csharp
// File: Services/Credentials/LinuxSecretService.cs

public class LinuxSecretService : ICredentialProvider
{
    public async Task StoreAsync(string key, string value)
    {
        // Try to use secret-service (GNOME Keyring, KDE Wallet)
        // Falls back to EncryptedFileCredentialProvider if not available
        
        // secret-service uses D-Bus to communicate with keyring daemon
        // If daemon not running, uses encrypted file fallback
    }
}
```

#### Cross-Platform Fallback: EncryptedFileCredentialProvider

For headless systems or when native storage unavailable:

```csharp
// File: Services/Credentials/EncryptedFileCredentialProvider.cs

public class EncryptedFileCredentialProvider : ICredentialProvider
{
    public async Task StoreAsync(string key, string value)
    {
        // 1. Generate encryption key from machine-specific data:
        //    - Machine ID (Windows) or /etc/machine-id (Linux)
        //    - Current username
        //    - Hostname
        //    Key is NOT stored in file (derived each time)
        
        // 2. Use PBKDF2 to strengthen key (100,000 iterations)
        
        // 3. Generate random salt and IV
        
        // 4. Encrypt value with AES-256-CBC
        
        // 5. Store [salt][IV][ciphertext] in file
        
        // File structure:
        // 32 bytes: salt
        // 16 bytes: IV
        // N bytes: ciphertext
    }
    
    public async Task<string> RetrieveAsync(string key)
    {
        // 1. Read [salt][IV][ciphertext] from file
        
        // 2. Re-derive key using same machine-specific data + salt
        
        // 3. Decrypt using AES-256-CBC with IV
        
        // 4. Return plaintext
    }
}
```

**Important**: The encryption key is derived from machine-specific data, not stored in a password file. This means:
- ✅ Same key can be regenerated on any machine copy
- ✅ Key is not stored anywhere (can't be stolen from file)
- ✅ Requires machine ID + username to decrypt (hard to crack)

**File location**: `~/.config/unifiwatch/credentials.enc` (Linux/macOS) or `%APPDATA%\UnifiWatch\credentials.enc` (Windows)

### CredentialProviderFactory

The factory automatically selects the right credential provider:

```csharp
public class CredentialProviderFactory
{
    public static ICredentialProvider CreateProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsCredentialManager();  // DPAPI
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsKeychain();  // Keychain
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                return new LinuxSecretService();  // Try secret-service
            }
            catch
            {
                // Fallback if secret-service not available
                return new EncryptedFileCredentialProvider();
            }
        }
        
        throw new PlatformNotSupportedException();
    }
}
```

### Practical Example: Using Credentials

```csharp
// In ConfigurationWizard.cs - storing a credential
public async Task ConfigureEmailAsync()
{
    Console.Write("SMTP Password: ");
    var password = Console.ReadLine();
    
    // Store securely in platform-specific storage
    await _credentialProvider.StoreAsync("UnifiWatch:email-smtp", password);
    
    // Configuration file just stores the key, not the password
    _config.Notifications.Email.CredentialKey = "UnifiWatch:email-smtp";
}

// In SmtpEmailProvider.cs - retrieving a credential
public async Task SendAsync(EmailNotification notification)
{
    var credentialKey = _config.Notifications.Email.CredentialKey;
    
    // Retrieve password from secure storage
    var password = await _credentialProvider.RetrieveAsync(credentialKey);
    
    // Use password to connect to SMTP server
    var client = new SmtpClient();
    await client.ConnectAsync("smtp.gmail.com", 587);
    await client.AuthenticateAsync(username, password);
}
```

---

## Stock Monitoring

Stock monitoring is the core functionality: checking Ubiquiti API for product availability and sending notifications.

### The Stock Check Flow

```
[Service runs every N seconds]
           ▼
[Call IUnifiStockService.GetStockAsync()]
           ▼
[UnifiStockService uses GraphQL API]
           ▼
[Parse response, check if in stock]
           ▼
[Compare with previous state]
           ▼
[If changed: send notifications]
           ▼
[Sleep, repeat]
```

### IUnifiStockService Interface

```csharp
public interface IUnifiStockService
{
    // Check if product is available (in stock)
    Task<bool> IsInStockAsync(string productId, CancellationToken ct);
    
    // Get detailed product information
    Task<UnifiProduct> GetProductAsync(string productId, CancellationToken ct);
    
    // List all products
    Task<IEnumerable<UnifiProduct>> GetProductsAsync(CancellationToken ct);
}
```

### UnifiStockService.cs (GraphQL Primary)

The primary implementation uses GraphQL API (faster, more efficient):

```csharp
public class UnifiStockService : IUnifiStockService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnifiStockService> _logger;
    
    public async Task<bool> IsInStockAsync(string productId, CancellationToken ct)
    {
        // 1. Build GraphQL query
        var query = @"
            query {
                getProduct(id: """ + productId + @""") {
                    id
                    name
                    inStock
                }
            }";
        
        // 2. Send HTTP POST request to Ubiquiti GraphQL endpoint
        var response = await _httpClient.PostAsync(
            "https://api.ui.com/graphql",
            new StringContent(query),
            ct);
        
        // 3. Parse JSON response
        var content = await response.Content.ReadAsStringAsync(ct);
        var json = JsonDocument.Parse(content);
        
        // 4. Extract inStock field
        var inStock = json
            .RootElement
            .GetProperty("data")
            .GetProperty("getProduct")
            .GetProperty("inStock")
            .GetBoolean();
        
        return inStock;
    }
}
```

**Why GraphQL?**
- Query only the fields you need (smaller response)
- Single request for multiple products (batch queries)
- Strongly typed schema

### UnifiStockLegacyService.cs (REST Fallback)

If GraphQL fails, falls back to REST API:

```csharp
public class UnifiStockLegacyService : IUnifiStockService
{
    public async Task<bool> IsInStockAsync(string productId, CancellationToken ct)
    {
        // Fallback to REST endpoint
        var response = await _httpClient.GetAsync(
            $"https://api.ui.com/products/{productId}",
            ct);
        
        // Parse response
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        
        return json
            .RootElement
            .GetProperty("availability")
            .GetString() == "IN_STOCK";
    }
}
```

### UnifiProduct Model

```csharp
public class UnifiProduct
{
    public string Id { get; set; }              // "UDM-SE"
    public string Name { get; set; }            // "UniFi Dream Machine SE"
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }        // "USD"
    public bool InStock { get; set; }
    public Uri ProductUrl { get; set; }
    public DateTime LastChecked { get; set; }
}
```

### Error Handling in Stock Checks

```csharp
// In StockWatcher.cs
try
{
    var inStock = await _unifiStockService.IsInStockAsync(productId, ct);
    
    if (inStock && !_previousState.ContainsKey(productId))
    {
        // Product went from out-of-stock to in-stock
        await SendNotificationAsync($"{productName} is now in stock!");
        _previousState[productId] = true;
    }
}
catch (HttpRequestException ex)
{
    // Network error - log and continue
    _logger.LogWarning($"Failed to check stock: {ex.Message}");
    // Retry on next cycle
}
catch (JsonException ex)
{
    // API response changed - log and continue
    _logger.LogError($"Failed to parse API response: {ex.Message}");
    // Continue with next product
}
```

### Key Points

- **Graceful Degradation**: GraphQL fails → use REST fallback
- **Error Recovery**: Network errors don't crash the service
- **State Tracking**: Remember previous state to detect changes
- **Rate Limiting**: Respect API rate limits, configurable check interval

---

## Notifications

Notifications are how UnifiWatch informs users about stock changes. Multiple channels can be used simultaneously.

### The Notification Architecture

```
[Product goes in stock]
           ▼
[NotificationService.SendAsync()]
           ▼
[NotificationOrchestrator.SendAsync()]
           ▼
        ┌──┴──┬──────────┬─────────────┐
        │     │          │             │
        ▼     ▼          ▼             ▼
    Desktop Email        SMS      (Any others)
    │       │            │
    └───────┼────────────┤
            │            │
            ▼            ▼
    SmtpEmailProvider  TwilioSmsProvider
        │
        └─→ SMTP Server (TLS encryption)
        │
        └─→ User's email
```

### INotificationProvider Interface

All notification providers implement this interface:

```csharp
public interface INotificationProvider
{
    // Send a notification
    Task<bool> SendAsync(
        string title,
        string message,
        Uri productUrl,
        CancellationToken ct);
    
    // Test if notifications are configured
    Task<bool> IsConfiguredAsync();
}
```

### NotificationOrchestrator

The orchestrator sends to all enabled channels simultaneously:

```csharp
public class NotificationOrchestrator : INotificationProvider
{
    private readonly List<INotificationProvider> _providers = [];
    
    public async Task<bool> SendAsync(string title, string message, Uri url, CancellationToken ct)
    {
        // De-duplication: Don't send duplicate notifications within 5 minutes
        var key = HashNotification(title, message);
        if (_recentNotifications.Contains(key))
            return false;  // Already sent recently
        
        // Send to all enabled providers in parallel
        var tasks = _providers
            .Where(p => p.IsConfiguredAsync().Result)
            .Select(p => p.SendAsync(title, message, url, ct))
            .ToList();
        
        var results = await Task.WhenAll(tasks);
        
        // Mark as sent
        _recentNotifications.Add(key);
        
        // Return true if at least one succeeded
        return results.Any(r => r);
    }
}
```

**Key idea**: Send to multiple channels in parallel, but if one fails, others still succeed.

### Desktop Notifications

Platform-specific native notifications:

#### Windows
```csharp
// Show Windows toast notification via PowerShell
// Uses Windows.UI.Notifications APIs through PowerShell to avoid NuGet package compatibility issues
var notification = new ToastNotification(xmlDocument);
ToastNotificationManager.CreateToastNotifier("UnifiWatch").Show(notification);
```

#### macOS
```csharp
// Use NSUserNotificationCenter or UNUserNotificationCenter
var notification = new NSUserNotification
{
    Title = title,
    InformativeText = message
};
```

#### Linux
```csharp
// Use D-Bus notifications (org.freedesktop.Notifications)
// Or use `notify-send` command for simple notifications
```

### Email Notifications

#### SmtpEmailProvider

```csharp
public class SmtpEmailProvider : IEmailProvider
{
    public async Task SendAsync(EmailNotification notification)
    {
        // 1. Create SMTP client
        using var client = new SmtpClient();
        
        // 2. Connect with TLS
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        
        // 3. Authenticate with credentials from secure storage
        var password = await _credentialProvider.RetrieveAsync("UnifiWatch:email-smtp");
        await client.AuthenticateAsync(username, password);
        
        // 4. Build email message
        var message = new MimeMessage
        {
            From = { new MailboxAddress("UnifiWatch", fromAddress) },
            To = { new MailboxAddress(recipientEmail) },
            Subject = $"✅ {notification.ProductName} is in stock!",
            Body = new TextPart("html")
            {
                Text = BuildHtmlTemplate(notification)
            }
        };
        
        // 5. Send
        await client.SendAsync(message);
        
        // 6. Disconnect
        await client.DisconnectAsync(true);
    }
}
```

**Security**:
- TLS encryption (SecureSocketOptions.StartTls)
- Credentials from secure storage, not hardcoded
- No credentials in logs
- No password in configuration file

#### HTML Email Template

```html
<html>
<body style="font-family: Arial, sans-serif;">
    <h2 style="color: #2ecc71;">✅ {ProductName} is now in stock!</h2>
    
    <p>
        <strong>Price:</strong> {Price} {Currency}<br/>
        <strong>Checked:</strong> {Timestamp}<br/>
        <strong>Store:</strong> {StoreName}
    </p>
    
    <p>
        <a href="{ProductUrl}" 
           style="background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;">
            View Product
        </a>
    </p>
    
    <hr/>
    
    <p style="font-size: 12px; color: #999;">
        Monitored by UnifiWatch. 
        <a href="app:settings">Manage notifications</a>
    </p>
</body>
</html>
```

### SMS Notifications

#### TwilioSmsProvider

```csharp
public class TwilioSmsProvider : ISmsSmsProvider
{
    private readonly TwilioClient _client;
    
    public async Task SendAsync(SmsNotification notification)
    {
        // 1. Validate phone number is in E.164 format
        //    Example: "+15551234567" (country code + number)
        if (!IsValidE164(notification.PhoneNumber))
        {
            _logger.LogError($"Invalid phone number format: {notification.PhoneNumber}");
            return false;
        }
        
        // 2. Truncate message to 160 characters (SMS limit)
        var message = TruncateMessage(notification.Message, 160);
        
        // 3. Send via Twilio API
        var result = await _client.Messages.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_twilioPhoneNumber),
            to: new Twilio.Types.PhoneNumber(notification.PhoneNumber));
        
        // 4. Log result
        _logger.LogInformation($"SMS sent: {result.Sid}");
        
        return result.Status != SmsStatus.Failed;
    }
}
```

**Phone Number Format**:
- Input: "+1-555-123-4567" or "5551234567" or "+15551234567"
- Normalized: "+15551234567" (E.164 format)
- Why E.164: International standard, unambiguous across countries

**Message Truncation**:
```csharp
// SMS limit is 160 characters (or 70 for Unicode)
private string TruncateMessage(string message, int maxLength)
{
    if (message.Length <= maxLength)
        return message;
    
    // Truncate at word boundary for readability
    var truncated = message.Substring(0, maxLength - 3);
    var lastSpace = truncated.LastIndexOf(' ');
    
    if (lastSpace > 0)
        truncated = truncated.Substring(0, lastSpace);
    
    return truncated + "...";
}
```

---

## Background Service

The background service (StockWatcher) runs continuously, periodically checking stock and sending notifications.

### StockWatcher Architecture

```csharp
public class StockWatcher : BackgroundService
{
    private readonly IConfigurationProvider _configProvider;
    private readonly IUnifiStockService _stockService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<StockWatcher> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UnifiWatch service starting...");
        
        try
        {
            // Load configuration
            var config = await _configProvider.LoadAsync(stoppingToken);
            
            // Main monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check stock
                    await CheckStockAsync(config, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during stock check: {ex.Message}");
                    // Continue - don't crash
                }
                
                // Wait before next check
                var interval = TimeSpan.FromSeconds(config.Service.CheckIntervalSeconds);
                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UnifiWatch service stopping...");
        }
    }
    
    private async Task CheckStockAsync(ServiceConfiguration config, CancellationToken ct)
    {
        // 1. Check if product is in stock
        var inStock = await _stockService.IsInStockAsync(
            config.ProductId,
            ct);
        
        // 2. Compare with previous state
        if (inStock && !_previousState.ContainsKey(config.ProductId))
        {
            // Transitioned from out-of-stock to in-stock
            _logger.LogInformation($"✅ {config.ProductName} is now in stock!");
            
            // 3. Send notifications to all channels
            await _notificationService.SendAsync(
                $"{config.ProductName} is in stock!",
                $"Price: {product.Price}",
                product.ProductUrl,
                ct);
        }
        
        // 4. Update state
        _previousState[config.ProductId] = inStock;
    }
}
```

### Configuration File Watching

The service reloads configuration without restarting:

```csharp
private void WatchConfigurationFile()
{
    var configPath = _configProvider.GetConfigPath();
    var directory = Path.GetDirectoryName(configPath);
    var fileName = Path.GetFileName(configPath);
    
    var watcher = new FileSystemWatcher(directory)
    {
        Filter = fileName,
        NotifyFilter = NotifyFilters.LastWrite
    };
    
    watcher.Changed += async (s, e) =>
    {
        // Small delay to ensure file write is complete
        await Task.Delay(100);
        
        try
        {
            // Reload configuration
            var newConfig = await _configProvider.LoadAsync(CancellationToken.None);
            _logger.LogInformation("Configuration reloaded");
            // Rest of service uses updated _config
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to reload configuration: {ex.Message}");
            // Keep using old configuration
        }
    };
    
    watcher.EnableRaisingEvents = true;
}
```

### Service Lifecycle on Windows

```
User runs: UnifiWatch.exe --install-service
                   ▼
Service Control Manager registers service
                   ▼
Service properties:
  - Name: UnifiWatch
  - Display Name: UnifiWatch Stock Monitor
  - Start Type: Automatic
  - Binary Path: C:\path\to\UnifiWatch.exe
                   ▼
Windows starts service automatically on boot
                   ▼
Program.cs runs CreateDefaultBuilder().RunAsService()
                   ▼
StockWatcher.ExecuteAsync() starts
                   ▼
Monitoring loop runs continuously
                   ▼
Event Viewer logs all actions
```

### Service Lifecycle on Linux (systemd)

```
User runs: sudo ./UnifiWatch --install-service
                   ▼
Creates /etc/systemd/system/unifiwatch.service
  [Unit]
  Description=UnifiWatch Stock Monitor
  After=network.target
  
  [Service]
  Type=simple
  ExecStart=/opt/unifiwatch/UnifiWatch
  Restart=on-failure
  
  [Install]
  WantedBy=multi-user.target
                   ▼
systemctl enable unifiwatch
                   ▼
systemctl start unifiwatch
                   ▼
Service runs immediately and on boot
                   ▼
journalctl -u unifiwatch shows logs
```

### Service Lifecycle on macOS (launchd)

```
User runs: ./UnifiWatch --install-service
                   ▼
Creates ~/Library/LaunchAgents/com.unifiwatch.plist
  <?xml version="1.0"?>
  <!DOCTYPE plist PUBLIC ... >
  <plist version="1.0">
  <dict>
    <key>Label</key>
    <string>com.unifiwatch</string>
    <key>ProgramArguments</key>
    <array>
      <string>/path/to/UnifiWatch</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
  </dict>
  </plist>
                   ▼
launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
                   ▼
Service runs immediately and on login
                   ▼
Console.app shows logs
```

---

## Dependency Injection

UnifiWatch uses .NET's built-in DI container for clean, testable code.

### What is Dependency Injection?

Instead of creating dependencies inside a class:

```csharp
// BAD - tightly coupled, hard to test
public class StockWatcher
{
    private readonly IUnifiStockService _stockService = new UnifiStockService();
    // Now StockWatcher always uses the real UnifiStockService
    // Can't substitute a mock for testing
}
```

We pass them in:

```csharp
// GOOD - loosely coupled, testable
public class StockWatcher
{
    private readonly IUnifiStockService _stockService;
    
    public StockWatcher(IUnifiStockService stockService)
    {
        _stockService = stockService;
    }
    
    // Can now use any IUnifiStockService implementation
    // Including mocks in tests
}
```

### DI Setup in Program.cs

```csharp
var builder = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Configuration
        services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
        
        // Credentials
        services.AddSingleton<ICredentialProvider>(
            sp => CredentialProviderFactory.CreateProvider()
        );
        
        // Stock APIs
        services.AddHttpClient<IUnifiStockService, UnifiStockService>();
        services.AddHttpClient<IUnifiStockLegacyService, UnifiStockLegacyService>();
        
        // Notifications
        services.AddSingleton<INotificationProvider, NotificationOrchestrator>();
        services.AddSingleton<DesktopNotificationService>();
        services.AddSingleton<EmailNotificationService>();
        services.AddSingleton<SmsNotificationService>();
        
        // Background service
        services.AddHostedService<StockWatcher>();
        
        // Logging
        services.AddLogging();
    });
```

### Lifetime Scopes

Different services have different lifetimes:

| Lifetime | When Created | When Destroyed | Use Case |
|----------|-------------|----------------|----------|
| **Singleton** | Once on first use | Process shutdown | Configuration, credentials, logging |
| **Transient** | Every request | Immediately after use | Stateless utilities |
| **Scoped** | Per request/scope | Scope ends | (Rarely used in services) |

**Example**:
```csharp
// Created once, same instance throughout application
services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();

// Created fresh each time requested
services.AddTransient<SomeUtility>();

// Created once per HTTP request (MVC/Web API)
services.AddScoped<SomeService>();
```

### Getting Services

**In Program.cs or service constructors**:
```csharp
// Constructor injection (automatic via DI)
public class MyService
{
    private readonly IConfigurationProvider _configProvider;
    
    public MyService(IConfigurationProvider configProvider)
    {
        _configProvider = configProvider;  // Auto-injected by DI container
    }
}

// Manual service location (generally avoided)
var provider = services.BuildServiceProvider();
var config = provider.GetRequiredService<IConfigurationProvider>();
```

---

## Localization

UnifiWatch supports multiple languages through the IStringLocalizer interface.

### i18n Philosophy

Write code that's easy to localize later:

```csharp
// GOOD - easy to extract strings
var message = LocalizeString("product-in-stock", productName);
Console.WriteLine(message);

// BAD - hard to extract from code
var message = $"Product {productName} is in stock!";
Console.WriteLine(message);
```

### Resource Files

English strings are stored in JSON files:

```json
// Resources/CLI.en-US.json
{
    "product-in-stock": "Product {0} is in stock!",
    "check-interval-prompt": "Check interval (seconds)",
    "configure-email": "Configure email notifications?",
    "error-invalid-email": "Invalid email address"
}

// Resources/Errors.en-US.json
{
    "credential-not-found": "Credential '{0}' not found",
    "config-file-corrupted": "Configuration file is corrupted"
}
```

### ResourceLocalizer

Custom implementation of IStringLocalizer:

```csharp
public class ResourceLocalizer : IStringLocalizer
{
    private readonly Dictionary<string, string> _strings = [];
    
    public LocalizedString this[string name]
    {
        get
        {
            if (_strings.TryGetValue(name, out var value))
                return new LocalizedString(name, value, true);
            
            // Fallback to key itself if not found
            return new LocalizedString(name, name, false);
        }
    }
    
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localizedString = this[name];
            return new LocalizedString(
                name,
                string.Format(localizedString.Value, arguments),
                localizedString.ResourceNotFound);
        }
    }
    
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _strings
        .Select(kvp => new LocalizedString(kvp.Key, kvp.Value, true));
}
```

### Using Localization

```csharp
public class ConfigurationWizard
{
    private readonly IStringLocalizer _localizer;
    
    public ConfigurationWizard(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }
    
    public void PromptForEmailAddress()
    {
        // Displays "Configure email notifications?" in user's language
        Console.WriteLine(_localizer["configure-email"]);
    }
    
    public void NotifyProductInStock(string productName)
    {
        // Displays "Product UDM-SE is in stock!" with correct localization
        var message = _localizer["product-in-stock", productName];
        Console.WriteLine(message);
    }
}
```

### Adding a New Language

1. Create resource file: `Resources/CLI.pt-BR.json`
2. Translate all strings
3. Update ResourceLocalizer to load the new language
4. Update localization factory to support new locale

---

## Testing

UnifiWatch has comprehensive test coverage (146 tests) using xUnit and Moq.

### Testing Philosophy

Tests verify behavior, not implementation:

```csharp
// GOOD - tests behavior
[Fact]
public async Task IsInStockAsync_WhenProductAvailable_ReturnsTrue()
{
    // Arrange
    var service = new UnifiStockService(mockHttpClient, mockLogger);
    var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{...in-stock JSON...}")
    };
    mockHttpClient.Setup(h => h.GetAsync(...))
        .ReturnsAsync(mockResponse);
    
    // Act
    var result = await service.IsInStockAsync("UDM-SE", CancellationToken.None);
    
    // Assert
    Assert.True(result);
}

// BAD - tests implementation details
[Fact]
public void IsInStockAsync_CallsHttpClient()
{
    // Just verifies that HttpClient.GetAsync was called
    // Doesn't test that it actually returns the right result
}
```

### Test Structure: Arrange-Act-Assert

```csharp
[Fact]
public async Task SendAsync_WithValidConfiguration_SendsEmail()
{
    // ARRANGE: Set up test data and mocks
    var config = new ServiceConfiguration
    {
        Notifications = new NotificationSettings
        {
            Email = new EmailSettings
            {
                Enabled = true,
                SmtpServer = "smtp.test.com",
                Port = 587
            }
        }
    };
    
    var mockSmtp = new Mock<ISmtpClient>();
    mockSmtp.Setup(s => s.SendAsync(It.IsAny<MimeMessage>()))
        .ReturnsAsync(true);
    
    var service = new SmtpEmailProvider(config, mockCredentialProvider, mockSmtp.Object);
    
    // ACT: Perform the action being tested
    var result = await service.SendAsync(
        "Test Product is in stock",
        "Price: $100",
        new Uri("https://example.com/product"),
        CancellationToken.None);
    
    // ASSERT: Verify the result
    Assert.True(result);
    mockSmtp.Verify(s => s.SendAsync(It.IsAny<MimeMessage>()), Times.Once);
}
```

### Mocking Dependencies

Use Moq library for mocking:

```csharp
// Create a mock of ICredentialProvider
var mockCredentialProvider = new Mock<ICredentialProvider>();

// Set up behavior
mockCredentialProvider
    .Setup(cp => cp.RetrieveAsync("password-key", It.IsAny<CancellationToken>()))
    .ReturnsAsync("test-password");

// Use in test
var service = new SmtpEmailProvider(
    config,
    mockCredentialProvider.Object,  // Pass mock to service
    logger);

// Verify it was called
mockCredentialProvider.Verify(
    cp => cp.RetrieveAsync("password-key", It.IsAny<CancellationToken>()),
    Times.Once);
```

### Running Tests

For hands-on testing instructions including unit tests, Phase 1 validation tests, and platform-specific verification, see:

- **Windows**: [WINDOWS_DEVELOPER_GUIDE.md - Testing](WINDOWS_DEVELOPER_GUIDE.md#testing)
- **Linux**: [LINUX_DEVELOPER_GUIDE.md - Testing](LINUX_DEVELOPER_GUIDE.md#testing)
- **macOS**: [MACOS_DEVELOPER_GUIDE.md - Testing](MACOS_DEVELOPER_GUIDE.md#testing)

### Key Test Categories

| Category | Tests | Purpose |
|----------|-------|---------|
| **Configuration** | 19 | Load/save/validate config, backup |
| **Credentials** | 46 | Store/retrieve secrets securely |
| **Notifications** | 53 | Desktop, email, SMS, orchestration |
| **Stock APIs** | 18 | GraphQL and REST API parsing |
| **Localization** | 10 | String loading and fallbacks |
| **Total** | **146** | **Comprehensive coverage** |

---

## Common Workflows

### Workflow 1: Configure the Application

User wants to set up UnifiWatch for their environment.

```bash
# Run configuration wizard
./UnifiWatch --configure

# This starts ConfigurationWizard.cs which:
# 1. Prompts for product ID
# 2. Prompts for check interval
# 3. Asks to enable email/SMS
# 4. If email: prompts for SMTP settings
# 5. Prompts for credentials → stores securely
# 6. Validates configuration
# 7. Saves to platform-specific path
```

**Code flow**:
```
Program.cs (--configure)
  ↓
ConfigurationWizard (interactive prompts)
  ↓
Prompt for SMTP password
  ↓
CredentialProvider.StoreAsync() (secure storage)
  ↓
ConfigurationProvider.SaveAsync() (JSON file)
  ↓
"Configuration saved!"
```

### Workflow 2: Start Background Service

User installs the service so it runs continuously.

```bash
# Install service (Windows)
UnifiWatch.exe --install-service

# Install service (Linux)
sudo ./UnifiWatch --install-service

# Install service (macOS)
./UnifiWatch --install-service
```

**What happens**:
1. Program detects platform
2. Registers service with OS
3. Sets configuration to auto-start
4. Service starts immediately
5. Runs StockWatcher.ExecuteAsync()

**On boot**:
1. OS starts service automatically
2. StockWatcher loads configuration
3. Enters monitoring loop
4. Every N seconds: check stock → send notifications if changed

### Workflow 3: Send Notification

Product goes in stock, notification is sent.

```
[StockWatcher checks stock via UnifiStockService]
            ↓
         [InStock = true]
            ↓
     [Not in previous state]
            ↓
[Notification.SendAsync()]
            ↓
    [NotificationOrchestrator]
            ↓
        ┌───┴───┬────────┬──────────┐
        ↓       ↓        ↓          ↓
    Desktop  Email     SMS      (Others)
        │       │        │
        ├───────┼────────┤
        ↓       ↓        ↓
    Toast    SMTP    Twilio API
   Message   TLS     PhoneAPI
        │       │        │
        └───────┼────────┘
                ↓
    [All channels complete]
                ↓
        [Log "Sent to 3/3"]
```

### Workflow 4: Reload Configuration

Configuration file changes, service picks up changes without restart.

```bash
# User edits configuration
nano ~/.config/unifiwatch/config.json
# Change check interval from 60 to 120 seconds

# FileSystemWatcher detects change
  ↓
# StockWatcher.OnConfigurationChanged()
  ↓
# Reload configuration
  ↓
# Update internal state
  ↓
# Continue with new check interval (120 seconds)
```

**No service restart needed!**

### Workflow 5: Handle Error Gracefully

Network is unavailable, service continues running.

```
[StockWatcher.CheckStockAsync()]
        ↓
[Call UnifiStockService.IsInStockAsync()]
        ↓
[HttpClient.GetAsync() throws HttpRequestException]
        ↓
[Catch exception in StockWatcher]
        ↓
[Log error: "Failed to check stock: Connection timeout"]
        ↓
[Continue - wait for next check cycle]
        ↓
[Try again in 60 seconds]
        ↓
[Network is back - success!]
```

**Key principle**: One failed check doesn't crash the service.

---

## Summary: The Big Picture

UnifiWatch works as follows:

1. **Configuration**: User runs `--configure`, providing settings (product ID, email, SMS)
   - Credentials stored securely (Windows Credential Manager, macOS Keychain, Linux encrypted file)
   - Configuration saved as JSON in platform-specific path

2. **Service Startup**: User runs `--install-service`, OS registers background service
   - On Windows: Service Control Manager
   - On Linux: systemd
   - On macOS: launchd

3. **Monitoring Loop**: Service runs continuously
   - Every N seconds: Check product stock via Ubiquiti API
   - Compare with previous state
   - If changed: Send notifications to all configured channels

4. **Notifications**: Multi-channel delivery
   - Desktop: Native toast/alerts
   - Email: SMTP with TLS encryption
   - SMS: Twilio API with international support
   - Channels run in parallel, one failure doesn't affect others

5. **Configuration Hot-Reload**: User can update config without restarting service
   - FileSystemWatcher detects changes
   - Configuration reloaded
   - Service continues with new settings

6. **Error Recovery**: Graceful error handling throughout
   - Network errors logged but don't crash service
   - Invalid configuration falls back to defaults
   - Missing credentials show clear error messages

---

## Tips for Contributing

### Code Style
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Use async/await consistently
- Avoid async void (use Task instead)

### Adding a Feature
1. Write unit test first (TDD)
2. Implement feature to pass test
3. Add documentation
4. Test on all platforms

### Adding a New Notification Provider
1. Implement `INotificationProvider` interface
2. Add to `NotificationOrchestrator`
3. Add configuration options
4. Add unit tests
5. Document setup instructions

### Debugging
```csharp
// Enable detailed logging
services.AddLogging(builder =>
    builder.AddConsole()
           .SetMinimumLevel(LogLevel.Debug));

// Use logger throughout code
_logger.LogDebug("Product: {productId}", productId);
_logger.LogInformation("Notification sent");
_logger.LogError("Error occurred: {error}", ex.Message);
```

---

## Further Reading

- **Architecture**: See SERVICE_ARCHITECTURE.md
- **Setup**: See SERVICE_SETUP.md
- **Security**: See SECURITY.md
- **Testing**: See WINDOWS/LINUX/MACOS_END_USER_GUIDE.md
- **Development Plan**: See BUILD_PLAN.md

**Happy coding!**


