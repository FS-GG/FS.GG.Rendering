# Choosing the axis and the candidate count

A candidate set is only useful if a human can look at it and tell you **which they prefer and why**.
That requires two disciplines: vary **one** axis, and keep the count small.

## The three axes of a visual direction

A candidate is the triple `(grammar, editorial ChannelMap, board treatment)`. Each is an axis you can
vary — but only **one per round**, so a preference is attributable.

| Axis | Values to sweep | Vary it when the open question is… |
|---|---|---|
| **Grammar** | `Grammar.Token` / `Grammar.Badge` / `Grammar.Ring` | "what form factor reads fastest for this game?" — rotating body vs upright emblem vs radial gauge. One `'stats -> Token` map feeds all three unchanged, so this sweep is nearly free. |
| **Editorial ChannelMap** | same grammar, different stat→channel assignment (threat on width vs size; health quiet on the arc vs redundant on hue when critical) | "we know the form factor; which stat deserves the salient channel?" This is the core design decision. |
| **Board treatment** | terrain contrast, on-board `R`, label on/off, the one live `Motion` rhythm | "the symbols are right; does the language survive the real board / density?" |

## Rules

- **One axis per round.** A set that changes grammar *and* assignment *and* size is unattributable —
  the human's preference tells you nothing about what to do next. Hold the frame fixed and the other
  two axes constant.
- **2–4 candidates.** Two is a genuine A/B; three or four is a comfortable contact sheet. Five or more
  is a wall no one compares fairly, and it usually means you skipped narrowing a previous round.
- **Each round must eliminate.** If the round ends with as many live directions as it began, the axis
  you swept was not decision-relevant. Switch axes; do not add candidates.
- **Screen before you present.** Lint every candidate with `Legibility.scoreIn grammar` in step 4 and
  drop the non-`Clean` ones (or re-tune them) *before* the contact sheet. A human's comparison budget
  is the scarce resource; never spend it on a candidate the linter already rejected.

## A typical convergence

1. **Round 1 — grammar.** Same frame, same map, three grammars (Token / Badge / Ring). Human picks
   Badge: "the upright frames don't smear when units cluster at the choke." Grammar is now fixed.
2. **Round 2 — editorial assignment.** Badge, same frame, two maps: threat-on-width vs threat-on-size.
   Human picks size: "I read the dangerous unit before I read anything else." Assignment fixed.
3. **Round 3 — treatment (if needed).** Badge + threat-on-size, two terrain contrasts. Converged.
4. **Iterate + approve.** Hand the winner to the [[fs-gg-symbology]] single-mapping loop, tune to
   `Clean`, pin the golden frame, write the rationale (including that Token/Ring and threat-on-width
   lost, and why).

The rationale of **what lost** is as valuable as the winner: it stops the next designer re-running a
round you already settled.
