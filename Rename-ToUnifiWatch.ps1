# Rename UnifiStockTracker to UnifiWatch
# This script performs a comprehensive rename across the entire codebase

param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Renaming UnifiStockTracker to UnifiWatch" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($WhatIf) {
    Write-Host "Running in WhatIf mode - no changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Rename file contents (text replacements)
Write-Host "Step 1: Updating file contents..." -ForegroundColor Green

$replacements = @(
    @{ Old = "UnifiStockTracker-CSharp"; New = "UnifiWatch" }
    @{ Old = "UnifiStockTracker.Tests"; New = "UnifiWatch.Tests" }
    @{ Old = "UnifiStockTracker"; New = "UnifiWatch" }
    @{ Old = "unifistocktracker"; New = "unifiwatch" }
    @{ Old = "unifistock"; New = "unifiwatch" }
)

# Files to update
$filesToUpdate = @(
    "*.md",
    "*.cs",
    "*.csproj",
    "*.sln",
    "*.json",
    "*.ps1",
    "*.psm1",
    "*.psd1"
)

$files = @()
foreach ($pattern in $filesToUpdate) {
    $found = Get-ChildItem -Path . -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
    $files += $found
}

# Exclude this script from replacements
$files = $files | Where-Object { $_.Name -ne "Rename-ToUnifiWatch.ps1" }

Write-Host "Found $($files.Count) files to update" -ForegroundColor Cyan

foreach ($file in $files) {
    Write-Host "  Processing: $($file.FullName.Replace($PWD, '.'))" -ForegroundColor Gray
    
    if (-not $WhatIf) {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        
        foreach ($replacement in $replacements) {
            $content = $content -replace [regex]::Escape($replacement.Old), $replacement.New
        }
        
        if ($content -ne $originalContent) {
            Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
            Write-Host "    ✓ Updated" -ForegroundColor Green
        } else {
            Write-Host "    - No changes" -ForegroundColor DarkGray
        }
    }
}

Write-Host ""
Write-Host "Step 2: Renaming files and directories..." -ForegroundColor Green

# Rename files
$filesToRename = @(
    @{ Old = "UnifiStockTracker.csproj"; New = "UnifiWatch.csproj" }
    @{ Old = "UnifiStockTracker-CSharp.sln"; New = "UnifiWatch.sln" }
)

foreach ($rename in $filesToRename) {
    $oldPath = Join-Path $PWD $rename.Old
    $newPath = Join-Path $PWD $rename.New
    
    if (Test-Path $oldPath) {
        Write-Host "  Renaming file: $($rename.Old) -> $($rename.New)" -ForegroundColor Cyan
        if (-not $WhatIf) {
            Move-Item -Path $oldPath -Destination $newPath -Force
            Write-Host "    ✓ Renamed" -ForegroundColor Green
        }
    }
}

# Rename test project folder
$testFolderOld = Join-Path $PWD "UnifiStockTracker.Tests"
$testFolderNew = Join-Path $PWD "UnifiWatch.Tests"

if (Test-Path $testFolderOld) {
    Write-Host "  Renaming directory: UnifiStockTracker.Tests -> UnifiWatch.Tests" -ForegroundColor Cyan
    if (-not $WhatIf) {
        Move-Item -Path $testFolderOld -Destination $testFolderNew -Force
        Write-Host "    ✓ Renamed" -ForegroundColor Green
    }
}

# Rename test project file inside renamed folder
$testProjOld = Join-Path $testFolderNew "UnifiStockTracker.Tests.csproj"
$testProjNew = Join-Path $testFolderNew "UnifiWatch.Tests.csproj"

if (Test-Path $testProjOld) {
    Write-Host "  Renaming test project file: UnifiStockTracker.Tests.csproj -> UnifiWatch.Tests.csproj" -ForegroundColor Cyan
    if (-not $WhatIf) {
        Move-Item -Path $testProjOld -Destination $testProjNew -Force
        Write-Host "    ✓ Renamed" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Next Steps (Manual):" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Rename the parent directory:" -ForegroundColor White
Write-Host "   cd .." -ForegroundColor Gray
Write-Host "   Rename-Item 'UnifiStockTracker-CSharp' 'UnifiWatch'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Update GitHub repository (if applicable):" -ForegroundColor White
Write-Host "   - Go to GitHub repository settings" -ForegroundColor Gray
Write-Host "   - Rename repository to 'UnifiWatch'" -ForegroundColor Gray
Write-Host "   - Update remote URL: git remote set-url origin https://github.com/EvotecIT/UnifiWatch.git" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Rebuild solution:" -ForegroundColor White
Write-Host "   cd UnifiWatch" -ForegroundColor Gray
Write-Host "   dotnet clean" -ForegroundColor Gray
Write-Host "   dotnet restore" -ForegroundColor Gray
Write-Host "   dotnet build" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Run tests:" -ForegroundColor White
Write-Host "   dotnet test" -ForegroundColor Gray
Write-Host ""

if ($WhatIf) {
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "WhatIf mode complete - no changes made" -ForegroundColor Yellow
    Write-Host "Run without -WhatIf to apply changes" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
} else {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Renaming complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
