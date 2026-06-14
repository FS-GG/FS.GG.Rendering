# FS.GG.Rendering

The **rendering/runtime** repository of the [FS-GG](https://github.com/FS-GG)
project: an F# desktop UI framework that renders [Elmish](https://elmish.github.io/elmish/)
(Model-View-Update) applications with [SkiaSharp](https://github.com/mono/SkiaSharp)
over **OpenGL (GL)**.

It owns the product end-to-end — scene and drawing primitives, layout, input, the
Skia viewer/host, Elmish integration, controls, the design-system and theme layers,
testing helpers, packages, docs, and templates. It builds, tests, documents, packs,
and releases with **standard [Spec Kit](https://github.com/github/spec-kit)** and
normal .NET tooling, with no dependency on any external governance platform.

## Status

Active. This repository was split out of the archived
[`EHotwagner/FS-Skia-UI`](https://github.com/EHotwagner/FS-Skia-UI) as a fresh start
and is being populated stage by stage per the rendering implementation plan
(R1 fresh repo → R2 product shape → R3 validation set → **R4 import source ✓** → R5 test
harness → …). As of R4 the **product source lives here**: the runtime libraries (`src/`)
build on `net10.0` and the default local test tier passes. See [`PROVENANCE.md`](PROVENANCE.md)
for what was imported and from where, and [`SKIPPED-TESTS.md`](SKIPPED-TESTS.md) for the
documented out-of-scope skips. Next: the comprehensive test harness (Stage R5).

### Build & test

```sh
dotnet build FS.GG.Rendering.slnx -c Release    # all runtime libs + local tests
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release   # default local tier (GL via X11)
```

### Rendering test harness (Stage R5, in progress)

`tests/Rendering.Harness/` is a tiered evidence CLI — a **capability, not a gate**. Every run
emits a `run.json`/`metrics.csv`/`summary.md` that declares **what it proves and what it does
not** (no overclaim), and tiers degrade cleanly when a capability is missing.

```sh
dotnet run --project tests/Rendering.Harness -- probe         # env facts (display/GL/refresh/backend)
dotnet run --project tests/Rendering.Harness -- offscreen     # T0 deterministic + T1 offscreen readback (headless)
dotnet run --project tests/Rendering.Harness -- perf --mode throughput --frames 100   # T3 offscreen render throughput
```

Working today (headless, CI-tested): **probe**, **T0/T1 offscreen**, **T3 offscreen
render-throughput** (real per-frame timing, honestly **not** vsync-faithful). The pure
overclaim/degradation core is unit-tested in `tests/Rendering.Harness.Tests`. Live tiers
(**T2** X11 window + input, **faithful vsync perf**, **T-uinput**) and the input backends are
pending — they need a live desktop session / kernel `uinput` not available in CI; the CLI
reports those subcommands as pending rather than faking a pass. See
[`docs/harness/capability-baseline.md`](docs/harness/capability-baseline.md).

## Project layout (FS-GG org)

| Repo | Role |
|---|---|
| **FS.GG.Rendering** (this) | Rendering/runtime product — active. |
| FS.GG.Governance | Optional governance/tooling — later, developed independently. |
| [.github](https://github.com/FS-GG/.github) | Org profile and cross-repo split/migration docs. |

## Working in this repo

- Feature workflow: standard Spec Kit (`specify → plan → tasks → implement`).
- Project rules live in [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
  (v1.0.0): contract-first `Spec → .fsi → semantic tests → implementation`,
  visibility in `.fsi`, idiomatic simplicity, Elmish/MVU for stateful/I-O work,
  mandatory test evidence, and observable/safe failure.

## License

[MIT](LICENSE) © 2026 EHotwagner
