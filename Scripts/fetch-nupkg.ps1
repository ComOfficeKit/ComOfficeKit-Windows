# Puts a ComOfficeKit package in packages/, where the samples' nuget.config looks for it.
#
#   Scripts/fetch-nupkg.ps1 -Tag <release tag>      # a release's package, from this repository's releases
#   Scripts/fetch-nupkg.ps1 -From <path to .nupkg>  # a package you already have
#
# -Tag needs no GitHub CLI: a public release answers anonymously over GitHub's REST API. GH_TOKEN or
# GITHUB_TOKEN, when set, is sent with the requests, so a CI runner sharing an address stays under the
# anonymous rate limit.
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
    $api = "https://api.github.com/repos/$repo/releases"
    $headers = @{ 'User-Agent' = 'ComOfficeKit-fetch-nupkg'; 'Accept' = 'application/vnd.github+json' }
    $token = if ($env:GH_TOKEN) { $env:GH_TOKEN } else { $env:GITHUB_TOKEN }
    if ($token) { $headers['Authorization'] = "Bearer $token" }
    # Windows PowerShell 5.1 may still offer only TLS 1.0/1.1 by default; GitHub needs 1.2.
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    try {
        $release = Invoke-RestMethod -UseBasicParsing -Uri "$api/tags/$Tag" -Headers $headers
    } catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        if ($status -ne 404) { throw "could not ask GitHub for release ${Tag}: $($_.Exception.Message)" }
        Write-Host "no such release: $Tag. Published releases:"
        try {
            # Windows PowerShell 5.1 passes the whole JSON array down a pipeline as one object: loop over it instead.
            $list = Invoke-RestMethod -UseBasicParsing -Uri "${api}?per_page=10" -Headers $headers
            foreach ($r in $list) { Write-Host "  $($r.tag_name)" }
        } catch { }
        exit 1
    }
    $asset = @($release.assets | Where-Object { $_.name -like 'ComOfficeKit.*.nupkg' })
    if ($asset.Count -ne 1) { throw "release $Tag carries $($asset.Count) packages; expected one" }
} elseif (-not (Test-Path $From) -or -not $From.EndsWith('.nupkg')) {
    throw "not a .nupkg: $From"
}

New-Item -ItemType Directory -Force $packages | Out-Null
Get-ChildItem $packages -Filter 'ComOfficeKit.*.nupkg' | Remove-Item -Force
if ($Tag) {
    $target = Join-Path $packages $asset[0].name
    # The release page's own link: anonymous, and not counted against the API's rate limit.
    Invoke-WebRequest -UseBasicParsing -Uri $asset[0].browser_download_url -Headers @{ 'User-Agent' = $headers['User-Agent'] } -OutFile $target
    $got = (Get-Item $target).Length
    if ($got -ne $asset[0].size) { throw "the download of $($asset[0].name) is $got bytes; the release says $($asset[0].size)" }
} else {
    Copy-Item $From $packages
}

$package = @(Get-ChildItem $packages -Filter 'ComOfficeKit.*.nupkg')
if ($package.Count -ne 1) { throw "expected one package in packages/, found $($package.Count)" }
$version = $package[0].Name -replace '^ComOfficeKit\.', '' -replace '\.nupkg$', ''
$cache = ((dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', '').Trim()
Remove-Item -Recurse -Force (Join-Path $cache "comofficekit\$version") -ErrorAction SilentlyContinue
Write-Host "packages/$($package[0].Name) ($([math]::Round($package[0].Length / 1MB, 1)) MB): ComOfficeKit $version"
