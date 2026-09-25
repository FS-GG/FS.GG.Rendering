# Pinned Rendering skill manifest capture

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/ManifestCaptureTests.fsx
```

`SkillStagingManifestCapture.fsx` opens the repository root and the fixed
`template/skill-manifest/skill-manifest.json` chain through held Linux directory
descriptors. Every child open uses `O_NOFOLLOW`; the manifest must be regular,
and two reads with descriptor metadata checks must agree. The captured raw
bytes are passed directly to the #1336 file plan. A fresh pinned capture plus
fresh product-source observation can refuse a stale plan before later use.

The test reproduces foreign bytes from a pathname probe/read swap, then checks
before-open link refusal, original bytes after a held-file or held-parent swap,
in-place mutation refusal, stale-plan refusal, symlink and FIFO refusal, and
the committed 20-product catalog. All fixture writes are under temporary
directories; no staging output is created.

The manifest descriptor and product-root descriptors are opened at different
times. A source may change after the last check, and bounded double reads do
not prove an atomic snapshot against adversarial ABA. The stacked directory
plan records empty directory paths and source modes; the stacked output mode
contract projects a fresh manifest mode under an explicit umask. Staged mode
parity remains unproved. #1332
Python preflight, #1334/#1335 source acceptance, and installed-package parity
remain separate gates. No package publication, release/BOM update, merge, or
receiver pin is authorized.
