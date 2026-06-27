# Coherence evidence — US1 gate (SC-001, SC-003)

**Pinned `FsSkiaUiVersion` = `0.1.50-preview.1`** (the single FS.GG.UI version literal; not `0.1.0-preview.1`).

All four supported profiles were generated from the template, restored, built, exercised for
evidence, and governance-tested against the **one** coherent pinned set. **All four GREEN.**

| Profile | restore (CV-1) | build (CV-2) | evidence (CV-3) | governance (CV-3) | one literal (CV-4) | no phantom IDs (CV-5) |
|---------|:---:|:---:|:---:|:---:|:---:|:---:|
| `headless-scene` | ✅ exit 0 | ✅ exit 0 | scene+layout ok | ✅ 4/4 | ✅ | ✅ |
| `governed` | ✅ exit 0 | ✅ exit 0 | scene+layout ok | ✅ 5/5 | ✅ | ✅ |
| `app` | ✅ exit 0 | ✅ exit 0 | scene+layout + **live launch + PNG screenshot** | ✅ 30/30 | ✅ | ✅ |
| `sample-pack` | ✅ exit 0 | ✅ exit 0 | scene+layout + **live launch + PNG screenshot** | ✅ 29/29 | ✅ | ✅ |

Per-profile detail: [profile-headless-scene.md](profile-headless-scene.md),
[profile-governed.md](profile-governed.md), [profile-app.md](profile-app.md),
[profile-sample-pack.md](profile-sample-pack.md).

## Assertions (the US3 `## Response` links this file)

- **Restore-ok ∧ build-ok ∧ evidence-ok under the single pin** — yes, all four profiles (table above).
- **Exactly one FS.GG.UI version literal** — `0.1.50-preview.1`; every resolved `FS.GG.UI.*` reference
  is that version. No `0.1.0-preview.1` anywhere in any resolved graph (SC-003).
- **No phantom IDs** — `FS.GG.UI.Color` and `FS.GG.UI.SkillSupport` appear in **no** resolved set; the
  two phantom `<PackageVersion>` pins (and their false comments) were removed from
  `template/base/Directory.Packages.props` (T004). The 16-real-ID set is the coherent snapshot
  (see [../contracts/snapshot-manifest.md](../contracts/snapshot-manifest.md)).
- **Live screenshot evidence** — `app` and `sample-pack` produced real launch + `--image-evidence`
  PNGs under `DISPLAY=:1` (`first-frame-presented=true`); **no** environment-limited substitution was
  needed (the headless evidence rules' degraded path did not apply).

## Verdict

US1 holds — the contract is **coherent** under `0.1.50-preview.1`. This is the MVP and the evidence
US2 (snapshot) records and US3 (cross-repo reconciliation) reports.
