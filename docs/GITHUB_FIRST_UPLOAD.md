# First GitHub upload

## Suggested repository

```text
pdf-compressor-windows
```

## Upload sequence

From the extracted project directory:

```text
git init
git add .
git commit -m "Initial open-source release"
git branch -M main
git remote add origin <YOUR_GITHUB_REPOSITORY_URL>
git push -u origin main
```

Then open the repository's **Actions** tab and run/check:

```text
Build unsigned Windows installer
```

Do not enable the SignPath workflow until the SignPath organization/project/slug values are configured.

## Before applying to SignPath

Confirm that the public repository visibly contains:

- source code
- `LICENSE`
- `README.md`
- reproducible build workflow
- third-party notices
- Ghostscript source/redistribution documentation
