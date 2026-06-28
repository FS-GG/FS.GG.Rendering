# T019 — Cross-repo contract coherence (Feature 212, FR-011 / contract C5)

Recorded via the `cross-repo-coordination` skill, 2026-06-28.

## Tracker

- **FS-GG/FS.GG.Rendering#9** — "H1 · rendering — Emit root .slnx + Directory.Build.props +
  global.json + build.sh wrapper in fs-gg-ui template; release test asserts dotnet build/test at
  root" (state: OPEN; labels: `contract-change`, `roadmap`).
- Update comment posted:
  https://github.com/FS-GG/FS.GG.Rendering/issues/9#issuecomment-4825806963

## Registry / compatibility update (ADR-0001)

`contract-change` to `fs-gg-ui-template`, recorded in `FS-GG/.github`:

- **`registry/dependencies.yml`**:
  - `root-buildable` sub-key added to the `fs-gg-ui-template` contract entry (surface:
    root `<Name>.slnx` + `global.json` net10 band + `build.sh`/`build.cmd` verb wrapper; stock
    `dotnet build/test/run`; tracker #9). Additive — no behavior break.
  - new coherence entry **`fs-gg-ui-template-root-build`** (`coherent: true`, owner rendering,
    tracker #9) summarizing the live evidence.
- **`docs/registry/compatibility.md`**:
  - versioned-contracts row for `fs-gg-ui-template` notes the root-buildable surface (since F212).
  - new coherence-state table row `fs-gg-ui-template-root-build`.

PR: **FS-GG/.github#25** —
https://github.com/FS-GG/.github/pull/25 (branch
`feature-212-fs-gg-ui-template-root-build`, label `contract-change`).

## Consumer impact

SDD's composition-acceptance probes (FS-GG/.github#16, "uniform build" pillar) can now drive a
generated product through stock, declared-or-default `dotnet build/test/run` at the product root with
no FAKE knowledge. A future change that breaks root buildability is caught at template release time
by the `template-product-tests` gate.
</content>
