<#
.SYNOPSIS
    Provisions a self-signed X.509 certificate for ReversiGameSystem WCF server.

.DESCRIPTION
    Generates a self-signed server authentication certificate with Subject CN=ReversiGameServer,
    installs it in Cert:\LocalMachine\My, and installs the public certificate in
    Cert:\LocalMachine\TrustedPeople to support TLS/NetTcpBinding Transport security
    with PeerOrChainTrust.

.NOTES
    Requires elevated Administrator privileges.
#>

#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [string]$SubjectName = "ReversiGameServer",
    [int]$ValidYears = 5
)

$ErrorActionPreference = "Stop"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be executed with elevated Administrator privileges."
    exit 1
}

$certSubject = "CN=$SubjectName"

Write-Host "Configuring X.509 certificate for $certSubject..."

# Remove any existing certificate with the same subject in LocalMachine\My
$existingMyCerts = Get-ChildItem -Path Cert:\LocalMachine\My |
    Where-Object { $_.Subject -eq $certSubject }
foreach ($oldCert in $existingMyCerts) {
    Write-Host "Removing existing certificate from LocalMachine\My: $($oldCert.Thumbprint)"
    Remove-Item -Path $oldCert.PSPath -Force
}

# Remove any existing certificate with the same subject in LocalMachine\TrustedPeople
$existingTrusted = Get-ChildItem -Path Cert:\LocalMachine\TrustedPeople |
    Where-Object { $_.Subject -eq $certSubject }
foreach ($oldTrusted in $existingTrusted) {
    Write-Host "Removing existing certificate from LocalMachine\TrustedPeople:" `
        $oldTrusted.Thumbprint
    Remove-Item -Path $oldTrusted.PSPath -Force
}

# Generate new self-signed certificate with Server Authentication EKU
$newCert = New-SelfSignedCertificate `
    -Subject $certSubject `
    -DnsName $SubjectName, "localhost", "127.0.0.1" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyExportPolicy Exportable `
    -KeySpec KeyExchange `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

Write-Host "Created certificate in LocalMachine\My with thumbprint $($newCert.Thumbprint)"

# Export public certificate and import into LocalMachine\TrustedPeople
$tempCertPath = [System.IO.Path]::Combine(
    [System.IO.Path]::GetTempPath(),
    "$SubjectName-$([Guid]::NewGuid().ToString('N')).cer")

try {
    Export-Certificate -Cert $newCert -FilePath $tempCertPath | Out-Null
    Import-Certificate -FilePath $tempCertPath `
        -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
    Write-Host "Imported public certificate into LocalMachine\TrustedPeople."
}
finally {
    if (Test-Path $tempCertPath) {
        Remove-Item -Path $tempCertPath -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Certificate setup complete for $SubjectName."
