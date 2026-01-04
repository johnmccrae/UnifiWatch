# Get-Office365OAuthToken.ps1
# Obtains an OAuth token from Office 365/Microsoft Entra ID for use with UnifiWatch
# This token can be stored as the "password" for SMTP authentication

Write-Host "Office 365 OAuth Token Acquisition" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host ""

Write-Host "This script will help you get an OAuth access token from Office 365." -ForegroundColor Yellow
Write-Host "You will need to register an application in Azure AD first." -ForegroundColor Yellow
Write-Host ""

# Check if we have the required module
if (-not (Get-Module -ListAvailable -Name Az.Accounts)) {
    Write-Host "Installing required Azure PowerShell module..." -ForegroundColor Yellow
    Install-Module -Name Az.Accounts -Force -AllowClobber
}

Write-Host ""
Write-Host "Step 1: Azure AD Application Registration" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You need to register an application in Azure AD to get OAuth credentials:" -ForegroundColor White
Write-Host ""
Write-Host "1. Go to https://portal.azure.com" -ForegroundColor Gray
Write-Host "2. Search for 'App registrations'" -ForegroundColor Gray
Write-Host "3. Click 'New registration'" -ForegroundColor Gray
Write-Host "4. Name it 'UnifiWatch'" -ForegroundColor Gray
Write-Host "5. Set 'Supported account types' to 'Accounts in this organizational directory only'" -ForegroundColor Gray
Write-Host "6. Click 'Register'" -ForegroundColor Gray
Write-Host ""
Write-Host "Then add API permissions:" -ForegroundColor White
Write-Host "1. Go to 'API permissions'" -ForegroundColor Gray
Write-Host "2. Click 'Add a permission'" -ForegroundColor Gray
Write-Host "3. Select 'Microsoft Graph'" -ForegroundColor Gray
Write-Host "4. Select 'Delegated permissions'" -ForegroundColor Gray
Write-Host "5. Search for and select: 'Mail.Send'" -ForegroundColor Gray
Write-Host "6. Click 'Add permissions'" -ForegroundColor Gray
Write-Host ""
Write-Host "Then create a client secret:" -ForegroundColor White
Write-Host "1. Go to 'Certificates & secrets'" -ForegroundColor Gray
Write-Host "2. Click 'New client secret'" -ForegroundColor Gray
Write-Host "3. Add description 'UnifiWatch SMTP'" -ForegroundColor Gray
Write-Host "4. Set expiration (1 year recommended)" -ForegroundColor Gray
Write-Host "5. Click 'Add'" -ForegroundColor Gray
Write-Host "6. COPY the secret value (you can only see it once!)" -ForegroundColor Gray
Write-Host ""

Write-Host "Step 2: Enter Your Application Details" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$clientId = Read-Host "Application (client) ID"
$clientSecret = Read-Host "Client secret value" -AsSecureString
$tenantId = Read-Host "Directory (tenant) ID"
$userEmail = Read-Host "Your Office 365 email address"

Write-Host ""
Write-Host "Acquiring OAuth token..." -ForegroundColor Yellow
Write-Host ""

try {
    # Convert secure string to plain text
    $clientSecretPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecret)
    )

    # Request token
    $tokenUrl = "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token"
    
    $body = @{
        client_id     = $clientId
        client_secret = $clientSecretPlain
        scope         = "https://graph.microsoft.com/.default"
        grant_type    = "client_credentials"
    }

    $response = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
    
    if ($response.access_token) {
        Write-Host "✓ OAuth token acquired successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Token Details:" -ForegroundColor Cyan
        Write-Host "  Expires in: $($response.expires_in) seconds (~$(([Math]::Round($response.expires_in/3600))) hours)" -ForegroundColor White
        Write-Host "  Type: Bearer" -ForegroundColor White
        Write-Host ""
        
        Write-Host "Next Steps:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "1. Store this token in Windows Credential Manager:" -ForegroundColor White
        Write-Host ""
        Write-Host "   `$secureToken = ConvertTo-SecureString '$($response.access_token)' -AsPlainText -Force" -ForegroundColor Yellow
        Write-Host "   `$credential = New-Object System.Management.Automation.PSCredential('oauth2@office365', `$secureToken)" -ForegroundColor Yellow
        Write-Host "   Get-StoredCredential -Target 'UnifiWatch:office365-email' -Credential `$credential -Persist LocalMachine" -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "2. In UnifiWatch config.json, set:" -ForegroundColor White
        Write-Host "   - SMTP Server: smtp.office365.com" -ForegroundColor Yellow
        Write-Host "   - SMTP Port: 587" -ForegroundColor Yellow
        Write-Host "   - Use TLS: true" -ForegroundColor Yellow
        Write-Host "   - From Address: $userEmail" -ForegroundColor Yellow
        Write-Host "   - Credential Key: office365-email" -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "3. Update credential storage to use the token:" -ForegroundColor White
        Write-Host "   Store it as: oauth2@office365 / <access_token>" -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "IMPORTANT: Tokens expire after ~1 hour. You'll need to refresh periodically." -ForegroundColor Red
        Write-Host "Consider using a refresh token flow or the Microsoft Graph API instead." -ForegroundColor Red
        
    } else {
        Write-Host "✗ Failed to acquire token" -ForegroundColor Red
        Write-Host ""
        Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}
