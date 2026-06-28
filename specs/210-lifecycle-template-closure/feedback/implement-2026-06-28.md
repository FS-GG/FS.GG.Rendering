---
phase: implement
date: 2026-06-28
severity: minor            # none | minor | major | blocker
---

## Process friction

Three friction points, all resolved in-phase (none blocking):

1. **Spec/plan assumed the cross-repo remainder was OPEN; at implementation time it was already
   `Done`.** The spec (FR-010/FR-011), research R7, and tasks T016/T017 were written for a world where
   `FS-GG/FS.GG.SDD#1` is *open* and the constitution-ownership decision is *unresolved* — to be
   "reused" / "captured". Reality at implementation: SDD#1 is **CLOSED** (2026-06-27) and the
   constitution-ownership decision is already a board item marked **Done** (downstream P2 impl Done
   too). Following T017 literally (search SDD issues → none → "create exactly one") produced a
   **duplicate** (`SDD#2`) of an already-tracked-and-resolved board decision, which I then closed.
   Lesson: a closure feature's coordination tasks must **read the board first** (the source of dedupe
   truth) before the per-repo issue search, because a decision can be tracked as a board *draft* item
   with no repo issue. The dedupe edge case (FR-010) is satisfied by reusing the board item, not by
   filing a new issue when the issue search comes up empty.

2. **Build spot-check target — the generated product ships no solution at the root.** The harness
   first ran `dotnet build` in the output dir with no target → `MSB1003: Specify a project or
   solution file`. The product's build entry point is `src/<name>/<name>.fsproj` (no `.sln`/`.slnx`).
   Fixed by targeting that fsproj explicitly. A note in the contract that the build spot-check must
   name the product project (not rely on CWD discovery) would have pre-empted the first red run.

3. **`provenance: live` requires re-running the full live gate on every record-content change.** The
   cross-repo remainder lines are *static* strings in the harness, but because the CLOSE conclusion is
   only valid from a `live` run, each edit to those strings forced another ~full 3×4-matrix + 2-build
   live run (the `--emit-report` path writes `verdict-core`, which is not close-eligible). Acceptable
   for a one-shot closure proof, but a future harness could separate "live-verified results" from
   "static coordination references" so the latter can be patched without re-running the live matrix.

## Generalizable code

none as library surface — this feature ships no product `.fs`/`.fsi` and no generated-template-output
change (FR-012). The only executable artifact is the closure-proof harness
`scripts/validate-published-acceptance.fsx`, deliberately a one-feature on-demand proof, not a
standing gate. **Candidate tooling pattern** (already shared with `validate-lifecycle-template.fsx`):
the "install pinned PACKAGE → run `dotnet new` matrix → byte-identity + build spot-check → uninstall in
`finally`" loop is reusable for any future "validate the published artifact, not the working tree"
acceptance — worth lifting into a shared `.fsx` helper if a third such validator appears.

## Skill gaps

none new — `cross-repo-coordination` covered the board/dedupe work and `fs-gg-feedback-capture` covered
this record. One doc addition would help: the cross-repo guidance should state that **board draft
items are first-class trackers for the dedupe check** (a decision can be "tracked exactly once" as a
board draft with no GitHub issue), so closure features don't mistake an empty issue-search for "no
tracker exists yet."
