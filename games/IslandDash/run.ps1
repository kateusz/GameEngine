# Windows equivalent of run.sh:  .\run.ps1   (or  .\run.ps1 -NoRun  /  .\run.ps1 -Config Debug)
param([string]$Config = "Release", [switch]$NoRun)
$ErrorActionPreference = "Stop"

$GameDir = $PSScriptRoot
$EngineDir = Resolve-Path "$GameDir\..\.."
$Stage = "$GameDir\.build\$Config"
$GameOut = "$GameDir\.build\game-$Config"
$CookOut = "$GameDir\.build\meshcook-$Config"
$BuildFlags = @("-c", $Config, "--nologo", "-v", "quiet", "-clp:ErrorsOnly;NoSummary")

dotnet build "$EngineDir\Runtime\Runtime.csproj" @BuildFlags -o $Stage
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build "$GameDir\IslandDash.csproj" @BuildFlags -o $GameOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build "$GameDir\tools\MeshCook\MeshCook.csproj" @BuildFlags -o $CookOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Cook into source assets first so editor Play and the staged Runtime share the same siblings.
$rid = if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { "win-arm64" } else { "win-x64" }
$env:PATH = "$CookOut\runtimes\$rid\native;$env:PATH"
dotnet "$CookOut\MeshCook.dll" "$GameDir\assets\models"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item "$GameOut\GameAssembly.dll", "$GameOut\GameAssembly.pdb", "$GameDir\game.config.json" $Stage -Force
Get-ChildItem "$GameDir\assets" -Directory | Where-Object { $_.Name -ne "scripts" } | ForEach-Object {
    $target = "$Stage\assets\$($_.Name)"
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item "$($_.FullName)\*" $target -Recurse -Force
}

if ($NoRun) { Write-Host "Staged to $Stage"; exit 0 }
$env:PATH = "$Stage\runtimes\$rid\native;$env:PATH"
Push-Location $Stage
try { dotnet Runtime.dll } finally { Pop-Location }
