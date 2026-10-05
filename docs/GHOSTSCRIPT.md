# Ghostscript in the release build

PDF Compressor uses Ghostscript as a separate command-line engine and the integrated Setup redistributes a pinned runtime.

## Pinned release

**Ghostscript 10.08.0**

Windows x64 installer:

```text
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10080/gs10080w64.exe
```

SHA-256:

```text
52a91b8bf09298788d7a57b9206127026c23eacd75405f0a131e26dc381dce50
```

Corresponding source archive:

```text
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10080/ghostscript-10.08.0.tar.xz
```

SHA-256:

```text
c20492bc8ebb96c87fa2e52a0926e1cda8cde95d66145e018ac713fed5da38cf
```

The CI preparation script verifies these hashes before using the files.

## Why the source archive is published

The integrated installer redistributes Ghostscript object/runtime code. The release process therefore keeps the corresponding upstream source available alongside the installer.

## Updating Ghostscript

A Ghostscript upgrade must be a deliberate source change:

1. Change the version/tag/file names in `scripts/prepare-ghostscript.ps1`.
2. Replace both SHA-256 values using the official upstream release information.
3. Update this document.
4. Build and test compression behavior again.
5. Publish the matching source archive with the new release.
