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

## Who files

**The finder is the worst available judge of whether the board needs another row**, because from inside
one item every distinct cause looks like one. The failure this addresses is not carelessness — those 48
rows each carried a `## Dedupe` section naming its searches. It is that rate and granularity are
properties of the *sequence* of findings, and no finder can see the sequence from inside one item.

So the finder and the filer are separated where the repository provides a second actor for it:

- **Where a board analyst is available** — `FS-GG/.github` carries one as the `board-analyst` skill,
  `scope: operator`, resolved in the operator checkout — the finder does not file. It records a
  **finding packet** and moves on: an ordinary issue or PR comment under the `fsgg:finding-packet`
  anchor, naming the surface, the root cause it established (or explicitly that it could not, and what
  it measured instead), what is red today, the gate that already derives the condition if one does, the
  open class row if one exists, why the fix could not ride the PR in hand, and the narrow `Paths:` it
  would propose. The packet exists because the finder holds the cause and the tree *now*, and a
  stranger re-deriving that from the board later spends a whole worker slot rebuilding it. The analyst
  adjudicates the packet; it never re-derives it, and it never fills in evidence the packet omits.
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

A new issue states observed behavior, the root cause — or, where you could not establish one, says so
explicitly and gives what you measured instead — acceptance criteria, verification, and a **narrow**
`Paths:` declaration. Add it to the board and set its initial Status. Use `Blocked by:` only for a real
ordering dependency, not transient file overlap. Use a coordination room or `say` for live overlap.

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
