module Symbology.Tests.ApiSurfaceMirrorTests

// Issue #276 — a generated product does not carry `src/`. It reads the symbology signatures from its own
// `docs/api-surface/`, and the symbology skill's Public Contract sends an agent to exactly those files to
// learn the `Report` shape. Nothing gated that copy and it went stale twice — losing the `Finding.Units`
// past-capacity semantics (76705f3), then the `ChannelKind` doctrine that made `Size`/`Threat`/`Charge`
// Ordered (#285). So: pin the mirror.
//
// ---------------------------------------------------------------------------------------------------
// THE BYTE GATE IS GONE (#753), BECAUSE THE MIRROR IS NO LONGER COPIED FROM `src/` (#752)
// ---------------------------------------------------------------------------------------------------
//
// This file used to assert that each mirror was "its header plus src minus the internals, byte for byte".
// `scripts/refresh-api-surface-mirror.fsx` now EMITS the tree from the PINNED package's packed `.fsi`
// plus the curation in `scripts/api-surface-manifest.txt`, so that sentence describes a file that no
// longer exists: the mirror is header + `namespace` + a manifest-curated SUBSET of the pin, in manifest
// order. It passed on the day it was deleted only by coincidence — `src` happens to equal the pin for
// these four files right now, AND the manifest happens to curate nothing out of them.
//
// It had to go rather than be adjusted, for the reason #694 gives: it compared the emitted tree against
// `src/`, one link up its own generator's chain (`src/` -> nupkg -> mirror). Two consequences made it
// actively harmful once #752 landed:
//
//   - It re-opened the window the generator closed. The mirror tracks the PIN, so any change to
//     `src/Symbology/*.fsi` would red this gate and be UNFIXABLE until the next release — the mirror
//     cannot move ahead of the pin. Package.Tests' M-MIR had `mirror-pending-release-ledger.txt` as its
//     escape hatch for exactly this; this gate never had one (the ledger names no Symbology entry), so
//     it would have been a hard block on the normal state of the repo.
//   - Legitimate curation would read as drift. Dropping a member from a Symbology stanza is sanctioned
//     (`docs/scaffold-map.md`; M-MIR was always "coherence … never completeness"), and this gate would
//     have reported it as "has drifted", sending the reader to hunt a decision.
//
// What it uniquely covered and nobody now does: it was the only gate comparing mirror PROSE to src prose
// (M-MIR compared declarations only). Under the generator that question becomes "do the PIN's doc
// comments match src's" — a pin-vs-src staleness question, not a mirror question, and it belongs with the
// doc-vs-pin oracle in `TemplateConsumesPinnedApiTests` if it is wanted at all. Recorded, not kept.
//
// The presence rule below survives: it is a SKILL fact (these four files are what the skill sends authors
// to), and the generator decides neither side of it. The generator's own orphan check catches a FILE with
// no stanza — never a stanza that went missing, which is the direction that breaks the skill.

open System.IO
open Expecto
open FS.GG.TestSupport

/// The public signature files the pure/render packages ship, and the skill sends authors to.
let private mirrorFiles =
    [ "Symbology/Symbology.fsi"
      // The pure label / rich-text LAYOUT engine was extracted out of `Symbology.fs` (F-CORE-1). Its
      // `.fsi` carries the PUBLIC label text types (`LabelRun`/`LabelText`/…) a product needs to author a
      // `Token.Label`; the `module internal LabelLayout` seam is not product-visible (S-INT, #585).
      "Symbology/LabelLayout.fsi"
      "Symbology/Legibility.fsi"
      "Symbology.Render/Render.fsi" ]

[<Tests>]
let apiSurfaceMirrorTests =
    testList
        "Issue276 shipped api-surface mirror"
        [
          // The mirror is what a product reads, so a signature the skill names must actually be there.
          test "the mirror ships every public symbology surface file" {
              for mirror in mirrorFiles do
                  let mirrorPath =
                      Path.Combine(RepositoryRoot.value, "template", "base", "docs", "api-surface", mirror)

                  Expect.isTrue (File.Exists mirrorPath) $"{mirror} is named by the skill but is not shipped"
          } ]
