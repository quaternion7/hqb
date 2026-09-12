[CmdletBinding()]
param([string]$UnityEditor = 'F:\Unity\Editor\Unity.exe')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('hqb-scale-tests-' + [guid]::NewGuid().ToString('N'))
$editorFiles = Join-Path $testRoot 'Assets'
New-Item -ItemType Directory -Path $editorFiles -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'plugin\src\SpawnlockScaleCompatibility.cs'), (Join-Path $repoRoot 'tests\SpawnlockCompatibilityTests.cs') -Destination $editorFiles

# Compile the actual miniaturizer, isolated from unrelated controller/game setup.
$controller = Get-Content (Join-Path $repoRoot 'plugin\src\HandQuickbeltController.cs') -Raw
$start = $controller.IndexOf('    internal sealed class HandQuickbeltMiniaturizer : MonoBehaviour')
if ($start -lt 0) { throw 'Miniaturizer class was not found.' }
$miniaturizer = "using System.Collections.Generic;`nusing FistVR;`nusing UnityEngine;`nnamespace HandQuickbelts {`n" + $controller.Substring($start)
[IO.File]::WriteAllText((Join-Path $editorFiles 'HandQuickbeltMiniaturizer.cs'), $miniaturizer)

# Use the same Harmony and MonoMod assemblies resolved by the production build.
$assets = Get-Content (Join-Path $repoRoot 'plugin\obj\project.assets.json') -Raw | ConvertFrom-Json
$packageRoot = @($assets.packageFolders.PSObject.Properties)[0].Name
foreach ($library in $assets.libraries.PSObject.Properties) {
    if ($library.Name -match '^(HarmonyX|Mono\.|MonoMod\.)') {
        foreach ($file in $library.Value.files) {
            if ($file -match '^lib/net35/.*\.dll$') {
                Copy-Item -LiteralPath (Join-Path $packageRoot ($library.Value.path + '/' + $file)) -Destination $editorFiles
            }
        }
    }
}
$logPath = Join-Path $testRoot 'test.log'
$process = Start-Process -FilePath $UnityEditor -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-createProject', ('"' + $testRoot + '"'), '-executeMethod', 'SpawnlockCompatibilityTests.Run', '-logFile', ('"' + $logPath + '"'))
Write-Output "Test process: $($process.Id)"
Write-Output "Test log: $logPath"
# Return immediately so callers can inspect progress and exit status without blocking the UI.
