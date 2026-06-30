# T021–T023 — Board & downstream closure (FR-008/009, SC-005)

## T021 — #33 closed (+`V`, +registry PR), board → Done

- **#33 state**: CLOSED / `COMPLETED` (closed `2026-06-30T19:01:35Z` — auto-closed by the `close #33`
  keyword on the merged `main` commit `afecc49`; the substantive resolution — feed listing + registry
  flip — completed afterward and is recorded on the issue).
- **Closure record comment**:
  [#33 issuecomment-4847119525](https://github.com/FS-GG/FS.GG.Rendering/issues/33#issuecomment-4847119525)
  — published `0.1.54-preview.1` + release run `28468936061` + registry PR [FS-GG/.github#78](https://github.com/FS-GG/.github/pull/78).
- **Coordination board (Projects v2 #1)**: item #33 status = **Done** (board automation moved it on
  close; verified `projectItems[].status.name == "Done"`).

## T022 — #31 unblocked

- **#31** (`[cross-repo] Game-template default is a controls demo …`): OPEN, board **In progress**.
- `blockedBy` = **empty** (`totalCount 0`); no `Blocked` label. There is no open-#33 block to clear —
  the mirror is already consistent now that #33 is closed. Nothing to do. ✅

## T023 — SDD#44 notified

- **SDD#44** (`Enumerate the new fs-gg-ui game profile and flip the game/rendering default app→game`):
  OPEN. Notified of the published `0.1.54-preview.1`:
  [SDD#44 issuecomment-4847119667](https://github.com/FS-GG/FS.GG.SDD/issues/44#issuecomment-4847119667).
  The `app → game` default-selection flip (owned by SDD, out of scope here) can now proceed.
