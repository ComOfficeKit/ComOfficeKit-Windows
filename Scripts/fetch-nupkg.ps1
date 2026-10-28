# Puts a ComOfficeKit package in packages/, where the samples' nuget.config looks for it.
#
#   Scripts/fetch-nupkg.ps1 -Tag <release tag>      # a release's package, from this repository's releases
#   Scripts/fetch-nupkg.ps1 -From <path to .nupkg>  # a package you already have
#
# packages/ then holds that one package and no other. NuGet keeps a copy of every package it restores,
# by id and version; the copy of this version is dropped, so a rebuilt package of the same version is
# the one the samples get.
param(
    [string]$Tag,
    [string]$From
)
$ErrorActionPreference = 'Stop'
if ([bool]$Tag -eq [bool]$From) { throw 'give one of -Tag <release tag> or -From <path to .nupkg>' }
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$packages = Join-Path $root 'packages'

# Check the source before touching packages/, so a wrong tag leaves a working checkout as it was.
if ($Tag) {
    $repo = 'ComOfficeKit/ComOfficeKit-Windows'
    gh release view $Tag --repo $repo *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "no such release: $Tag. Published releases:"
        gh release list --repo $repo --limit 10
        exit 1
    }
} elseif (-not (Test-Path $From) -or -not $From.EndsWith('.nupkg')) {
    throw "not a .nupkg: $From"
}

New-Item -ItemType Directory -Force $packages | Out-Null
Get-ChildItem $packages -Filter 'ComOfficeKit.*.nupkg' | Remove-Item -Force
if ($Tag) {
    gh release download $Tag --repo $repo --pattern 'ComOfficeKit.*.nupkg' --dir $packages
    if ($LASTEXITCODE -ne 0) { throw "could not download the package of $Tag" }
} else {
    Copy-Item $From $packages
}

$package = @(Get-ChildItem $packages -Filter 'ComOfficeKit.*.nupkg')
if ($package.Count -ne 1) { throw "expected one package in packages/, found $($package.Count)" }
$version = $package[0].Name -replace '^ComOfficeKit\.', '' -replace '\.nupkg$', ''
$cache = ((dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', '').Trim()
Remove-Item -Recurse -Force (Join-Path $cache "comofficekit\$version") -ErrorAction SilentlyContinue
Write-Host "packages/$($package[0].Name) ($([math]::Round($package[0].Length / 1MB, 1)) MB): ComOfficeKit $version"
