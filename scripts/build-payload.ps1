param(
    [string]$OutputZip = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $root "_build"
$stage = Join-Path $build "payload"
if (-not $OutputZip) {
    $OutputZip = Join-Path $build "Payload.zip"
}

if (-not $env:GHOSTSCRIPT_ROOT) {
    throw "GHOSTSCRIPT_ROOT is not set. Run scripts/prepare-ghostscript.ps1 first."
}

$gsExe = Join-Path $env:GHOSTSCRIPT_ROOT "bin\gswin64c.exe"
if (-not (Test-Path $gsExe)) {
    throw "Invalid GHOSTSCRIPT_ROOT: $env:GHOSTSCRIPT_ROOT"
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "csc.exe not found."
}

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Write-Host "Compiling PDF_Compressor.exe..."
& $csc /nologo /target:winexe /optimize+ `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    "/out:$stage\PDF_Compressor.exe" `
    "$root\src\PDF_Compressor.cs" "$root\src\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw "PDF_Compressor.exe compilation failed." }

Write-Host "Compiling Uninstall.exe..."
& $csc /nologo /target:winexe /optimize+ `
    "/win32manifest:$root\installer\uninstall.manifest" `
    /r:System.Windows.Forms.dll `
    "/out:$stage\Uninstall.exe" `
    "$root\installer\Uninstall.cs" "$root\installer\UninstallAssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw "Uninstall.exe compilation failed." }

Write-Host "Copying Ghostscript runtime..."
Copy-Item $env:GHOSTSCRIPT_ROOT (Join-Path $stage "Ghostscript") -Recurse -Force

Copy-Item "$root\LICENSE" "$stage\LICENSE" -Force
Copy-Item "$root\THIRD_PARTY_NOTICES.md" "$stage\THIRD_PARTY_NOTICES.md" -Force
Copy-Item "$root\README.md" "$stage\README.md" -Force
Copy-Item "$root\src\PDF_Compressor.cs" "$stage\PDF_Compressor_SOURCE.cs" -Force

@"
Ghostscript 10.08.0 corresponding source:
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10080/ghostscript-10.08.0.tar.xz

SHA-256:
c20492bc8ebb96c87fa2e52a0926e1cda8cde95d66145e018ac713fed5da38cf

The release workflow publishes the source archive alongside the Windows installer.
"@ | Set-Content "$stage\GHOSTSCRIPT_SOURCE_INFO.txt" -Encoding UTF8

if (Test-Path $OutputZip) { Remove-Item $OutputZip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stage, $OutputZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Write-Host "Payload created: $OutputZip"
