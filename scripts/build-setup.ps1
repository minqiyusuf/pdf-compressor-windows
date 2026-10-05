param(
    [Parameter(Mandatory=$true)]
    [string]$PayloadZip,

    [string]$OutputExe = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $PayloadZip)) {
    throw "Payload ZIP not found: $PayloadZip"
}

if (-not $OutputExe) {
    $dist = Join-Path $root "dist"
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    $OutputExe = Join-Path $dist "PDF_Compressor_Setup_1.0.0.exe"
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "csc.exe not found."
}

Write-Host "Building Setup from payload: $PayloadZip"

& $csc /nologo /target:winexe /optimize+ `
    "/win32manifest:$root\installer\setup.manifest" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
    "/resource:$PayloadZip,Payload.zip" `
    "/out:$OutputExe" `
    "$root\installer\SetupInstaller.cs" "$root\installer\SetupAssemblyInfo.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Setup compilation failed."
}

Write-Host "Setup created: $OutputExe"
