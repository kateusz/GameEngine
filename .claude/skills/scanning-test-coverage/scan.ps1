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
$excluded = 0

$matches = rg --no-heading -n "public (?:\w+ )*(class|record|struct) " $SourceDir -g "*.cs" 2>$null
if (-not $matches) { $matches = @() }

foreach ($line in $matches) {
    if ($line -match '^(?<file>[^:]+):(?<rest>.+)$') {
        $file = $Matches.file
        $rest = $Matches.rest
    } else {
        continue
    }

    if ($rest -match 'public interface ' -or $rest -match 'public enum ') {
        $excluded++
        continue
    }

    if ($rest -notmatch 'public (?:\w+ )*(class|record|struct) (?<type>[A-Za-z_][A-Za-z0-9_]*)') {
        continue
    }

    $typeName = $Matches.type
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
Write-Output "summary|missing=$missing|covered=$covered|grouped=$grouped|excluded=$excluded"
