# Test-GraphApiEmail.ps1
# Tests Microsoft Graph API authentication and email sending

$TenantId = Read-Host "Tenant ID"
$ClientId = Read-Host "Client ID"
$ClientSecret = Read-Host "Client Secret" -AsSecureString
$MailboxEmail = Read-Host "Mailbox email address (the account sending email)"
$RecipientEmail = Read-Host "Recipient email (where to send test email)"

Write-Host ""
Write-Host "Testing Microsoft Graph API Email..." -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host ""

$clientSecretPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecret)
)

# Step 1: Get access token
Write-Host "Step 1: Authenticating..." -ForegroundColor Yellow
try {
    $tokenBody = @{
        client_id     = $ClientId
        client_secret = $clientSecretPlain
        scope         = "https://graph.microsoft.com/.default"
        grant_type    = "client_credentials"
    }

    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
        -Method Post -Body $tokenBody -ContentType "application/x-www-form-urlencoded"
    
    $accessToken = $tokenResponse.access_token
    Write-Host "✓ Authentication successful!" -ForegroundColor Green
    Write-Host "  Token acquired (expires in $($tokenResponse.expires_in) seconds)" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Authentication failed!" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "  Details: $errorBody" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host ""

# Step 2: Send test email via Graph API
Write-Host "Step 2: Sending test email..." -ForegroundColor Yellow
try {
    $mailBody = @{
        message = @{
            subject = "UnifiWatch Graph API Test - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            body = @{
                contentType = "HTML"
                content = @"
<html>
<body>
<h2>UnifiWatch Email Test</h2>
<p>This email was sent via Microsoft Graph API.</p>
<p><strong>Test Details:</strong></p>
<ul>
  <li>Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</li>
  <li>From: $MailboxEmail</li>
  <li>Mailbox: $MailboxEmail</li>
</ul>
<p>If you received this email, your Graph API email configuration is working!</p>
</body>
</html>
"@
            }
            toRecipients = @(
                @{
                    emailAddress = @{
                        address = $RecipientEmail
                    }
                }
            )
        }
        saveToSentItems = "true"
    }

    $headers = @{
        "Authorization" = "Bearer $accessToken"
        "Content-Type" = "application/json"
    }

    $graphUrl = "https://graph.microsoft.com/v1.0/users/$MailboxEmail/sendMail"
    
    Write-Host "  Endpoint: $graphUrl" -ForegroundColor Gray
    Write-Host "  Recipient: $RecipientEmail" -ForegroundColor Gray
    
    $emailResponse = Invoke-RestMethod -Uri $graphUrl `
        -Method Post `
        -Headers $headers `
        -Body ($mailBody | ConvertTo-Json -Depth 10)
    
    Write-Host "✓ Email sent successfully!" -ForegroundColor Green
    Write-Host "  Status: 202 Accepted" -ForegroundColor Green
    
    
} catch {
    Write-Host "✗ Email send failed!" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        
        Write-Host ""
        Write-Host "  Response Details:" -ForegroundColor Yellow
        Write-Host "$errorBody" -ForegroundColor Yellow
        
        # Parse and display common errors
        if ($errorBody -match "Authorization_RequestDenied") {
            Write-Host ""
            Write-Host "  💡 Common fix: Make sure your app has 'Mail.Send' permission in Azure AD" -ForegroundColor Cyan
        }
        elseif ($errorBody -match "ResourceNotFound") {
            Write-Host ""
            Write-Host "  💡 Common fix: Check that '$MailboxEmail' is a valid mailbox" -ForegroundColor Cyan
        }
        elseif ($errorBody -match "Authorization_RequestDenied") {
            Write-Host ""
            Write-Host "  💡 Common fix: Check app permissions and ensure admin consent is granted" -ForegroundColor Cyan
        }
    }
    exit 1
}

Write-Host ""
Write-Host "✓ All tests passed!" -ForegroundColor Green
Write-Host ""
Write-Host "Your Graph API configuration is working correctly." -ForegroundColor Green
Write-Host "You can now configure UnifiWatch to use Graph API for email." -ForegroundColor Green
