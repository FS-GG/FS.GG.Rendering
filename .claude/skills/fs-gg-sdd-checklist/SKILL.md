---
name: fs-gg-sdd-checklist
description: Stage 4 of the FS.GG SDD lifecycle — fsgg-sdd checklist reviews requirements quality and computes FR→AC coverage. Carries the load-bearing coverage-line grammar (- FR-###: … (covers AC-###)) that silently fails if mis-formatted. Use after clarify, before plan.
---

# Checklist (stage 4)

`checklist` is the requirements-quality gate before planning — "unit tests for the
spec." Its most important job is computing **FR→AC coverage**, and that depends on
a strict, easy-to-get-subtly-wrong grammar. Get the grammar right and coverage is
real; get it wrong and a requirement is silently reported uncovered.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/checklist.md` is a complete,
worked `checklist.md` — the view the real `checklist` gate produces from the corpus sources, regenerated and
checked clean on every build by the skill↔gate doctest. Where the prose below and the
example disagree, the example is the authority.

## Command

```text
fsgg-sdd checklist --work <id>
```

## Produces / consumes

- **Consumes:** `work/<id>/spec.md`, `work/<id>/clarifications.md`.
- **Hybrid source:** `work/<id>/checklist.md` — see "Who owns which section" below.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `plan` ([[fs-gg-sdd-plan]]).

## The coverage line (load-bearing grammar)

A functional requirement is marked **covered** only when a *strict-scan* parser
finds a list item that:

1. starts with a literal `- `,
2. then `FR-` followed by **three or more digits** (case-insensitive),
3. then a literal `:`,
4. then prose,
5. with the acceptance reference (`AC-###`, optionally `US-###`) **on the same
   physical line.**

The scan reads a **single physical line** — it does not join continuation lines. So
a long requirement that your editor **soft-wraps** across several physical lines,
with `(covers AC-###)` landing on line two, is reported uncovered even though the
marker is right there. Keep the whole `- FR-###: … (covers AC-###)` on one physical
line (disable soft-wrap / "reflow paragraph" for that line if you must).

**Accepted** (each line establishes coverage):

```text
- FR-001: W/S move the left paddle. (covers AC-002)
- FR-014: Ball serves toward the loser. (Stories: US-003; Acceptance: AC-009)
```

**Counted but NOT covered** (a separate loose scan `\bFR-\d{3,}\b` lists the
requirement, so it *looks* present but establishes no coverage):

```text
**FR-001** W/S move the left paddle. (AC-002)     ← bold id, not a "- FR-001:" item
- FR-001 — moves the paddle (AC-002)              ← no colon after the id
(covers AC-002)                                    ← AC ref on its own line
- FR-001: a long requirement whose marker         ← soft-wrapped: the marker
  wrapped to the next line. (covers AC-002)          landed on line two
```

If `checklist` reports a requirement uncovered that you "know" you wrote, it is
almost always one of these four form errors — and the last one, a soft-wrapped
bullet, is the sneakiest because the marker *is* present, just on the wrong physical
line.

## Required headings and id prefixes

Sections: **Source Specification, Source Clarifications, Source Snapshot, Checklist
Items, Review Results, Accepted Deferrals, Blocking Findings, Advisory Notes,
Lifecycle Notes.**

Id prefixes: `CHK-###` (checklist items), `CR-###` (review results).

## Who owns which section

`checklist.md` is a **hybrid**: yours to keep, but not yours alone. Every run of
`fsgg-sdd checklist` re-derives five sections wholesale from the current `spec.md`
and `clarifications.md`, discarding whatever was there:

**Source Snapshot, Checklist Items, Review Results, Accepted Deferrals, Blocking
Findings.**

You do **not** need to pre-create these headings: a run **scaffolds** any that are
missing and then fills them (FS.GG.SDD#572), so "re-derived wholesale" means created-if-absent
too — a deleted `## Accepted Deferrals` heading comes back rather than blocking.

Hand-writing a review into any of them is work the next run erases — the reviews
come from `--input`. What is left to you is **Source Specification, Source
Clarifications, Advisory Notes, Lifecycle Notes**, and the front matter:

<!-- fsgg-sdd:example corpus=checklist.md mode=ref -->
```markdown
## Advisory Notes
- Coverage is complete; both requirements pass.
```

Note also that **coverage lines live in `spec.md`, not here.** A `- FR-001: … (covers
AC-001)` line written into `checklist.md` establishes nothing — and the section you
wrote it in is re-derived anyway.

`docs/examples/lifecycle-artifacts/checklist.md` is a full gate-clean artifact,
tool-written sections and all. Read it to see what the stage produces; don't copy it
in — the tool writes those bytes itself.

## Pitfalls

- The three rejected forms above — bold id, missing colon, off-line AC ref.
- Fewer than three digits in the id (`FR-1`) — needs `FR-###` (3+ digits).
- **`## Blocking Findings` empty-section rule.** A bullet here blocks `plan` unless
  it is a disclaimer — a bare `- None.` / `- No blocking findings.` is safe and does
  **not** block. But a `No …` bullet that names a real gap (`- No tests cover
  FR-003.`) is a genuine finding and *does* block, by design. Full grammar:
  `docs/reference/authoring-contracts.md`.

## Reaching `checklistReady`

There is **no manual status transition to author**. A clean `checklist` review
writes `status: checklistReady` directly; an unclean one writes `needsCorrection`.
If `plan` reports *"Checklist status '…' is not checklistReady"*, clear the blocking
findings / stale reviews and **re-run `fsgg-sdd checklist`** — it re-promotes the
status. Do not hand-edit the status field.

## Next

- `plan` — turn covered requirements into a technical plan: [[fs-gg-sdd-plan]].

## Related

- [[fs-gg-sdd-authoring-contracts]] (the full grammar + drift guard),
  [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/reference/authoring-contracts.md` (§ Acceptance coverage line);
  `docs/quickstart.md`.
