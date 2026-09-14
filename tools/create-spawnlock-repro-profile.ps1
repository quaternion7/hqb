[CmdletBinding()]
param(
    [string]$ProfileName = 'HQB 1.0.0 - MasteryCamos Repro',
    [string]$UnityMonoLibraries = 'F:\Unity\Editor\Data\Mono\lib\mono\unity'
)

$ErrorActionPreference = 'Stop'
$managerRoot = Join-Path $env:APPDATA 'r2modmanPlus-local\H3VR'
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $managerRoot 'profiles'))
$profile = [IO.Path]::GetFullPath((Join-Path $profilesRoot $ProfileName))
if ((Split-Path -Parent $profile) -ne $profilesRoot -or (Test-Path -LiteralPath $profile)) {
    throw 'Choose a new profile name directly inside the H3VR profiles directory.'
}
$staging = Join-Path ([IO.Path]::GetTempPath()) ('hqb-repro-packages-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
$packages = @(
    @{ Author = 'BepInEx'; Name = 'BepInExPack_H3VR'; Version = '5.4.1700' },
    @{ Author = 'nrgill28'; Name = 'Sodalite'; Version = '1.5.1' },
    # 0.0.1 advertises NGA.JsonSaveSystem, so MasteryCamos rejects its plugin GUID.
    @{ Author = 'NGA'; Name = 'JsonFileIO'; Version = '0.0.3' },
    @{ Author = 'NGA'; Name = 'ProfileSaveFolder'; Version = '1.0.0' },
    @{ Author = 'NGA'; Name = 'MasteryCamos'; Version = '2.2.3' },
    @{ Author = 'quaternion'; Name = 'Hand_Quickbelts'; Version = '1.0.0' }
)
foreach ($package in $packages) {
    $package.Id = $package.Author + '-' + $package.Name
    $cache = Join-Path $managerRoot ('cache\' + $package.Id + '\' + $package.Version)
    if (Test-Path -LiteralPath (Join-Path $cache 'manifest.json')) {
        $package.Source = $cache
    } else {
        $package.Source = Join-Path $staging $package.Id
        $zip = Join-Path $staging ($package.Id + '.zip')
        Invoke-WebRequest ('https://thunderstore.io/package/download/' + $package.Author + '/' + $package.Name + '/' + $package.Version + '/') -OutFile $zip
        Expand-Archive -LiteralPath $zip -DestinationPath $package.Source
    }
    $package.Manifest = Get-Content (Join-Path $package.Source 'manifest.json') -Raw | ConvertFrom-Json
    if ($package.Manifest.name -ne $package.Name -or $package.Manifest.version_number -ne $package.Version) {
        throw ('Package identity mismatch: ' + $package.Id)
    }
}
foreach ($package in $packages) {
    foreach ($dependency in $package.Manifest.dependencies) {
        if ($dependency -notmatch '^(.+)-(\d+\.\d+\.\d+)$') { throw "Invalid dependency: $dependency" }
        $dependencyId = $Matches[1]
        $minimum = [version]$Matches[2]
        $installed = @($packages | Where-Object { $_.Id -eq $dependencyId })
        if ($installed.Count -ne 1 -or [version]$installed[0].Version -lt $minimum) {
            throw "Unsatisfied dependency: $dependency"
        }
    }
}
# MasteryCamos references Newtonsoft.Json 13 without declaring/distributing it.
# Supply the official .NET 3.5 library explicitly for this minimal test profile.
$jsonArchive = Join-Path $staging 'newtonsoft-json-13.0.3.zip'
$jsonPackage = Join-Path $staging 'newtonsoft-json-13.0.3'
Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg' -OutFile $jsonArchive
Expand-Archive -LiteralPath $jsonArchive -DestinationPath $jsonPackage
$supportLibraries = @('System.Data.dll', 'System.Xml.Linq.dll', 'System.Transactions.dll', 'Mono.Data.Tds.dll')
foreach ($filename in $supportLibraries) {
    if (!(Test-Path -LiteralPath (Join-Path $UnityMonoLibraries $filename))) { throw "Missing Unity runtime library: $filename" }
}
New-Item -ItemType Directory -Path $profile | Out-Null
$entries = @()
foreach ($package in $packages) {
    $pluginDir = Join-Path $profile ('BepInEx\plugins\' + $package.Id)
    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    if ($package.Id -eq 'BepInEx-BepInExPack_H3VR') {
        Get-ChildItem -LiteralPath (Join-Path $package.Source 'BepInExPack_H3VR') -Force | Copy-Item -Destination $profile -Recurse -Force
    } else {
        foreach ($entry in Get-ChildItem -LiteralPath $package.Source -Force) {
            if ($entry.PSIsContainer -and $entry.Name -in @('plugins', 'patchers')) {
                $destination = Join-Path $profile ('BepInEx\' + $entry.Name + '\' + $package.Id)
                New-Item -ItemType Directory -Path $destination -Force | Out-Null
                Get-ChildItem -LiteralPath $entry.FullName -Force | Copy-Item -Destination $destination -Recurse -Force
            } else {
                Copy-Item -LiteralPath $entry.FullName -Destination $pluginDir -Recurse -Force
            }
        }
    }
    $version = [version]$package.Version
    $metadata = [ordered]@{
        manifestVersion = 1; name = $package.Id; authorName = $package.Author
        websiteUrl = ('https://thunderstore.io/c/h3vr/p/' + $package.Author + '/' + $package.Name + '/')
        displayName = $package.Name; description = $package.Manifest.description
        gameVersion = '0'; networkMode = 'both'; packageType = 'other'; installMode = 'managed'
        installedAtTime = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds(); loaders = @()
        dependencies = @($package.Manifest.dependencies); incompatibilities = @(); optionalDependencies = @()
        versionNumber = [ordered]@{ major = $version.Major; minor = $version.Minor; patch = $version.Build }
        enabled = $true; onlineSource = $true; trustedPackage = $false
    }
    $entries += $metadata
    $metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $pluginDir 'mm_v2_manifest.json') -Encoding utf8
}
# Emit conventional mods.yml without relying on an external YAML serializer.
$yaml = foreach ($entry in $entries) {
    '- manifestVersion: 1'
    foreach ($key in $entry.Keys | Where-Object { $_ -ne 'manifestVersion' }) {
        if ($key -eq 'versionNumber') {
            '  versionNumber:'
            foreach ($part in @('major', 'minor', 'patch')) { '    ' + $part + ': ' + $entry[$key][$part] }
        } else {
            '  ' + $key + ': ' + (ConvertTo-Json -InputObject $entry[$key] -Compress -Depth 5)
        }
    }
}
$yaml | Set-Content -LiteralPath (Join-Path $profile 'mods.yml') -Encoding utf8
New-Item -ItemType Directory -Path (Join-Path $profile '_state') | Out-Null
'currentState: []' | Set-Content -LiteralPath (Join-Path $profile '_state\installation_state.yml') -Encoding utf8
$runtimeDir = Join-Path $profile 'BepInEx\plugins\HQB-Repro-Runtime-Libraries'
New-Item -ItemType Directory -Path $runtimeDir | Out-Null
Copy-Item -LiteralPath (Join-Path $jsonPackage 'lib\net35\Newtonsoft.Json.dll'), (Join-Path $jsonPackage 'LICENSE.md') -Destination $runtimeDir
'Newtonsoft.Json 13.0.3 (net35) from official NuGet. Required by MasteryCamos but missing from its package dependencies. Local test-profile support library; HQB is unchanged.' | Set-Content (Join-Path $runtimeDir 'README.txt')
foreach ($filename in $supportLibraries) {
    Copy-Item -LiteralPath (Join-Path $UnityMonoLibraries $filename) -Destination $runtimeDir
}
Add-Content (Join-Path $runtimeDir 'README.txt') 'System.Data, System.Xml.Linq, System.Transactions and Mono.Data.Tds come from the locally installed Unity Mono unity runtime. Local test-only dependencies, not an HQB release.'
# Keep the release defaults; no existing profile settings or saves are copied.
Write-Output "Created profile: $profile"
$packages | ForEach-Object { Write-Output ($_.Id + '-' + $_.Version) }
