# Cross-repo coherence (T024 / FR-010 / research R4)

Captured 2026-06-30 via the `cross-repo-coordination` skill.

## State

- **Coordination issue #36** (FS-GG/FS.GG.Rendering): commented the implementation result with all
  three acceptance items ticked (catalog rewrite, scaffold-map repoint, currency check).
  <https://github.com/FS-GG/FS.GG.Rendering/issues/36#issuecomment-4847892177>. Kept **open** —
  delivery to consumers requires the next republish.
- **Coordination board (Projects v2 #1 `Coordination`)**: #36 moved `Backlog → In review`; the
  `Blocked by` note updated from the stale `#33` (now CLOSED — it was the Feature 220/222 game-profile
  republish) to "awaits next fs-gg-ui-template republish + registry flip; feeds Templates providers
  pin bump".
- **Epic #34** (P1 · Consumer skill surface) is the parent; no open republish vehicle currently
  exists, so #224 rides the **next** `fs-gg-ui-template` republish (same pattern as #35 / Feature 223).

## Registry / contract coherence (deferred to the republish, per protocol)

- Registry `FS-GG/.github` `registry/dependencies.yml` `fs-gg-ui-template` sits at
  `0.1.55-preview.1` (Feature 223 / #35). This change is **package content only**; the registry
  version/coherence update + `compatibility.md` projection ride the actual org-feed republish
  (contract-change protocol), exactly as #35 did. No coherence flag flips true→false — a coherent
  advance. No registry edit is made by this local merge (would be premature before the feed publish).

## Separate finding flagged

The `"replaces": "fs-gg-ui"` substitution mangling the shipping `fs-gg-ui-widgets` skill's `name:`
to `<product>-widgets` (see `produced-surface.md`) is recorded on #36 as a candidate epic-#34
follow-up; out of scope for this feature's repo-side currency check.
