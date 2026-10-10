# SVG-RUNTIME-01 — Rendering owner ledger

Status: complete at the source/generated-candidate boundary. SVG-RUNTIME-01.4 is qualified by its routine
repository and browser-matrix gates, and SVG-RUNTIME-01.5 is merged with exact installed-player evidence.
Publication remains owned by SVG-PREVIEW-B. Route: routine.

## SVG-RUNTIME-01.4 — Browser clock and retained projection host

`SvgSessionPolicy` is the portable boundary between browser time/resources and the authoritative Game
session. It converts increasing requestAnimationFrame timestamps to integer microseconds once, permits only
one pending projection plus one coalesced replacement, and accepts retained projections only when both their
generation and revision are current. Pause, reset, replacement, suspension, blur, visibility loss and
disposal invalidate the old generation before a late reply can mutate presentation.

`SvgSessionHost` owns the animation frame and lifecycle listeners. Its callbacks interpret policy effects
through `SessionRuntime` and `SessionOperations`; product state and fixed-step catch-up remain in Game. A
suspension gap pauses authority and asks the caller for explicit recovery rather than banking background tab
time. Presentation revisions never feed back into authoritative state.

Acceptance evidence:

- Four .NET reducer tests cover variable cadence, integer conversion, bounded projection ownership,
  monotonic retained revisions, stale generations, suspension recovery and pause/step/reset/replace/dispose.
- `tests/SvgSessionCorrespondence/run.sh` packs this repository with exact Game revision
  `996832a6ebb5c893199627b0ecc46f7c1da848cd`; .NET and Fable produce the same 299-byte transition corpus
  (`sha256:569bc5541e77ca2f67f6c531af201ad5072a596eb454f13ec7cdfe98844aefb5`).
- The packaged browser fixture exercises real RAF cadence, projection coalescing, pause, single-step, reset,
  replacement, a stale reply, blur recovery and disposal in Chromium, Firefox and WebKit. Disposal reports
  zero owned listeners, frames and requests in every family.

The host adds no Game package dependency to the portable Scene layer and no clock, DOM or renderer dependency
to Game.Core. Candidate package versions remain private pending Preview B.

## FABLE-ADOPT-01.2 — External-authority presentation source

`SvgExternalSessionPolicy` and `SvgExternalSessionHost` add an opt-in presentation boundary for products
whose simulation authority is external to the browser. The state separates local mount generation from an
opaque external epoch, gives each request a monotonic acquisition identity, accepts only increasing revisions
within that epoch, owns at most one acquisition and one replaceable queued presentation, and cancels
browser-owned work before reconnect, replacement, or disposal. Same-epoch reconnect preserves the accepted
baseline; a distinct epoch starts a new baseline.

The host emits no simulation advance or native pause, resume, reset, step, or authority-clock effects.
Commands and settlement receipts stay on the product gateway path and are outside presentation coalescing.
The source boundary includes pure reducer checks, the literate `models/svg-runtime/external-session.md`
authority with fifteen directed traces, 82 complete state rows, and four assertion-specific guard controls,
packed .NET/Fable correspondence, and the existing three-browser fixture. Callback failures and cancellation
settlement uncertainty remain visible while listener cleanup and reentrant callback fencing preserve actual
owned-resource truth. Package publication, an installed consumer, and product adoption remain later gates.

The [Templates qualification caller](../../.github/workflows/templates-external-reference-qualification.yml)
now pins both its reusable workflow and consumer checkout to protected Templates
`0d945052250eb53056a5b4a87fe742efd1b7a0a2` (Templates PR #685). That successor retains the
eight-case receipt binder and pre-acquisition static controls, and scopes command-outcome test
actions and receipt status to the intended direct reference controls. Rendering's static controls
accept this literal join and refuse both prior consumers, mismatched or floating pins, non-public
inputs, broader permissions and bypassed preflight controls.

The previous caller's [full run 38005778318 attempt 1](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/38005778318)
authenticated the original archive and compiled the generated Fable composition. Chromium passed
four existing cases; all four command-outcome cases stopped at an ambiguous button locator.
Firefox and WebKit did not run, and the successful full-result binder was skipped. That failed
attempt remains consumed and its failure evidence is retained.

After protected caller delivery and programme projection, [full run 38011120121 attempt 1](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/38011120121)
passed at exact Rendering caller `d8556ece76bc4701e9f04ececff5aaef8b0b7bda` and Templates
consumer `0d945052250eb53056a5b4a87fe742efd1b7a0a2` (tree
`7cc5a78662a75a6eb708bfb9066a42cc7a918d7a`). This consumer includes the qualified, default-false
Portal example from PR #684 as well as the reference selector correction from PR #685. The direct
qualification omits that option; the Portal subtree is excluded, with Portal adoption retaining its
own acceptance scope.

The native preflight authenticated original Rendering source
`6c9f766fdd91483c2de6f061e75589e94852a265` (tree `90b94005f308917f95eebad031bb22a97bb82e0f`),
producer run `37419955679` attempt 1 and artifact `11392773621`. The original archive SHA-256 is
`486182db3efd8442c007f66322a7bfb27caf0cb8e3c87bc2521e66dd90a7adee`; its 19-package custody
manifest SHA-256 is `ecd0c5d742fd8d19d5659991e413e92971c1d2cb9aea913faa2bb249f53f577d`.
The generated composition selects three source-bearing producer archives and public Rendering
0.32.1 inputs. Actual template generation, public locked restores, Fable/Vite builds, codec,
Studio and tactical controls passed, together with all 186 ProviderComposition assertions.

Chromium, Firefox and WebKit each passed the same eight cases once: four command-outcome cases,
three existing external-reference cases and the FourD reference case. All 24 passed with zero
skips, flaky results or unexpected failures. The final binder joined the exact consumer, original
producer custody and all three browser report hashes. Independent native artifact readback verified
[artifact 11653406338](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/38011120121/artifacts/11653406338)
SHA-256 `fd007c0119b35be7b07367b1a8c03f860e277275de6fc16a53e57d904c07cf8b`
and qualification receipt SHA-256
`857ab24886094aa4699cbf9e4eee04f56cdd8a94300a7ad5e06d9592f979e8d9`.

A separate local strict TypeScript window qualified exactly `two-client.spec.ts`,
`soldier-reference.spec.ts`, `external-command-outcomes.spec.ts` and `playwright.config.ts`
from protected Templates `20c009bc6728302f9ed06fdf49cb2e8bd19e417d` (PR #687). All six
Browser.Tests source and package inputs matched the browser-qualified `0d945052` consumer.
Node 26.10.0 and TypeScript 5.9.3 checked those four modules with `strict`, `noEmit` and
`skipLibCheck: false`, using the exact installed graph: Playwright test/runner/core 1.63.0,
Node types 26.6.5 and undici types 8.9.0. npm 12.2.0 acquired that six-package graph once
with install scripts disabled; the corrected operation reused it without npm or acquisition.

The first compiler operation stopped with five TS2488 diagnostics because its qualification
configuration omitted `DOM.Iterable`; its deliberate-error control did not run. That failed
attempt remains consumed. A separately admitted compiler-only operation added that library,
changed no source or strictness setting, passed with exit 0 and no diagnostics, and refused
one deliberate string-to-number error with exit 2 and exactly one TS2322. Before/after
readback matched the complete 464-entry installed file/symlink tree, four executable aliases
and every immutable input, including the original failed evidence. The accepted result receipt
SHA-256 is `df3344d85b46f8b9f527118909708e533d92a982c6539bdddb63ae7e6a856c40`.
All four new compiler-stage process groups and sessions were observed empty at completion;
no cleanup signal was needed. This qualifies those four modules only: full Client/Vite
TypeScript coverage and browser runtime acceptance are separate scopes.

A further strict window passed `Client/vite.config.ts`, the Client's only handwritten
TypeScript module, at the same Templates release producer `20c009bc`. Current protected
Templates `b93f82edabf7b4195b5a1d53f254532df93180ec` adds only the Portal owning-plan record;
the Client source, package manifest and lock remain byte-identical to browser-qualified
`0d945052`. The operation used Node 26.10.0, npm 12.2.0, TypeScript 5.9.3 and exact locked
Vite 7.3.6. One script-disabled, optional-dependency-omitting acquisition installed 29 Client
packages; three read-only aliases reused the qualified compiler, Node types and undici types
without reacquiring the original graph. The positive check retained `strict`, `noEmit`,
`skipLibCheck: false`, NodeNext resolution and `DOM.Iterable`: exit 0, no diagnostics. The
countercontrol exited 2 with exactly one TS2322. Readback matched the new 576-entry tree,
32 composed package roots and original 464-entry graph; all six new stage groups and sessions
were observed empty with zero cleanup signals. The accepted receipt SHA-256 is
`0ea9ba254473742f53d0312653deac108260c6e4de7d5ff1fc6df12135dc0ca7`.

Together these windows cover the five handwritten TypeScript modules. Client application
code is F# compiled by Fable to JavaScript; this compiler-only check ran no Vite, Fable,
browser or assistive technology and does not establish generated-JavaScript or full-site
semantic acceptance.

This closes this exact generated-consumer Fable/browser qualification window and the two narrow
TypeScript windows. Full-site typechecking, actual screen-reader observation, coherent publication,
fresh installed creation, preserving upgrades and product-native acceptance remain separate gates. No producer archive was
rebuilt or published; the receipt records publication and installed acceptance as false. Historical
retained operations and their unresolved ownership or cleanup remain unchanged.

## SVG-RUNTIME-01.5 — Generated continuous player and Preview-B handoff

Game producer repair PR #625 merged as `c6de5b83eaa3d3f14909b42c3f8c3c94558157c9`. Templates PR #471
merged as `4a392a18ada74a87a12dd0b31aebaa7039a84b86`, consuming Rendering revision
`50bb064c8acb0251a469ca406d193551f8e209d1` after the native gamepad bridge repair.

The exact candidate gates passed generated authoring, input and runtime players in Chromium, Firefox and
WebKit, Orca/AT-SPI, package consumers, installed typed receivers and repository composition. The continuous
player exercises real semantic keyboard, pointer, touch and gamepad movement, authoritative pause/step/reset
and lose/win/restart behavior while excluding Studio modules. Public pins remained unchanged.


## FABLE-ADOPT-01.4 — External authority successor release

- [ ] **.4-P1 — Rendering 0.32.0 publisher source preparation — route: routine.**
  Source starts at accepted external host commit `097e228388375bf27a03ac008627e4a038686c53`.
  The successor plan selects all 17 libraries, BOM and UI.Template, exact release axes and
  isolated candidate-consumer pins at 0.32.0, baseline 0.31.0, and delivered external host
  `.fs`/`.fsi` entries plus its public surface marker. The development graph version stays
  0.4.0-preview.1. Existing dependency locks describe that graph and unchanged external
  dependencies; no candidate restore or lock regeneration has been claimed.

  The independent Rendering.Skills content package also prepares patch candidate 0.2.1
  because the delivered symbology recipe pins changed; its per-file manifest digest is
  regenerated from owner source bytes. It is outside the 19-member UI coherent set and
  remains unpublished pending its own authority and installed consumer join. Front-door
  generated fragments describe source coordinates; public 0.31.0 remains explicit outside
  those regions.

  Static preflight reuses roster, custody and ApiCompat validators and extends finite
  negative fixtures before costly jobs. It refuses wrong source/version/axes/roster, missing
  external sources/surface, floating release SDK setup and pending publication readiness.
  Source-only calls skip publication credentials and feed work while retaining native
  package/generated-consumer checks, then pack the full 19-archive set through the same
  first-pack script as the publisher and retain source-labelled immutable custody. SDK 10.0.401 setup is followed by loaded host/SDK/F#
  identity evidence. The existing transaction and retry semantics are unchanged; a new
  Quint pipeline model is deferred. Initial static assessment cap: 30 minutes. Engineering
  effort, runner savings and latency savings have not been measured. Native .NET/browser,
  actual package/lock resolution and required coherent CI remain pending root admission.

- [ ] **.4-P2 — Protected coherent publication.**
  `publicationReady: false` keeps effects closed. Candidate 0.32.0 is neither reserved nor
  proven absent. Genuine Rendering grants and complete identity evidence must join before
  enabling the existing publisher. A NuGet version index does not prove deleted-version
  absence; Templates' active/deleted census remains UNKNOWN403 and retains its own owner.
  WASM-only NuGet authorization does not cover Rendering. Publication must retain original
  custody, publish GitHub Packages first, replay the same bytes to nuget.org and qualify
  signature-aware payload readback through existing guards.

- [ ] **.4-I1 — Installed public receivers.**
  Templates and the root own final public pins and fresh public-only direct/SDD/wizard
  acceptance. This source preparation closes neither installed reference adoption nor
  FourD/BAR/SC2 native acceptance. Telemetry attempt
  `shared-render-coherent-publication-source-20261002`: NOTCONFIG, no handle; native usage UNKNOWN.
