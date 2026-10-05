# PDF Compressor

A small Windows utility for **local PDF compression with target-size control**.

PDF Compressor is designed for ordinary documents as well as scanned books and archival material. It provides a batch queue, a quality-protection strategy, and an optional size-first strategy when a hard upload limit must be met.

## Features

- Fully local processing — PDFs are not uploaded.
- Batch task queue.
- Target output size in MB.
- **Quality-first** and **Size-first** strategies.
- **Ancient book / scanned document** mode.
- Default PDF output.
- Optional JPEG / PNG page export.
- Drag-and-drop PDF files.
- Remembers the last-used settings.
- Integrated Windows Setup can bundle a pinned Ghostscript runtime.
- Normal Windows uninstall entry.

### Quality-first

For scanned/archival PDFs, Quality-first stops at a readability protection threshold rather than forcing the file below the requested target at any cost.

For the current scanned-book profile, the lower protection range is approximately:

- color/gray layers: ~180 DPI
- monochrome/text image layers: ~200 DPI

If that protection line is reached first, the task reports that the protected result is still larger than the requested target.

### Size-first

Size-first is intended for strict upload limits. It may use substantially stronger downsampling and can visibly reduce image quality.

## Privacy

PDF Compressor performs document processing locally on the Windows computer. The application itself does not upload PDFs to a remote service.

## Requirements

### End users

The integrated Setup build is intended for 64-bit Windows 10/11 and includes the Ghostscript runtime used by the application.

### Developers

- Windows
- .NET Framework C# compiler (`csc.exe`)
- Ghostscript for local integrated builds, or the CI preparation script in this repository.

## Build locally

If Ghostscript is already installed on the development PC, run:

```text
BUILD_LOCAL_SETUP.cmd
```

The builder checks common Ghostscript locations and also supports the `GHOSTSCRIPT_ROOT` environment variable.

The final installer is written to:

```text
dist\PDF_Compressor_Setup_1.0.0.exe
```

## GitHub Actions

`.github/workflows/build.yml` builds the integrated unsigned installer on a GitHub-hosted Windows runner.

The workflow pins Ghostscript **10.08.0**, verifies SHA-256 before use, and also downloads the matching Ghostscript source archive so the corresponding source can be distributed alongside the runtime.

## SignPath

This repository includes SignPath artifact configurations and a disabled signing-workflow template:

```text
.github/workflows/signpath-release.yml.disabled
```

After the project has been accepted/configured in SignPath, follow `docs/SIGNPATH.md` and rename the template to `.yml`.

## Licensing

PDF Compressor is licensed under **GNU AGPL v3 or later**. See `LICENSE`.

Ghostscript is a third-party project with its own copyright and licensing. See `THIRD_PARTY_NOTICES.md` and `docs/GHOSTSCRIPT.md`.

## Security / authenticity

Unsigned developer builds may trigger Microsoft Defender SmartScreen. The intended public release path is:

```text
public GitHub source
→ GitHub-hosted build
→ SignPath signing
→ signed release installer
```

See `docs/RELEASE.md`.
