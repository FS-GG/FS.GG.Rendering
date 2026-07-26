# Judgement findings

These findings are deliberately outside `reconcile --apply`:

- `UNCLAIMED-IN-PROGRESS`: identify the worker or decide whether the column is stale.
- `BLOCKER-UNPARSEABLE` or unknown: resolve the named dependency without guessing.
- `UNDECLARED-PATHS` / bad touch-set: ask the owner to declare a narrow truthful set.
- `DONE-STATUS-OPEN-ISSUE`: determine whether the flip was premature.
- `EPIC-*`: inspect every child and its acceptance criteria before rolling up.

Record evidence and the decision. Mechanical board writes may follow a decision, but the engine must
not manufacture the decision itself.
