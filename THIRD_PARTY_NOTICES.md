# Third-Party Notices

## Ghostscript

PDF Compressor uses **Ghostscript** as a separate command-line PDF processing engine.

- Project: https://www.ghostscript.com/
- Upstream releases/source: https://github.com/ArtifexSoftware/ghostpdl-downloads/releases
- Release pinned for the v1.0 build pipeline: **Ghostscript 10.08.0**
- License: **GNU Affero General Public License v3 or later (AGPL-3.0-or-later)** for the open-source release.

PDF Compressor does not claim ownership of Ghostscript.

### Redistribution

The integrated Windows Setup redistributes Ghostscript runtime files. Release builds should make the corresponding Ghostscript source available from the same release location. The GitHub workflow in this repository downloads the matching source archive and publishes it as a build/release artifact.

For the exact upstream license terms, retain the license/documentation shipped with the Ghostscript runtime and consult Artifex's official licensing information.

## PDF Compressor

PDF Compressor's own source code is licensed under **AGPL-3.0-or-later**. See `LICENSE`.

This file is informational and is not legal advice.
