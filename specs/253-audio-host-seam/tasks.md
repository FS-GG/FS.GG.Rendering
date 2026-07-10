# Tasks: The audio host seam

**Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Issue**: FS.GG.Rendering#245

All tasks landed. Statuses are the record of what shipped, not a forecast.

## Phase 1 — the seam (FR-001, FR-003, FR-005)

- [X] **T001** Pin `FS.GG.Audio.Core` 0.1.0 in `Directory.Packages.local.props`. NOT in the root
      `Directory.Packages.props` — that file is org-synced from `FS-GG/.github` and says so.
- [X] **T002** `PackageReference` it from `src/SkiaViewer/SkiaViewer.fsproj`. Core only; Host/Engine
      stay out of the rendering core.
- [X] **T003** `ViewerEffect` gains `PlayAudio of effects: AudioEffect list` in `Viewer.Types.fs(i)`.
- [X] **T004** Extract the generated-app launch body to a private `runGeneratedApp` taking an
      `audioSink`; interpret `PlayAudio` by handing the batch to it.
- [X] **T005** Re-express `runApp` / `runAppWithWindowBehavior` as `runGeneratedApp ... ignore`, so
      FR-005 holds by construction. Add `runAppWithAudio` and `runAppWithWindowBehaviorAndAudio`.
- [X] **T006** The interactive (pointer/size-aware) host discards `PlayAudio`, as it discards the
      other effects it does not interpret. Commented in place, so it reads as a decision.

## Phase 2 — evidence (FR-006, SC-001)

- [X] **T007** `GeneratedAppHost.audioRequests : ViewerEffect list -> AudioEffect list` — flatten in
      dispatch order, drop non-audio. Pure, so a product can assert a frame with no window or device.
- [X] **T008** `tests/SkiaViewer.Tests/Issue245AudioSeamTests.fs`: flattening, ordering, the empty
      frame, the init batch, and the `audioRequests |> Audio.interpret` round-trip to `AudioEvidence`.
- [X] **T009** Add `FS.GG.Audio.Host` as a **test-only** pin/reference and assert the real composition
      `Audio.play backend` over `NullBackend` receives the batches in order (SC-001).
- [X] **T010** Assert `runAppWithAudio` on a window-less host fails as `runApp` does and never touches
      the sink. Skips where a display exists; the skip states its reason.

## Phase 3 — the template (FR-002, US1)

- [X] **T011** `template/base/src/Product/AudioCues.fs` — `forTransition` cue map + `resolver`.
      Replaceable; documented as such in the file header.
- [X] **T012** Register it in `Product.fsproj`, gated to `game || sample-pack`, compiled after
      `Model.fs` (it names `Msg`) and before `EvidenceCommands.fs` (which consumes it).
- [X] **T013** `EvidenceCommands.fs` lifts cues onto `PlayAudio`, profile-gated, skipping the effect
      entirely on a silent transition.
- [X] **T014** `Program.fs` creates `OpenAlBackend.create AudioCues.resolver` and passes
      `Audio.play backend` to the audio-carrying launches.

## Phase 4 — the gates the change moves (FR-007)

- [X] **T015** `src/Testing/TestingEvidence.fs`: accept the audio-carrying launches. The requirement is
      "the default branch opens the window through `generatedHost`", not one spelling.
- [X] **T016** `tests/Testing.Tests/Tests.fs`: pin that the audio launches validate **and** that a
      default branch launching nothing is still rejected — the widened check must not degenerate.
- [X] **T017** Bundle `template/base/docs/api-surface/Audio.Host/Host.fsi`; widen A-MEMBERS to resolve
      against Core ∪ Host; add a gate that the surface backing the runtime claim is bundled.
- [X] **T018** Rewrite the `fs-gg-audio` skill's "Generated Product" section: name the two files, the
      sink, the silent escape hatch (`runApp`), and the hardware-free assertion path.

## Phase 5 — mechanical coherence

- [X] **T019** `dotnet restore --force-evaluate` the touched projects. **Under a scratch
      `NUGET_PACKAGES`** — the shared cache's `fsharp.core/10.1.301` is the poisoned SDK
      `library-packs` copy, and force-evaluating against it rewrites the committed FSharp.Core
      `contentHash` in every lockfile it touches.
- [X] **T020** `scripts/refresh-surface-baselines.fsx`. Additive only: one DU case, three members.
- [X] **T021** Mirror the new members into `template/base/docs/api-surface/SkiaViewer/SkiaViewer.fsi`.
- [X] **T022** `scripts/generate-skill-manifest.fsx` — the manifest is content-addressed over the
      SKILL.md bodies, so T018 stales it.

## Phase 6 — verification

- [X] **T023** Full solution: 16 test assemblies green.
- [X] **T024** Release-only `Package.Tests` (not in the slnx): 247 green.

## Follow-up, deliberately not done here

- **FR-008** is a *constraint on spec 244*, not work in this feature: `244-persistence-effect-surface`
  is still `Status: Draft` and must not land its record-only host seam until it can point at this one.
  Persistence should follow this shape — a `ViewerEffect` case plus a caller-supplied sink — rather
  than reproduce the record-only stub on a second capability.
- **Registry edge** (issue #245, last acceptance box): record the consuming edge on the `fs-gg-audio`
  entry in `FS-GG/.github` `registry/dependencies.yml`, publish-before-flip (FR-007 of the registry
  protocol). Cross-repo, so it is a separate item under `cross-repo-coordination`.
- **Issue #247** remains open: `Audio.Engine`, `Audio.Elmish` and `Controls.Elmish` are still
  referenced-but-undocumented, and the mirror still has no gate that fails on the gap. This feature
  bundled `Audio.Host` only, because the skill's claim depends on it.
