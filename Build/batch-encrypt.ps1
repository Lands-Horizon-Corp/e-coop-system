#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Batch encrypt multiple strings for source code embedding

.DESCRIPTION
    Encrypts multiple strings from a file or array and outputs
    C# code ready to paste into your source files.

.EXAMPLE
    .\batch-encrypt.ps1 -InputFile "strings-to-encrypt.txt"
    .\batch-encrypt.ps1 -Strings @("url1", "url2", "url3")
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$InputFile,
    
    [Parameter(Mandatory=$false)]
    [string[]]$Strings,
    
    [Parameter(Mandatory=$false)]
    [switch]$OutputCSharp
)

# Encryption functions matching StringEncryption.cs
function Get-DerivedKey {
    $data = "ECoopSystem.SecureKey.2026.LandsHorizon.v1"
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    return $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($data))
}

function Get-DerivedIV {
    $data = "ECoopSystem.IV.2026.SecureInit"
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($data))
    $iv = New-Object byte[] 16
    [Array]::Copy($hash, $iv, 16)
    return $iv
}

function Encrypt-String {
    param([string]$plainText)
    
    if ([string]::IsNullOrEmpty($plainText)) {
        return ""
    }
    
    $key = Get-DerivedKey
    $iv = Get-DerivedIV
    
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $key
    $aes.IV = $iv
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    
    $encryptor = $aes.CreateEncryptor($aes.Key, $aes.IV)
    $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($plainText)
    $encryptedBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
    
    return [Convert]::ToBase64String($encryptedBytes)
}

function Get-SafeVariableName {
    param([string]$text)
    
    # Convert to safe C# variable name
    $safe = $text -replace '[^a-zA-Z0-9_]', '_'
    $safe = $safe -replace '^[0-9]', 'N$0'
    return $safe
}

# Get strings to encrypt
$stringsToEncrypt = @()

if ($InputFile) {
    if (Test-Path $InputFile) {
        $stringsToEncrypt = Get-Content $InputFile | Where-Object { $_.Trim() -ne "" }
    } else {
        Write-Host "Error: File not found: $InputFile" -ForegroundColor Red
        exit 1
    }
} elseif ($Strings) {
    $stringsToEncrypt = $Strings
} else {
    Write-Host "Error: Please provide either -InputFile or -Strings parameter" -ForegroundColor Red
    Write-Host "Example: .\batch-encrypt.ps1 -Strings @('url1', 'url2')" -ForegroundColor Yellow
    exit 1
}

if ($stringsToEncrypt.Count -eq 0) {
    Write-Host "Error: No strings to encrypt" -ForegroundColor Red
    exit 1
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host " Batch String Encryption" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Encrypting $($stringsToEncrypt.Count) strings...`n" -ForegroundColor White

$results = @()

foreach ($str in $stringsToEncrypt) {
    $encrypted = Encrypt-String -plainText $str
    $varName = "Enc_" + (Get-SafeVariableName $str)
    
    $results += [PSCustomObject]@{
        Original = $str
        Encrypted = $encrypted
        VariableName = $varName
    }
}

# Output results
if ($OutputCSharp) {
    Write-Host "// Auto-generated encrypted strings" -ForegroundColor Green
    Write-Host "// Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Green
    Write-Host ""
    
    foreach ($result in $results) {
        Write-Host "// Original: $($result.Original)"
        Write-Host "private const string $($result.VariableName) = `"$($result.Encrypted)`";"
        Write-Host "public static string $(($result.VariableName) -replace '^Enc_', '') => StringEncryption.Decrypt($($result.VariableName));"
        Write-Host ""
    }
} else {
    # Table output
    Write-Host ($results | Format-Table -AutoSize | Out-String)
    
    Write-Host "`nTo generate C# code, use: -OutputCSharp" -ForegroundColor Yellow
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host " Encryption Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Create output file
$outputFile = "encrypted-strings-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
$results | Export-Csv -Path $outputFile -NoTypeInformation
Write-Host "Results saved to: $outputFile" -ForegroundColor Cyan
