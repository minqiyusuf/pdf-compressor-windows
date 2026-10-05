$ErrorActionPreference = "Stop"

$version = "10.08.0"
$tag = "gs10080"
$installer = "gs10080w64.exe"
$installerSha256 = "52a91b8bf09298788d7a57b9206127026c23eacd75405f0a131e26dc381dce50"
$sourceArchive = "ghostscript-10.08.0.tar.xz"
$sourceSha256 = "c20492bc8ebb96c87fa2e52a0926e1cda8cde95d66145e018ac713fed5da38cf"

$root = Split-Path -Parent $PSScriptRoot
$cache = Join-Path $root "_build\ghostscript-download"
$extract = Join-Path $root "_build\ghostscript-extracted"
$sourceOut = Join-Path $root "dist\third-party-source"

New-Item -ItemType Directory -Force -Path $cache, $extract, $sourceOut | Out-Null

$installerPath = Join-Path $cache $installer
$sourcePath = Join-Path $sourceOut $sourceArchive

$installerUrl = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/$tag/$installer"
$sourceUrl = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/$tag/$sourceArchive"

Write-Host "Downloading Ghostscript $version Windows runtime..."
Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath

$actual = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $installerSha256) {
    throw "Ghostscript installer SHA-256 mismatch. Expected $installerSha256, got $actual"
}

Write-Host "Downloading corresponding Ghostscript source..."
Invoke-WebRequest -Uri $sourceUrl -OutFile $sourcePath

$actualSource = (Get-FileHash $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSource -ne $sourceSha256) {
    throw "Ghostscript source SHA-256 mismatch. Expected $sourceSha256, got $actualSource"
}

$sevenZipCandidates = @(
    "7z.exe",
    "C:\Program Files\7-Zip\7z.exe",
    "C:\Program Files (x86)\7-Zip\7z.exe"
)
$sevenZip = $null
foreach ($candidate in $sevenZipCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $sevenZip = (Get-Command $candidate).Source
        break
    }
    if (Test-Path $candidate) {
        $sevenZip = $candidate
        break
    }
}
if (-not $sevenZip) {
    throw "7-Zip was not found on the build runner."
}

if (Test-Path $extract) {
    Remove-Item $extract -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $extract | Out-Null

Write-Host "Extracting official Ghostscript installer..."
& $sevenZip x $installerPath "-o$extract" -y | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "7-Zip could not extract the Ghostscript installer."
}

$gsExe = Get-ChildItem -Path $extract -Filter "gswin64c.exe" -Recurse | Select-Object -First 1
if (-not $gsExe) {
    throw "gswin64c.exe was not found after extracting the official installer."
}

$binDir = Split-Path -Parent $gsExe.FullName
$gsRoot = Split-Path -Parent $binDir

Write-Host "Ghostscript runtime root: $gsRoot"

if ($env:GITHUB_ENV) {
    "GHOSTSCRIPT_ROOT=$gsRoot" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    "GHOSTSCRIPT_SOURCE_ARCHIVE=$sourcePath" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
} else {
    $env:GHOSTSCRIPT_ROOT = $gsRoot
    $env:GHOSTSCRIPT_SOURCE_ARCHIVE = $sourcePath
}

Write-Host "Ghostscript preparation complete."
