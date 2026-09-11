# SVG-SCENE-02.2 portable contract evidence

Date: 2026-09-11

Rendering base: `815783987fbf1d6f2e8165e2ae31ddf0bf61db2d`

Game authority: [FS.GG.Game#621](https://github.com/FS-GG/FS.GG.Game/pull/621), source
`e52ef60b960101abba6d5389b7c6ede414c739d2`, squash merge
`494bd45591851c03496460e250f6584025fb5e52`

## Portable Rendering contract

`FS.GG.UI.Scene` adds an identified, ordered `SvgDocument` that embeds the existing `Scene` leaves.
Render IDs and optional semantic IDs remain separate, layer visibility survives the retained-scene
adapter, and the original Scene and retained APIs remain source compatible. The portable validator
refuses duplicate or blank identities, missing and cross-kind local references, reference cycles and
depth, oversized document/node/path/definition/symbol expansion, non-finite affine transforms, invalid
view boxes, unsupported Scene leaves and external font locations. Direct document construction cannot
bypass the existing supported Scene subset.

`SvgAffine.compose parent local` uses the SVG six-value convention and applies `local` before `parent`.
Unit tests independently calculate translate/rotate, skew/non-uniform reflection and inverse examples;
singular and non-finite inverses return typed errors. The .NET and Fable/Node consumers compile these
contracts solely from a locally packed `FS.GG.UI.Scene 0.29.0-preview.1` candidate and execute the same
translate/rotate, skew/reflection, singular and non-finite checks. They also construct asset and
build-extension descriptors. Serialization remains `ContractOnly` until SVG-SCENE-02.3; no browser,
editor, session, or extension runtime is claimed.

The local candidate identity is `FS.GG.UI.Scene 0.29.0-preview.1`. Its archive is a rehearsal artifact,
not a publication receipt; exact release bytes remain owned by SVG-PREVIEW-A's candidate-first gate.

The canonical retained authority records the document replacement amendment boundary in
`models/svg-foundation/README.md`. Its existing trace corpus remains byte-for-byte unchanged: both
package consumers replay 192 transitions through the public reducer, kill the action-mapping and stale
acceptance mutants, and emit identical projections with SHA-256
`cd1b2c74c95a5f25df06ca0921af7d7af9c578d1f589ef6012fea56fadd5c0d2`. SVG-SCENE-02.4 owns the actual
document replacement reducer/model amendment; this milestone does not introduce a competing authority.

## Game contract authority

The merged `FS.GG.Game.Core` slice supplies Rendering-free, consumer-defined session, semantic-input,
projection and snapshot envelopes for initialize, admit, advance, project, snapshot and restore. Its
support value is explicitly `ContractEnvelopeOnly`: it implements no local loop, worker, server,
transport, persistence or M5 runtime. The local `0.15.0-preview.1` .NET/Fable package consumers agree on
a 488-byte oracle with SHA-256 `328cf37f9cb92064452f3fc3883426230ce97df3d4d3907dc1ca0fdf813164a5`.
The additive surface has 22 new types and 62 new members with no removals. The owning repository passed
882 tests and hosted gate run [34638805938](https://github.com/FS-GG/FS.GG.Game/actions/runs/34638805938).

## Boundary and lineage

All candidate packages remain local; no public package, default, provider or lifecycle changed. Public
installed SDD qualification remains `FS.GG.SDD.Cli 1.7.0`, and the 192-transition correspondence is
preserved. The first whole-solution no-restore attempt in the fresh Rendering worktree failed because
38 projects had no restore assets. Locked restore succeeded, after which the solution built cleanly.
Rendering still lacks a base-loaded repository routine validator; canonical routine fixtures and the
protected PR checks are recorded instead of expanding this product change into governance bootstrap.
No S.I.R. access occurred.

The first protected deterministic run (`34639780361`, job `103396527722`) exposed the expected
candidate/publication seam: the exact-head package exported the new modules and types while the generated
workspace mirror still correctly described public `0.28.0`. The repair adds a reasoned temporary
mirror-omission stanza and raises its explicit ceiling from 483 to 508. A faithful local reproduction
packed the whole coherent framework at `0.28.0`; all 20 applicable template/pin tests then passed (two
immutable-oracle cases were skipped by their existing policy). The stanza must be removed when
SVG-PREVIEW-A publishes and moves the pin, allowing the mirror generator to read those public bytes.
