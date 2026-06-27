# T020 / T021 — Honest, diagnosable failure (SC-005, FR-005) — live

## T020 — build.fsx failure path names engine + feed/path (no edit needed)

The generated `build.fsx` resolution path already emits a loud, named diagnostic before `run` is ever
called (`template/base/build.fsx:131-135`):

```
FS.GG.UI.Build <version> could not be restored to <cache path>. Ensure the version exists on a
configured feed (`dotnet restore`).
```

It names **both** the engine identity (`FS.GG.UI.Build <version>`) and the cache path/feed searched,
and frames the cause as a feed condition — so no message change was required.

## T021 — Live honest-failure validation

Generated a `governed` product, re-pinned `FsSkiaUiVersion=0.1.99-preview.999` (absent from every
feed), ran `dotnet fsi build.fsx target EvidenceGraph`:

- **Exit code: 1** (no fabricated success).
- **Message (verbatim):**
  ```
  System.Exception: FS.GG.UI.Build 0.1.99-preview.999 could not be restored to
  /home/developer/.nuget/packages/fs.gg.ui.build/0.1.99-preview.999/lib/net10.0/FS.GG.UI.Build.dll.
  Ensure the version exists on a configured feed (`dotnet restore`).
  ```

The failure **names the engine** (`FS.GG.UI.Build 0.1.99-preview.999`) and **the path/feed** searched,
is diagnosable without reading the build script source, and is clearly a framework/feed condition
("Ensure the version exists on a configured feed") — **not** a defect in the developer's generated
product (US3 #2, SC-005).

Note on the engine-internal honest-fail path (a *present, malformed* product artifact): covered by the
`tests/Build.Tests` semantic test "EvidenceAudit honest-fail …" — `run` returns non-0 and writes
`evidence-audit.md` with `verdict=FAIL`, `failure-class=product-evidence-defect`, and the
framework/feed-vs-product-defect clarification.
