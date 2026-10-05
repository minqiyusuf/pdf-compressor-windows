# SignPath setup guide

The repository is structured for SignPath Foundation open-source signing, but the signing workflow is intentionally disabled until a SignPath project is approved and configured.

## 1. Public repository

Push this project to a public GitHub repository.

Recommended repository name:

```text
pdf-compressor-windows
```

Keep `LICENSE`, source code, build scripts and third-party notices public.

## 2. Apply/configure SignPath

After the open-source project is accepted:

1. Add/link the SignPath GitHub.com Trusted Build System.
2. Install the SignPath GitHub App for the repository as required by SignPath origin verification.
3. Create/link a SignPath project for this repository.
4. Create/import these two artifact configurations:
   - `payload` from `.signpath/artifact-configurations/payload.xml`
   - `setup` from `.signpath/artifact-configurations/setup.xml`
5. Create a signing policy such as:
   - `release-signing`

The template currently assumes the project slug:

```text
PDF_Compressor
```

If SignPath assigns a different slug, edit the workflow.

## 3. GitHub repository settings

Add this GitHub Actions secret:

```text
SIGNPATH_API_TOKEN
```

Add this GitHub Actions variable:

```text
SIGNPATH_ORGANIZATION_ID
```

## 4. Enable the workflow

Rename:

```text
.github/workflows/signpath-release.yml.disabled
```

to:

```text
.github/workflows/signpath-release.yml
```

## 5. Two-stage signing

The workflow is intentionally two-stage:

1. Build `PDF_Compressor.exe` and `Uninstall.exe`.
2. Put them in `Payload.zip`.
3. Send the payload artifact to SignPath so the inner executables are Authenticode-signed.
4. Build the final Setup using the signed payload.
5. Send the final Setup to SignPath and Authenticode-sign the outer installer.

This avoids the common problem where only the installer is signed but the installed application itself is unsigned.

## 6. Release tags

After signing works, use tags such as:

```text
v1.0.0
v1.0.1
```

The template runs on `v*` tags and can also be started manually.

## Important

SignPath project slugs, signing-policy slugs and artifact-configuration slugs must exactly match the values configured in your SignPath organization.
