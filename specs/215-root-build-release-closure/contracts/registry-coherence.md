# Contract: Registry coherence + closure ordering (ADR-0001 / contract C5)

**Owner**: `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`), PR `#25`;
issue `FS-GG/FS.GG.Rendering#9`; FS-GG "Coordination" board. Driven via `gh` + the `cross-repo-coordination`
skill.
**Satisfies**: FR-005, FR-006, FR-007, FR-008, FR-009, FR-010 · SC-004, SC-005, SC-006 · US2, US3

## Registry entry (US2)

On the `fs-gg-ui-template` contract, in **both** the authoritative `registry/dependencies.yml` and its
`docs/registry/compatibility.md` projection:

| Field | Required |
|---|---|
| `surface` | records `root-buildable` (root `.slnx` + SDK pin + verb wrapper) |
| `coherent` | `true` |
| `version` / `tag` | `0.1.52-preview.1` / `fs-gg-ui-template/v0.1.52-preview.1` (== published; FR-005) |
| `tracking` | `FS-GG/FS.GG.Rendering#9`, attributed to Feature 215 / Feature 212 (FR-007) |
| carrier | PR `FS-GG/.github#25` — currently **CONFLICTING**: rebase to clear, re-pin draft to `0.1.52` |

## Hard ordering (FR-006 / SC-004)

```text
release Published (feed + tag, gate green)  ──required──►  merge PR #25
```

PR #25 MUST land **with or after** the release, **never before** — no window in which the registry advertises
a guarantee no published package satisfies (Edge "Premature registry merge"). The pinned version MUST equal
the actually-released version (adjust if the release number differs from the PR's current draft).

## Closure (US3)

1. **Issue #9** — close with a comment citing (FR-008):
   - released template version `0.1.52-preview.1` + tag `fs-gg-ui-template/v0.1.52-preview.1`,
   - the green **real-release** `template-product-tests` run URL,
   - the merged `FS-GG/.github#25`.
   ```bash
   gh issue comment 9 --repo FS-GG/FS.GG.Rendering --body "<evidence>"
   gh issue close   9 --repo FS-GG/FS.GG.Rendering
   ```
2. **Coordination board** — set the H1 rendering item to `Done` (FR-009): `gh project item-edit … --field
   Status --value Done` (resolve the item id via `gh project item-list`; board draft items are dedupe
   trackers — read the board first).
3. **Downstream unblock** — signal the FS.GG.SDD acceptance-probe consumer that the released root-buildable
   template is available for its probes (FR-010 / SC-006).

## Verification

- Merged registry: `fs-gg-ui-template` entry shows `root-buildable` + `coherent: true` pinned to
  `0.1.52-preview.1`, tracker `#9` visible on the compatibility surface.
- Registry merge timestamp ≥ release publish time (ordering held).
- `gh issue view 9` → CLOSED with the evidence comment; board H1 rendering item → `Done`.
