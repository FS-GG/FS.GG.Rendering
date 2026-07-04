# Readiness evidence — Feature 241 (runtime ergonomics polish)

Date: 2026-07-04. Branch `241-runtime-ergonomics`. dotnet 10.0.301.

## Summary

| Item | Verified | Evidence |
|---|---|---|
| §3.5 `Cmd.none`/`Sub.none` exist + laws | ✅ real | FSI + Expecto (4/4) |
| §3.5 surface baseline drift gate | ✅ real | `SurfaceAreaTests` 40/40 green with +2 baseline lines |
| §3.4 collision guidance present | ✅ real | `grep KeyboardMsg product.md` hits |
| §3.6 `measureText` surfaced | ✅ real | `grep measureText product.md` + `fs-gg-scene` skill hit; helper already packed |
| skill-manifest digests | ✅ real | regen + `--check` up to date; `Feature231/224/225` green |
| Full Package.Tests | ✅ real | 187/187 |
| Full Elmish.Tests | ✅ real | 219 pass / 17 pre-existing skips |
| **Live scaffold + build + test of a `game` product** | ✅ **real** (via local feed @0.1.63-preview.1) | fresh scaffold builds green (main+tests), product tests 26/26 |
| §3.5 in a scaffolded product | ✅ real | generated `Model.fs` has 6× `Cmd.none`; product compiles |
| §3.6 `measureText` in a scaffolded product | ✅ real | `(Scene.measureText "SCORE 0" font).Width` compiles in-product |
| **Bug caught + fixed by the live build** | ✅ real | see below |

## §3.5 library + laws (real)

`FS.GG.UI.Controls.Elmish.Authoring.{Cmd,Sub}.none` added. FSI against the freshly built
`bin/Debug` assembly:

```
Cmd.none = [] : true
Sub.none empty : true
productMessages Cmd.none = [] : true
```

Expecto `adapter no-op aliases (§3.5)` — 4/4 passed (`tests/Elmish.Tests/AdapterCmdSubNoneTests.fs`).
Resolution proof: opening `FS.GG.UI.Controls.Elmish` + `…Authoring` (the exact opens added to the
product template) makes `Cmd.none`/`Sub.none` resolve unambiguously — the package's own `open Elmish`
is NOT in a generated product's scope, so there is no Fable-`Elmish.Cmd` clash.

## Surface + gates (real)

- `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` gained exactly
  `FS.GG.UI.Controls.Elmish.Authoring.Cmd` and `…Authoring.Sub` (`git diff` = +2 lines; no other
  baseline drifted). `SurfaceAreaTests` 40/40.
- Skill bodies `fs-gg-scene` + `fs-gg-elmish` edited → `skill-manifest.json` regenerated (2 digests
  changed) → `generate-skill-manifest.fsx --check` = up to date. `Feature231/224/225` green.
- Full `Package.Tests` 187/187; full `Elmish.Tests` 219 pass / 17 skip (pre-existing GL/env skips).

## Live scaffold+build+test (done — packed coherent set to local feed)

Packed the whole coherent set at a fresh **0.1.63-preview.1** to the local feed
(`dotnet fsi scripts/dev-repack.fsx --sample samples/CanvasDemo --version 0.1.63-preview.1`; 17
`FS.GG.UI.*` packages, incl. `Controls.Elmish` carrying `Authoring`). Then scaffolded a `game`
product from the **live repo template** (`dotnet new install . --force` so the local `template/base`
edits win over the released nupkg), pinned it to 0.1.63-preview.1, and built + tested it.

- **Feed routing gotcha (env-specific):** the global `~/.nuget/NuGet/NuGet.Config`
  `packageSourceMapping` routes `FS.GG.*` to **nuget.org only** (`local-feed` never serves `FS.GG.*`).
  A scaffolded product therefore needs a product-level `nuget.config` that maps `FS.GG.*` → the local
  feed, else the locally-packed 0.1.63 is "not considered." (Recorded so a future local scaffold
  doesn't lose an hour to it.)
- **Result:** a fresh scaffold from the fixed template **builds green (main + tests)** and the
  generated product's own tests pass **26/26**. `Model.fs` carries 6× `Cmd.none`; EvidenceCommands is
  clean.

## Bug the live build caught (and the fix)

The first live build FAILED with `EvidenceCommands.fs: error FS0001: The type 'ViewerEffect' does not
match the type 'AdapterEffect<'a>'`. Root cause: the host records `generatedHost`/`interactiveHost`
`Init` return a **`ViewerEffect list`**, NOT an `AdapterCommand<'msg>` — so my `[] → Cmd.none`
substitution in `EvidenceCommands.fs` was a type error. **Fix:** §3.5 is scoped to `Model.fs`'s
`update`/`subscriptions` (which genuinely are `AdapterCommand`/`AdapterSubscription`); the
`EvidenceCommands.fs` host-`Init` `[]` are reverted. Confirmed by a fresh scaffold building green.
This is exactly the class of error the library-only tests could not see (the template is not compiled
by `Package.Tests`).

## §3.4 note (honest)

The §3.4 collision is doc-only and real, but **context-dependent**: a minimal repro with the
consumer's `KeyDown` defined adjacent to its use compiles fine (the local type definition wins F#
resolution). The `does not match 'ViewerKey'` failure needs the feedback's cross-file structure (a
`mapKey` that opens `FS.GG.UI.KeyboardInput` without the consumer's `Msg.KeyDown` as the nearest
binding). No clean minimal live repro was produced; the `product.md` guidance (qualify
`KeyboardMsg.KeyDown` / don't open unqualified) is present and the qualified form compiles.

## Release note

The template now consumes `FS.GG.UI.Controls.Elmish.Authoring`, which ships in **0.1.63+**. The
template still pins 0.1.62 on this feature branch (feature PRs don't bump versions in this repo — a
separate `release:` PR moves the coherent set, per #79/#80/#81). A release bump to **≥ 0.1.63** is
required before a default-feed scaffold builds. Verified working at 0.1.63-preview.1 via the local
feed above.
