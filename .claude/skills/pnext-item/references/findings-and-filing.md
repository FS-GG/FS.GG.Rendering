# Findings and filing

Before filing, list open issues over REST and search titles, bodies, and comments for the cause. REST is
the fallback when the Projects GraphQL budget is exhausted. Reuse an existing issue when it expresses
the same cause; transplant new evidence there.

A new issue states observed behavior, root cause, acceptance criteria, verification, and a narrow
`Paths:` declaration. Add it to the board and set its initial Status. Use `Blocked by:` only for a real
ordering dependency, not transient file overlap. Use a coordination room or `say` for live overlap.

Never broaden the current PR merely because a nearby defect is easy. Put distinct work in the follow-up
queue so the same informed worker can take it after this item.

This file governs findings the implementer discovers before independent review. After the review gate
starts, [independent-review](independent-review.md) takes precedence: the critic alone searches and
files review-discovered causes, files only material unresolved work, and files it directly rather than
through either agent's private follow-up queue. Nonmaterial observations never become issues or board
rows.
