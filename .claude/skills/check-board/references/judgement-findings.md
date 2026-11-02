# Judgement findings

For the organization successor, inspect human Ready with unresolved action prerequisites,
ownerless In progress, superseded In review, Done without required acceptance and parents with open
required children. Report exact owner/evidence gaps and proposed judgement; do not auto-change human
fields. Follow the [prepared organization boundary](organization-v2-inspection.md), including scoped
Unknowns and fresh execution-side rechecks. Source preparation does not activate consumption.

The legacy finding names below retain only their separately qualified product/historical meaning.


These findings are deliberately outside `reconcile --apply`:

- `UNCLAIMED-IN-PROGRESS`: identify the worker or decide whether the column is stale.
- `BLOCKER-UNPARSEABLE` or unknown: resolve the named dependency without guessing.
- `UNDECLARED-PATHS` / bad touch-set: ask the owner to declare a narrow truthful set.
- `DONE-STATUS-OPEN-ISSUE`: determine whether the flip was premature.
- `EPIC-*`: inspect every child and its acceptance criteria before rolling up.

Record evidence and the decision. Mechanical board writes may follow a decision, but the engine must
not manufacture the decision itself.
