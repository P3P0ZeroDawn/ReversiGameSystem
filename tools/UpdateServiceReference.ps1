param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$ServiceUtilityPath = [IO.Path]::Combine(
        [Environment]::GetFolderPath('ProgramFilesX86'),
        'Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\SvcUtil.exe')
)

$ErrorActionPreference = 'Stop'
$taskWorkspace = Split-Path -Parent $PSScriptRoot
$clientDirectory = Join-Path $taskWorkspace 'src\ReversiGameSystem.Client.Presentation'
$objectDirectory = [IO.Path]::GetFullPath((Join-Path $clientDirectory 'obj'))
$metadataDirectory = Join-Path $objectDirectory ('ServiceMetadata-' + [Guid]::NewGuid().ToString('N'))
$referenceDirectory = Join-Path $clientDirectory 'Connected Services\Server'

if (-not (Test-Path -LiteralPath $ServiceUtilityPath -PathType Leaf)) {
    throw 'The Windows SDK service reference generator was not found; supply ServiceUtilityPath.'
}

Push-Location $taskWorkspace
try {
    dotnet build src/ReversiGameSystem.Server.Contracts/ReversiGameSystem.Server.Contracts.csproj `
        --configuration $Configuration --verbosity quiet -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'The server contracts could not be compiled.' }

    New-Item -ItemType Directory -Force -Path $metadataDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $referenceDirectory | Out-Null
    $contractAssembly = Join-Path $taskWorkspace (
        'src\ReversiGameSystem.Server.Contracts\bin\' + $Configuration +
        '\net472\ReversiGameSystem.Server.Contracts.dll'
    )
    & $ServiceUtilityPath /nologo /target:metadata "/directory:$metadataDirectory" $contractAssembly
    if ($LASTEXITCODE -ne 0) { throw 'The contract metadata could not be exported.' }

    $metadataInputs = Get-ChildItem -LiteralPath $metadataDirectory -File |
        Where-Object { $_.Extension -in '.wsdl', '.xsd' } |
        ForEach-Object { $_.FullName }
    if (-not $metadataInputs) { throw 'The contract metadata is empty.' }

    $outputPath = Join-Path $referenceDirectory 'Reference.cs'
    & $ServiceUtilityPath /nologo '/namespace:*,ReversiGameSystem.ServerProxy' "/out:$outputPath" `
        /noConfig /serializer:DataContractSerializer @metadataInputs
    if ($LASTEXITCODE -ne 0) { throw 'The service reference could not be generated.' }
}
finally {
    Pop-Location
    $resolvedMetadataDirectory = [IO.Path]::GetFullPath($metadataDirectory)
    if (-not $resolvedMetadataDirectory.StartsWith(
        $objectDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The temporary metadata directory is outside the client build directory.'
    }
    if (Test-Path -LiteralPath $resolvedMetadataDirectory) {
        Remove-Item -LiteralPath $resolvedMetadataDirectory -Recurse -Force
    }
}
