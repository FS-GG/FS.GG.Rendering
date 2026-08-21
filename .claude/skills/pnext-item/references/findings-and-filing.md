# Findings and filing

## Establish the cause; do not reach for the New Issue button

A finding is where a defect *surfaced*, which is rarely where it *lives*. Before anything else, list
open issues over REST and search titles, bodies, and comments **for the cause, not the symptom** — rows
expressing one cause routinely share no symptom text at all. REST is the fallback when the Projects
GraphQL budget is exhausted. Reuse an existing issue when it expresses the same cause; transplant your
new evidence there.

That much has never been in dispute. What follows is: **finding a distinct, unfiled, well-evidenced
cause is not by itself a reason to create a row.**

## The bar a finding must clear

`.github#2584` measured 48 rows filed in 30 hours, every one of them a distinct, correct,
well-evidenced cause. The predicate *"distinct unfiled cause → file"* passed all 48, which is what
makes it unfalsifiable in an instrumented codebase rather than merely permissive. A finding becomes a
row only if it clears all three of these:

1. **Red today.** Name a command failing on the default branch now, or the specific merge it blocks.
   "Latent" and "nothing is broken yet" are not rows.
2. **Not already derived.** If a checked-in gate script computes and reports the condition, that output
   **is** the tracking. A row restating it drifts the moment it is written.
3. **Class-anchored.** If an open row already proposes the mechanism that prevents this finding's whole
   class, the finding is **evidence on that row** until the class row lands.

The bar governs **findings**. It does not govern operating changes the host or the user has already
decided to make: a decision is not required to be red before it is recorded.

## Disclosed residual — a terminal state, not an unbounded waiver

Some limitations have a real, finite bound but are not worth closing further. A limitation is a
**disclosed residual** and may be a terminal state rather than a finding only when **all four** of
these conditions hold:

1. **Measured bound.** The bound is measured against a named subject and revision, not estimated or
   described as merely small.
2. **Caller-facing disclosure.** The bound is stated where a caller or operator meets the affected
   contract. A commit message or private scratch result alone is not disclosure.
3. **Executable pin with observed red.** A checked-in assertion fails if the residual grows beyond
   the recorded bound, and its evidence includes a mutation that widened the residual and made that
   pin red. A pin whose failure has not been observed does not establish this condition.
4. **Owned, durable decision.** The actor who owns disposition at that boundary records the decision,
   their identity, and links to the measurement, disclosure, pin, and red mutation in the durable
   artifact that owns the disposition: the item or PR for an implementer, the review record for an
   independent critic, the packet/register disposition for a board analyst, or the decision record
   for a human choice. Someone without disposition authority may propose a residual, but cannot
   declare one terminal.

The conditions are conjunctive. Meeting three identifies a finding about the missing fourth; an
unbounded "known issue" remains a finding. This is also **not an exemption from the filing bar's
red-today test**: the bar decides whether a finding becomes a row, while this rule decides whether a
measured limitation is still a finding at all.

Two existing decisions demonstrate the boundary:

- `.github#2667` stopped at the plain-lift limitation after the residual was disclosed and pinned;
  the independently measured optional improvement was deliberately declined rather than treated as
  proof the accepted item was unfinished (`.github#2691` comment `5304344697`).
- `.github#2712` records DEC-003 at its true `Done` bound and pins that exact outcome in
  `LifecycleProjectionTests`. Independent confirmation changed the reducer so the formerly refuted,
  narrower bound became true; exactly that pin went red (`.github#2745` comment `5310802698`).

The `.github#266` exclusion is absolute: a check that cannot fail, an empty or unreadable subject
reported as pass, or a pin whose widening mutation stays green has no enforceable bound to disclose.
It is a defective verification artifact, not a disclosed residual, and must be repaired or routed as
a finding.

## Who files

**The finder is the worst available judge of whether the board needs another row**, because from inside
one item every distinct cause looks like one. The failure this addresses is not carelessness — those 48
rows each carried a `## Dedupe` section naming its searches. It is that rate and granularity are
properties of the *sequence* of findings, and no finder can see the sequence from inside one item.

So the finder and the filer are separated where the repository provides a second actor for it:

- **Where a board analyst is available** — `FS-GG/.github` carries one as the `board-analyst` skill,
  `scope: operator`, resolved in the operator checkout — the finder does not file. It records a
  **finding packet** and moves on: an ordinary issue or PR comment under the `fsgg:finding-packet`
  anchor, carrying a fenced `json` block in the `fsgg.coord.finding-packet/v1` shape. The packet exists
  because the finder holds the cause and the tree *now*, and a stranger re-deriving that from the board
  later spends a whole worker slot rebuilding it. The analyst adjudicates the packet; it never
  re-derives it, and it never fills in evidence the packet omits.

  ````
  <!-- fsgg:finding-packet -->
  ```json
  {
    "schema":     "fsgg.coord.finding-packet/v1",
    "surface":    "where it showed up — file:line, a command, or a run URL",
    "cause":      { "established": "the root cause" },
    "redToday":   { "found": "the command failing on main now, or the merge it blocks" },
    "derivedBy":  { "searchedNotFound": "the search that found no scripts/check-*.py computing this" },
    "classRow":   { "notSearched": "why you did not look" },
    "whyNotHere": "why the fix could not ride the PR you were already pushing",
    "paths":      ["the narrow declaration you would propose"],
    "finder":     "your minted worker id, alone"
  }
  ```
  ````

  `cause` is `{"established": …}` or `{"notEstablished": "what you measured instead"}` — #1858's rule,
  one step earlier. `redToday`, `derivedBy` and `classRow` each take exactly one of `{"found": …}`,
  `{"searchedNotFound": "the search you ran"}` or `{"notSearched": "why you did not"}`. **Say
  `notSearched` when you did not look.** It is an honest answer and costs you nothing; the old `none`
  — and a bare `null` — cannot tell the analyst whether you looked, and tests 2 and 3 of the bar above
  are precisely questions about whether a search happened and was adequate.

  **Validate before you post**, while you still hold the tree:

  ```sh
  scripts/fsgg-coord packet validate my-packet.json
  ```

  It reads a file and decides — no board, no network, and **no power to refuse a post**. Nothing sits
  between you and the register. If it refuses and you are out of time, post the prose you have: a
  packet nobody validated still beats a finding nobody recorded. Packets already posted in free prose
  remain valid and readable; the validator applies **forward only** (`.github#2737`).
- **Where no analyst is available**, the finder files — and applies the same three tests to itself,
  recording which one it considered and why the finding cleared it.

**Nothing waits on the analyst.** Posting a packet is not a handoff and blocks no review round, no
merge, and no done stamp. A synchronous filing choke-point would wedge chains, and a wedged chain costs
more than a duplicate row.

A rejected finding still needs somewhere durable to live. It is **not** the worker follow-up queue —
that is keyed on the resolved worker id and is the *"I can fix this, just not in THIS PR"* promise a
worker makes to itself, so it cannot hold a finding that must survive for whoever eventually claims the
area. Route it to the row where it will be looked for, and to the analyst's off-board rejected-findings
register.

## When a row is created

A new issue is composed from the complete `fsgg.coord.intake/v1` draft in
[deep detail](deep-detail.md): observed behavior, root cause — or, where you could not establish one,
what you measured instead — acceptance, verification, `paths`, `class`, `severity`, and optional
`blockedBy` all belong in that draft. Run `scripts/fsgg-coord intake validate`, then `intake apply` on
the same file. The transaction creates or reuses the issue and projects its initial board fields.
Hand-authoring `Paths:` or `Class:` in the created body is a defect, not a style choice. Use
`blockedBy` only for a real ordering dependency, not transient file overlap. Use a coordination room
or `say` for live overlap.

Declare only what the work touches. An over-broad declaration costs the whole board a lane and nothing
in `lint` catches it: `lint` flags a row with no `Paths:` and a row whose tokens are unmatchable, never
one merely far wider than its work.

Never broaden the current PR merely because a nearby defect is easy. Put distinct work you intend to
take yourself in the follow-up queue so the same informed worker can pick it up after this item.

## The review boundary

This file governs findings the implementer discovers **before** independent review.

After the review gate starts, [independent-review](independent-review.md) takes precedence: the critic
alone searches review-discovered causes, **owns the disposition of the findings it raises**, files only
material unresolved work, and files it directly rather than through either agent's private follow-up
queue. That directness is deliberate and is not overridden here — a critic whose material finding needs
a third party's permission to become a number has less authority than the review contract grants it,
and a review round that waits on an analyst is a wedged chain. Nonmaterial observations never become
issues or board rows; they are said in the review body. An analyst folds, retitles or closes a
review-filed row in a later pass, after the fact, exactly as it would any other row.
