param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$capturePath = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath (Join-Path $capturePath 'run.json')) {
    throw 'Capture directory already contains a run. Choose a new directory to preserve the evidence.'
}
$previousOutput = $env:RENDER_BASELINE_DIR
try {
    $env:RENDER_BASELINE_DIR = $capturePath
    dotnet test (Join-Path $repoRoot 'tests/Engine.GraphicsTests/Engine.GraphicsTests.csproj') `
        --configuration $Configuration --filter 'FullyQualifiedName~RenderingReferenceScenesTests' --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Reference capture failed with exit code $LASTEXITCODE" }
    if (!(Test-Path -LiteralPath (Join-Path $capturePath 'run.json'))) {
        throw 'No capture manifest: graphics tests may have been skipped on this platform.'
    }
    python (Join-Path $PSScriptRoot 'compare_reference.py') $capturePath
    if ($LASTEXITCODE -ne 0) { throw 'Reference preview conversion failed' }
}
finally { $env:RENDER_BASELINE_DIR = $previousOutput }
