# Release process

## Development / local testing

On a Windows development PC with Ghostscript installed:

```text
BUILD_LOCAL_SETUP.cmd
```

Result:

```text
dist\PDF_Compressor_Setup_1.0.0.exe
```

This local build is **unsigned** unless you separately sign it.

## CI build

The normal GitHub Actions build:

```text
.github/workflows/build.yml
```

performs these steps on `windows-latest`:

1. Download pinned Ghostscript Windows runtime.
2. Verify its SHA-256.
3. Download the matching Ghostscript source archive.
4. Verify the source archive SHA-256.
5. Extract the runtime.
6. Compile PDF Compressor and the uninstaller.
7. Package the integrated payload.
8. Compile the Setup.
9. Upload the unsigned Setup artifact.
10. Upload the corresponding Ghostscript source artifact.

## Signed public release

After SignPath configuration, enable:

```text
.github/workflows/signpath-release.yml
```

The signed workflow signs both:

- installed application binaries
- final Setup executable

For a public release, publish the signed Setup and the corresponding Ghostscript source archive together.

## Version updates

For a new PDF Compressor version, update at minimum:

- `VERSION`
- assembly versions
- Setup output file name
- workflow path/file names where version is explicit
- release notes/tag

For a Ghostscript update, follow `docs/GHOSTSCRIPT.md`.

## SmartScreen

Authenticode signing identifies the publisher and protects integrity, but a new publisher/application may still need to build Microsoft SmartScreen reputation over time.
