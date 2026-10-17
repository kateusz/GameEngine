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

Copy-Item "$GameOut\GameAssembly.dll", "$GameOut\GameAssembly.pdb", "$GameDir\game.config.json" $Stage -Force
Get-ChildItem "$GameDir\assets" -Directory | Where-Object { $_.Name -ne "scripts" } | ForEach-Object {
    $target = "$Stage\assets\$($_.Name)"
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item "$($_.FullName)\*" $target -Recurse -Force
}

# The Runtime loads cooked "<model>.mesh" siblings only; (re)cook any that are missing or stale.
dotnet "$CookOut\MeshCook.dll" "$Stage\assets\models"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($NoRun) { Write-Host "Staged to $Stage"; exit 0 }
Push-Location $Stage
try { dotnet Runtime.dll } finally { Pop-Location }
