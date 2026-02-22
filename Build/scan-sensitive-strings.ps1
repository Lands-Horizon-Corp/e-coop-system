#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Scans source code for potentially sensitive strings that should be encrypted

.DESCRIPTION
    Analyzes C# source files for string literals that might contain sensitive
    data and should be encrypted before production deployment.

.EXAMPLE
    .\scan-sensitive-strings.ps1
    .\scan-sensitive-strings.ps1 -Path "Services" -Verbose
#>

param(
    [string]$Path = ".",
    [switch]$ShowContext,
    [int]$ContextLines = 2
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Sensitive String Scanner" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Patterns that indicate sensitive data
$patterns = @{
    "URL" = @(
        'https?://[^\s"'']+',
        'http://[^\s"'']+'
    )
    "API Endpoint" = @(
        '"/api/[^\s"'']+',
        '''/api/[^\s"'']+'''
    )
    "Connection String" = @(
        'Server\s*=',
        'Database\s*=',
        'Password\s*=',
        'ConnectionString\s*='
    )
    "Key/Secret" = @(
        'ApiKey\s*=',
        'SecretKey\s*=',
        'Token\s*=',
        'api[_-]?key',
        'secret[_-]?key'
    )
    "File Path" = @(
        '\.dat"',
        '\.key"',
        '\.secret"',
        '\.config"'
    )
}

# Get all C# files
$csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse | 
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\|\.g\.cs$|AssemblyInfo\.cs$' }

Write-Host "Scanning $($csFiles.Count) C# files..." -ForegroundColor White
Write-Host ""

$findings = @()

foreach ($file in $csFiles) {
    $relativePath = Resolve-Path -Relative $file.FullName
    $content = Get-Content $file.FullName
    
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        $lineNumber = $i + 1
        
        foreach ($category in $patterns.Keys) {
            foreach ($pattern in $patterns[$category]) {
                if ($line -match $pattern) {
                    # Skip if already encrypted (contains "Encrypt", "Decrypt", or Base64 pattern)
                    if ($line -match '(Encrypt|Decrypt|FromBase64String|ToBase64String|^[A-Za-z0-9+/=]{20,}$)') {
                        continue
                    }
                    
                    # Skip comments
                    if ($line.Trim().StartsWith('//') -or $line.Trim().StartsWith('*')) {
                        continue
                    }
                    
                    $findings += [PSCustomObject]@{
                        File = $relativePath
                        Line = $lineNumber
                        Category = $category
                        Content = $line.Trim()
                        Pattern = $pattern
                    }
                    
                    break
                }
            }
        }
    }
}

# Display findings
if ($findings.Count -eq 0) {
    Write-Host "? No sensitive strings found!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your code appears to be secure." -ForegroundColor Green
    Write-Host "All sensitive data is likely already encrypted." -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "??  Found $($findings.Count) potentially sensitive string(s):" -ForegroundColor Yellow
    Write-Host ""
    
    $groupedFindings = $findings | Group-Object Category
    
    foreach ($group in $groupedFindings) {
        Write-Host "[$($group.Name)] - $($group.Count) occurrence(s):" -ForegroundColor Cyan
        
        foreach ($finding in $group.Group) {
            Write-Host "  File: $($finding.File):$($finding.Line)" -ForegroundColor White
            Write-Host "  Code: $($finding.Content)" -ForegroundColor Gray
            
            if ($ShowContext) {
                $fileContent = Get-Content $finding.File
                $start = [Math]::Max(0, $finding.Line - 1 - $ContextLines)
                $end = [Math]::Min($fileContent.Count - 1, $finding.Line - 1 + $ContextLines)
                
                Write-Host "`n  Context:" -ForegroundColor DarkGray
                for ($i = $start; $i -le $end; $i++) {
                    $prefix = if ($i -eq $finding.Line - 1) { "  >" } else { "   " }
                    Write-Host "$prefix $($i + 1): $($fileContent[$i])" -ForegroundColor DarkGray
                }
            }
            
            Write-Host ""
        }
        Write-Host ""
    }
    
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host " Recommendations" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "For each sensitive string:" -ForegroundColor Yellow
    Write-Host "  1. Encrypt it:" -ForegroundColor White
    Write-Host "     .\Build\encrypt-string.ps1 -PlainText `"your-sensitive-string`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Replace in code:" -ForegroundColor White
    Write-Host "     // Before:" -ForegroundColor Gray
    Write-Host "     const string url = `"https://api.example.com`";" -ForegroundColor Gray
    Write-Host ""
    Write-Host "     // After:" -ForegroundColor Gray
    Write-Host "     const string encUrl = `"encrypted-base64-here`";" -ForegroundColor Gray
    Write-Host "     var url = StringEncryption.Decrypt(encUrl);" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Test:" -ForegroundColor White
    Write-Host "     dotnet build && dotnet run" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Run verification after build:" -ForegroundColor Cyan
Write-Host "  .\Build\verify-protection.ps1" -ForegroundColor White
Write-Host ""

Write-Host "For more information:" -ForegroundColor Cyan
Write-Host "  docs/OBFUSCATION.md" -ForegroundColor White
Write-Host "  docs/SETUP_OBFUSCATION.md" -ForegroundColor White
Write-Host ""
