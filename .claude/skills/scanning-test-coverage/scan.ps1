# Scan a source directory for public types and report missing 1:1 test files.
# Usage: scan.ps1 -SourceDir <path> -TestDir <path> [-SourceRoot <path>]
# Requires: rg (ripgrep) on PATH.

param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDir,

    [Parameter(Mandatory = $true)]
    [string]$TestDir,

    [string]$SourceRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command rg -ErrorAction SilentlyContinue)) {
    Write-Error "rg (ripgrep) not found on PATH"
    exit 1
}

if (-not (Test-Path -LiteralPath $SourceDir -PathType Container)) {
    Write-Error "source directory not found: $SourceDir"
    exit 1
}

if (-not (Test-Path -LiteralPath $TestDir -PathType Container)) {
    Write-Error "test directory not found: $TestDir"
    exit 1
}

if ([string]::IsNullOrEmpty($SourceRoot)) {
    $SourceRoot = ($SourceDir -split '/')[0]
}

# Determine test project name from TestDir (e.g., "tests/Engine.Tests" → "Engine.Tests")
$testProjectName = ($TestDir -split '/')[-1]

# Check if InternalsVisibleTo is configured for the test project
$internalsVisible = $false
$assemblyInfo = Join-Path -Path $SourceRoot -ChildPath "AssemblyInfo.cs"
if (Test-Path -LiteralPath $assemblyInfo) {
    $content = Get-Content -LiteralPath $assemblyInfo -Raw
    if ($content -match [regex]::Escape("InternalsVisibleTo(`"$testProjectName`")")) {
        $internalsVisible = $true
    }
}
if (-not $internalsVisible) {
    $ivtMatch = rg -l "InternalsVisibleTo\(""$testProjectName""\)" $SourceRoot -g "*.cs" 2>$null
    if ($ivtMatch) { $internalsVisible = $true }
}

$rel = $SourceDir
if ($rel.StartsWith("$SourceRoot/")) {
    $rel = $rel.Substring($SourceRoot.Length + 1)
} elseif ($rel -eq $SourceRoot) {
    $rel = ""
}
if ($rel.StartsWith("Scene/")) {
    $rel = $rel.Substring(6)
}

$testSub = ""
if ($rel) { $testSub = "/$rel" }

$missing = 0
$covered = 0
$grouped = 0
$skipped = 0

if ($internalsVisible) {
    $pattern = "(?:public|internal) (?:\w+ )*(class|record|struct) "
} else {
    $pattern = "public (?:\w+ )*(class|record|struct) "
}
$matches = rg --no-heading -n $pattern $SourceDir -g "*.cs" 2>$null
if (-not $matches) { $matches = @() }

foreach ($line in $matches) {
    if ($line -match '^(?<file>[^:]+):(?<rest>.+)$') {
        $file = $Matches.file
        $rest = $Matches.rest
    } else {
        continue
    }

    $reType = if ($internalsVisible) { '(?:public|internal) (?:\w+ )*(class|record|struct) (?<type>[A-Za-z_][A-Za-z0-9_]*)' } else { 'public (?:\w+ )*(class|record|struct) (?<type>[A-Za-z_][A-Za-z0-9_]*)' }
    if ($rest -notmatch $reType) {
        continue
    }

    $typeName = $Matches.type

    rg -q "\[SkipUnitTests\]" $file 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Output "skipped|$file|$typeName|[SkipUnitTests]"
        $skipped++
        continue
    }
    $expected = "$TestDir$testSub/${typeName}Tests.cs" -replace '\\', '/'

    $oneToOne = rg --files $TestDir -g "${typeName}Tests.cs" 2>$null | Select-Object -First 1
    if ($oneToOne) {
        $oneToOne = $oneToOne -replace '\\', '/'
        Write-Output "covered|$file|$typeName|$oneToOne"
        $covered++
        continue
    }

    $groupMatches = rg -l -g "*.cs" "\b$typeName\b" $TestDir 2>$null | Where-Object { $_ -notmatch "${typeName}Tests\.cs$" }
    $groupMatch = $groupMatches | Select-Object -First 1
    if ($groupMatch) {
        $groupMatch = $groupMatch -replace '\\', '/'
        Write-Output "grouped|$file|$typeName|$groupMatch"
        $grouped++
        continue
    }

    Write-Output "missing|$file|$typeName|$expected"
    $missing++
}

Write-Output "---"
Write-Output "summary|missing=$missing|covered=$covered|grouped=$grouped|skipped=$skipped"
