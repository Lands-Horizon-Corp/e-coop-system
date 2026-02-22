#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Encrypts strings for use in source code

.DESCRIPTION
    Utility script to encrypt sensitive strings using the same encryption
    algorithm as StringEncryption.cs for embedding in source code.

.EXAMPLE
    .\encrypt-string.ps1 -PlainText "https://api.example.com/"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$PlainText
)

# This is a simplified version - implement actual encryption matching StringEncryption.cs
Add-Type -AssemblyName System.Security

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

$encrypted = Encrypt-String -plainText $PlainText

Write-Host "`nOriginal: $PlainText" -ForegroundColor Cyan
Write-Host "Encrypted: $encrypted" -ForegroundColor Green
Write-Host "`nUse this in source code:" -ForegroundColor Yellow
Write-Host "StringEncryption.Decrypt(`"$encrypted`")" -ForegroundColor White
Write-Host ""
