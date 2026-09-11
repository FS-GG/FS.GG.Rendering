# SVG-QUAL-01.2 retained reducer qualification

The retained SVG interaction contract now has one editable literate profile-2 authority at
`models/svg-foundation/retained-interaction.md`. Explicit source-range and action bindings drive a
deterministic SDD 1.7.0 extraction committed under `readiness/svg-qual-01-2/`; the former standalone
`.qnt` sources were removed so generated evidence cannot become a competing authority.

Qualification uses the public `FS.GG.SDD.Cli` 1.7.0 package from nuget.org. After installation it
runs offline, authors and inspects two fresh scratch workspaces, compares their generated evidence
with the committed tree, and rejects shifted source ranges and an unknown action declaration. The
upstream installed-authority release is FS.GG.SDD v1.7.0 at source
`b1a3bc1c46dfc28d7e8a02696f0e7bf4b026df50`; its public dual-feed Q2/Q3 run was `34623411200` with
receipt artifact `10273841192`.

The extracted Quint module runs its declared tests and a fixed-seed bounded run. ITF model witnesses
are deterministically converted into a committed 192-transition corpus covering all eight public
message kinds and all modeled outcomes. The real `SvgRetained.update` correspondence suite covers
selection, replacement, clearing, focus movement, camera changes, and pointer capture/release,
including replacement retention and clearing. Mutants for stale acceptance, action mapping,
retention, focus direction, camera projection, and capture ownership must fail. Independently packed
.NET and Fable/Node consumers replay the same corpus against the public reducer, emit the first
divergence, kill stale-acceptance and action-mapping mutants, and produce byte-identical canonical
projection results.

This is bounded safety and implementation-correspondence evidence: 12 steps, 16 traces, two stable
selectable identities, bounded revisions and pointers, and integer camera samples. Temporal liveness,
unbounded reachability, exhaustive state-space claims, and floating-point numerical equivalence are
not qualified here. The production reducer tests retain finite floating-point validation, and the
Chromium suite remains an effect-boundary check.
