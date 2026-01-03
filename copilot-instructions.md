# Copilot Instructions for UnifiWatch Project

## 🚨 CRITICAL: Terminal Commands on Windows Node 🚨

**⚠️ THIS WORKSPACE RUNS ON WINDOWS WITH POWERSHELL. DO NOT USE LINUX/UNIX COMMANDS.**

**IF YOU SEE A COMMAND EXECUTION ERROR MENTIONING A COMMAND IS "NOT RECOGNIZED", YOU TRIED A LINUX COMMAND. STOP AND USE POWERSHELL INSTEAD.**

### ❌ FORBIDDEN Commands (Will Fail on Windows)
- `tail`, `head`, `grep`, `sed`, `awk`, `cut`, `cat`, `ls`, `find`, `wc`, `chmod`, etc.
- **These WILL fail with "command not recognized" errors**
- **Never pipe to these commands, never use them in chains**

### ✅ USE ONLY PowerShell Equivalents
- `Select-Object -Last 50` (instead of `tail -n 50`)
- `Select-Object -First 50` (instead of `head -n 50`)
- `Select-String -Pattern "text"` (instead of `grep`)
- `Get-Content` (instead of `cat`)
- `Get-ChildItem` (instead of `ls`)

### 🔴 ABSOLUTELY CRITICAL REMINDER
**When filtering command output on Windows, ALWAYS use PowerShell syntax:**
- ❌ `dotnet publish | tail -5` → **WILL FAIL**
- ✅ `dotnet publish | Select-Object -Last 5` → **CORRECT**
- ❌ `command | head -20` → **WILL FAIL**
- ✅ `command | Select-Object -First 20` → **CORRECT**

---

## Operating System and Shell Awareness

### Critical: Always Check Environment Info

Before executing any terminal commands, verify the current operating system and shell from the environment context:
- **Current OS:** Windows
- **Current Shell:** `pwsh.exe` (PowerShell Core)
- This information is provided in `<environment_info>` tags

### Windows PowerShell vs Unix-like Commands

**DO NOT** use Unix/Linux commands on Windows PowerShell:
- ❌ `tail -n 50 file.txt` → Will fail on Windows
- ❌ `grep "pattern" file.txt` → Use `Select-String` instead
- ❌ `cat file.txt | head -20` → Not available on PowerShell
- ❌ `ls -la` → Use `Get-ChildItem` or `dir` instead

### Correct Windows PowerShell Alternatives

| Unix Command | PowerShell Equivalent | Example |
|--------------|----------------------|---------|
| `tail -n 50` | `Select-Object -Last 50` | `Get-Content file.txt \| Select-Object -Last 50` |
| `head -n 50` | `Select-Object -First 50` | `dotnet test \| Select-Object -First 50` |
| `grep "pattern"` | `Select-String -Pattern` | `Get-Content file.txt \| Select-String "pattern"` |
| `cat file.txt` | `Get-Content file.txt` | `Get-Content file.txt` |
| `ls` | `Get-ChildItem` or `dir` | `Get-ChildItem` |
| `find . -name "*.cs"` | `Get-ChildItem -Filter -Recurse` | `Get-ChildItem -Filter "*.cs" -Recurse` |
| `wc -l file.txt` | `@(Get-Content file.txt).Count` | `@(Get-Content file.txt).Count` |

### PowerShell-Specific Best Practices

1. **Pipeline Output Handling:**
   - Use `Select-Object` for filtering and selecting properties
   - Use `Select-String` for pattern matching
   - Use `Where-Object` for conditional filtering

2. **Command Chaining:**
   - PowerShell uses `;` or newlines to chain commands
   - Pipes (`\|`) pass objects between cmdlets
   - Example: `dotnet test; if ($LASTEXITCODE -eq 0) { Write-Host "Success" }`

3. **String Output from Tools:**
   - Tools like `dotnet` output strings, not PowerShell objects
   - To filter output: `dotnet test 2>&1 | Select-String "FAIL"`
   - To get last N lines: `dotnet test 2>&1 | Select-Object -Last 50`

4. **Avoiding Sub-shells:**
   - Don't use `powershell -c "command"` - execute directly
   - Don't nest PowerShell calls unnecessarily

### Practical Examples for This Project

**Example 1: Running tests and viewing failures**
```powershell
# ✅ CORRECT
dotnet test 2>&1 | Select-String "FAIL" -Context 0,5

# ❌ WRONG
dotnet test 2>&1 | grep "FAIL"
```

**Example 2: Getting test summary**
```powershell
# ✅ CORRECT
dotnet test 2>&1 | Select-Object -Last 20

# ❌ WRONG
dotnet test 2>&1 | tail -20
```

**Example 3: Building and running tests**
```powershell
# ✅ CORRECT
cd c:\localrepo\UnifiWatch; dotnet build; dotnet test

# ❌ WRONG
cd /localrepo/UnifiWatch && dotnet build && dotnet test
```

**Example 4: Finding C# test files**
```powershell
# ✅ CORRECT
Get-ChildItem -Filter "*Tests.cs" -Recurse

# ❌ WRONG
find . -name "*Tests.cs"
```

## Project-Specific Notes

### Test Execution
- Always use `dotnet test` directly - no shell alternatives needed
- For output filtering, use PowerShell's `Select-String` or `Select-Object`
- Test output redirection: `2>&1` works on PowerShell to capture both stdout and stderr

### File Operations
- Use `Get-Content` and `Set-Content` instead of Unix tools
- File paths can use forward slashes `/` on Windows PowerShell (it's compatible)
- Absolute paths should use drive letters: `C:\path\to\file`

### Environment Verification
- Always refer to the `<environment_info>` section at the top of each task
- If OS changes to Linux/macOS, switch to appropriate Unix commands
- Current setup: **Windows + PowerShell (pwsh.exe)**

## Summary

✅ **Golden Rule:** If you're unsure about a command on Windows PowerShell, ask yourself:
1. Is this a standard Unix command?
2. Am I on Windows with PowerShell?
3. If yes to both: Use the PowerShell equivalent instead

This ensures efficient, correct command execution without failures or workarounds.


