# Windows Testing Guide

This guide covers testing the UnifiWatch application on Windows.

## Prerequisites

- Windows 10 or later
- .NET 9.0 SDK or later
- PowerShell or Command Prompt

## Quick Test

Run all tests:

```powershell
dotnet test
```

## Test Results Summary

**Last tested:** January 15, 2026
**Platform:** Windows
**SDK Version:** .NET 9.0

### Test Statistics
- **Total Tests:** 223
- **Passed:** 212 ✅
- **Failed:** 0 ❌
- **Skipped:** 11 (Integration/advanced tests)

### Skipped Tests
The following tests are skipped as they require real HTTP services or are deferred:
- Integration tests for email/SMS notifications (Phase 2+)
- End-to-end stock check workflow tests (Phase 2+)
- Localization resource validation (deferred to Phase 2b)
- Service lifecycle integration tests (Phase 5+)

## Platform-Specific Notes

### Credential Storage
On Windows, the application uses **Windows Credential Manager (DPAPI)** for secure credential storage. This provides:
- Native OS-level encryption
- Automatic key management by Windows
- No manual machine ID setup required
- Integration with Windows security policies

### Testing Credential Provider
The credential provider tests validate DPAPI encryption on Windows:

```powershell
dotnet test --filter CredentialProviderTests
```

Expected output: All credential provider tests should pass (46+ tests).

### File Paths
Windows uses different path conventions:
- Configuration directory: `%APPDATA%\UnifiStock`
- Credentials file: `%APPDATA%\UnifiStock\credentials.enc.json`

## Running Tests with Coverage

Generate test coverage report:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Troubleshooting

### Issue: "Access Denied" errors
**Solution:** Run PowerShell as Administrator if testing credential storage in system directories.

### Issue: Tests fail with path errors
**Solution:** Ensure paths use Windows path separators (`\`) and that the application correctly detects Windows as the platform.

### Issue: DPAPI encryption fails
**Solution:** 
- Verify your Windows user account is properly configured
- Check that Windows Credential Manager service is running
- Ensure your user profile is not corrupted

### Issue: Port conflicts when testing
**Solution:** Stop any running instances of the application or other services using the same ports.

## Clean Test Environment

Remove test artifacts:

```powershell
Remove-Item -Recurse -Force bin, obj
Remove-Item -Recurse -Force UnifiWatch.Tests\bin, UnifiWatch.Tests\obj
Remove-Item -Recurse -Force $env:APPDATA\UnifiWatch
```

## Running Specific Test Categories

Run only unit tests:
```powershell
dotnet test --filter Category=Unit
```

Run only integration tests (requires network):
```powershell
dotnet test --filter Category=Integration
```

## Performance Testing

Run tests with detailed timing:

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## Security Considerations

### DPAPI Encryption
- Credentials are encrypted using Windows DPAPI
- Keys are tied to your Windows user account
- Credentials cannot be decrypted by other users
- If the user profile is moved to another machine, credentials must be re-entered

### Testing Security Features
To verify DPAPI encryption is working:

```powershell
# Run credential tests
dotnet test --filter "FullyQualifiedName~CredentialProviderTests"

# Verify encrypted file exists
Test-Path $env:APPDATA\UnifiStock\credentials.enc.json

# Verify file is encrypted (not plaintext)
Get-Content $env:APPDATA\UnifiStock\credentials.enc.json
```

The content should be binary/encrypted data, not readable JSON.

## CI/CD Integration

For Windows-based CI/CD pipelines (Azure DevOps, GitHub Actions Windows runners):

```yaml
# Example GitHub Actions workflow
- name: Run tests on Windows
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
  
- name: Publish test results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Windows Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
```

## Next Steps

After verifying all tests pass on Windows:
1. Test on macOS (see [MACOS_TESTING.md](MACOS_TESTING.md))
2. Test on Linux (see [MACOS_BUILD_AND_TEST.md](MACOS_BUILD_AND_TEST.md))
3. Review the build plan (see [BUILD_PLAN.md](BUILD_PLAN.md))

## Known Platform Differences

- **Line Endings:** Tests handle both CRLF (Windows) and LF (Unix) line endings
- **Case Sensitivity:** Windows file system is case-insensitive; tests account for this
- **Path Separators:** Code uses `Path.Combine()` for cross-platform compatibility
- **Credential Storage:** DPAPI on Windows vs file-based encryption on Unix

## Support

For issues specific to Windows:
- Check Windows Event Viewer for application errors
- Verify .NET SDK installation: `dotnet --info`
- Ensure Windows Defender or antivirus isn't blocking the application
