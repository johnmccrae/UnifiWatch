using UnifiStockTracker.Services;

// Test notification
Console.WriteLine("Sending test notification...");
NotificationService.ShowNotification(
    "UniFi Stock Alert - TEST", 
    "Dream Machine Pro is now in stock! Click to view.");

Console.WriteLine("Notification sent! Check your Action Center (Windows) or notification area.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
