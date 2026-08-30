[CmdletBinding()]
param(
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$stage = $null
$temporaryZip = $null

Push-Location $repoRoot
try {
    if (-not $SkipBuild)
    {
        & dotnet build .\HQB.sln -c Release
        if ($LASTEXITCODE -ne 0)
        {
            throw "Release build failed."
        }
    }

    $manifest = Get-Content .\manifest.json -Raw | ConvertFrom-Json
    if ($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$')
    {
        throw "manifest.json contains an invalid package name."
    }
    if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$')
    {
        throw "manifest.json contains an invalid semantic version."
    }
    if ($manifest.description.Length -gt 250)
    {
        throw "manifest.json description exceeds 250 characters."
    }
    if ($manifest.dependencies.Count -eq 0)
    {
        throw "manifest.json must declare its dependencies."
    }

    Add-Type -AssemblyName System.Drawing
    $icon = [System.Drawing.Image]::FromFile((Resolve-Path .\icon.png))
    try
    {
        if ($icon.Width -ne 256 -or $icon.Height -ne 256)
        {
            throw "icon.png must be exactly 256x256 pixels."
        }
    }
    finally
    {
        $icon.Dispose()
    }

    $dll = (Resolve-Path .\plugin\bin\Release\net35\quaternion.hqb.dll).Path
    $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version
    $dllVersion = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
    if ($dllVersion -ne $manifest.version_number)
    {
        throw "DLL version $dllVersion does not match manifest version $($manifest.version_number)."
    }

    $stage = Join-Path ([System.IO.Path]::GetTempPath()) ("hqb-package-" + [guid]::NewGuid().ToString("N"))
    $temporaryZip = "$stage.zip"
    New-Item -ItemType Directory -Path $stage | Out-Null

    $packageFiles = @(
        $dll,
        ".\manifest.json",
        ".\README.md",
        ".\CHANGELOG.md",
        ".\icon.png",
        ".\LICENSE"
    )
    Copy-Item -LiteralPath $packageFiles -Destination $stage
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $temporaryZip -CompressionLevel Optimal

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($temporaryZip)
    try
    {
        $actualNames = @($archive.Entries | ForEach-Object { $_.FullName })
        $expectedNames = @("quaternion.hqb.dll", "manifest.json", "README.md", "CHANGELOG.md", "icon.png", "LICENSE")
        foreach ($name in $expectedNames)
        {
            if ($actualNames -notcontains $name)
            {
                throw "Package is missing $name."
            }
        }
        if ($actualNames.Count -ne $expectedNames.Count)
        {
            throw "Package contains unexpected files."
        }
    }
    finally
    {
        $archive.Dispose()
    }

    New-Item -ItemType Directory -Path .\dist -Force | Out-Null
    $output = Join-Path (Resolve-Path .\dist) "quaternion-Hand_Quickbelts-$($manifest.version_number).zip"
    Copy-Item -LiteralPath $temporaryZip -Destination $output -Force

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $output).Hash
    Write-Output "Package: $output"
    Write-Output "SHA256:  $hash"
}
finally
{
    Pop-Location

    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($stage -and (Test-Path -LiteralPath $stage))
    {
        $resolvedStage = [System.IO.Path]::GetFullPath($stage)
        if (-not $resolvedStage.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase))
        {
            throw "Refusing to clean a staging path outside the temporary directory."
        }
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
    if ($temporaryZip -and (Test-Path -LiteralPath $temporaryZip))
    {
        $resolvedZip = [System.IO.Path]::GetFullPath($temporaryZip)
        if (-not $resolvedZip.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase))
        {
            throw "Refusing to clean a staging archive outside the temporary directory."
        }
        Remove-Item -LiteralPath $resolvedZip -Force
    }
}
