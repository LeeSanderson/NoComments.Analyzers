#Requires -Version 7
<#
.SYNOPSIS
    Tags and pushes the next vX.Y.Z release, which triggers .github/workflows/release.yml.

.DESCRIPTION
    Copied from OpinionatedDotNetCodingStandards (scripts/New-ReleaseTag.ps1), minus that
    repository's own pre-release checks.

    The git tag is the only source of the package version. The script bumps the highest
    existing vMAJOR.MINOR.PATCH tag (v0.0.0 when there is none), requires a matching
    '## [vX.Y.Z]' heading in CHANGELOG.md, runs the full test suite, then tags and pushes.
    Nothing is tagged if the heading is missing or a test fails.

    Prereleases (for example v0.1.0-beta.1) are not made by this script: tag them by hand.

.EXAMPLE
    ./scripts/New-ReleaseTag.ps1 -Minor
#>
[CmdletBinding()]
param(
    [switch] $Major,
    [switch] $Minor,
    [switch] $Patch,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$selected = @($Major, $Minor, $Patch) | Where-Object { $_ }
if ($selected.Count -eq 0) {
    Write-Error "Specify exactly one of -Major, -Minor, or -Patch."
    exit 1
}
if ($selected.Count -gt 1) {
    Write-Error "-Major, -Minor, and -Patch are mutually exclusive. Specify exactly one."
    exit 1
}

$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne 'main') {
    Write-Error "Must be on 'main' to create a release tag. Current branch: $branch"
    exit 1
}

Write-Host "Pulling latest changes..."
git pull
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$tags = git tag -l 'v*'
$highest = $tags |
    Where-Object { $_ -match '^v(\d+)\.(\d+)\.(\d+)$' } |
    Sort-Object {
        $parts = $_ -replace '^v', '' -split '\.'
        [int64]('{0:D9}{1:D9}{2:D9}' -f [int]$parts[0], [int]$parts[1], [int]$parts[2])
    } -Descending |
    Select-Object -First 1

if (-not $highest) {
    $highest = 'v0.0.0'
}

$parts = $highest -replace '^v', '' -split '\.'
$verMajor = [int]$parts[0]
$verMinor = [int]$parts[1]
$verPatch = [int]$parts[2]

if ($Major) {
    $verMajor++; $verMinor = 0; $verPatch = 0
} elseif ($Minor) {
    $verMinor++; $verPatch = 0
} else {
    $verPatch++
}

$newTag = "v$verMajor.$verMinor.$verPatch"
Write-Host "New tag: $newTag (previous: $highest)"

if (-not $Force) {
    $changelog = Get-Content -Path 'CHANGELOG.md' -Raw -ErrorAction SilentlyContinue
    if (-not $changelog) {
        Write-Error "CHANGELOG.md not found. Use -Force to skip the changelog check."
        exit 1
    }
    $escapedTag = [regex]::Escape($newTag)
    if ($changelog -notmatch "(?im)^##\s*\[$escapedTag\]") {
        Write-Error "CHANGELOG.md has no heading matching '## [$newTag]'. Add a changelog entry or use -Force to skip."
        exit 1
    }
}

Write-Host "Running full test suite..."
# Every test packs or builds a throwaway consumer project, so peak memory grows with the number
# of tests running at once. The default (one per CPU thread) can exhaust memory on a developer
# machine. The test project runs on Microsoft.Testing.Platform, which takes --max-threads directly.
dotnet test --solution NoComments.Analyzers.slnx --configuration Release --max-threads 3
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git tag $newTag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git push origin $newTag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Released $newTag"
