# T011 — Publish run (FR-006) — GREEN

**Captured**: 2026-06-29.

Release triggered by pushing `v0.1.53-preview.1` at `main` HEAD (`55e5967`).

**Run**: https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28404668485 — **conclusion: success**

| Job | Result |
|---|---|
| Generated product (template instantiation, release-only) | 🟢 success |
| Package consumption (release-only) | 🟢 success |
| **Publish FS.GG.UI.\* to org GitHub Packages (release-only)** | 🟢 success |

✅ The release-only gates (`template-product-tests`, `package-tests`) passed **before** publish, and
the whole coherent set was packed at one `-p:Version=0.1.53-preview.1` and pushed to
`nuget.pkg.github.com/FS-GG`. No assertion weakened (Principle V).
