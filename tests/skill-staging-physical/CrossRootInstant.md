# Bounded manifest and product-root recheck

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/CrossRootInstantTests.fsx
```

The disposable red-before fixture starts with visible manifest A and product
B. After opening the manifest directory by descriptor, it replaces the
visible manifest directory with B, then changes the product file to A. The
recorded stable phases are A+B, B+B, and B+A; the manifest path is briefly
absent during the directory move. A+A is never visible. The held
manifest descriptor still yields A, and the old `preparePinnedWithHooks`
accepted a plan containing manifest A and product A.

The pinned planner now captures the manifest a second time after it captures
all product sources. It refuses the persistent B manifest with
`manifest-changed-during-capture`, while a stable A+A fixture still passes.
The earlier held-file and held-directory controls continue to prove that a
single descriptor capture returns its opened bytes; their plan-level outcome
is now subject to the second manifest check.

This is a bounded read-only refusal, not an atomic cross-root snapshot. A
source or manifest can change after the last check. Coordinated moves and
restorations between checks, including ABA, can still produce a plan whose
inputs were never simultaneously visible. No staging output or receiver is
written. #1332 Python preflight and #1334/#1335 physical-source acceptance,
actual output ACL/ownership/rollback, concurrent umask or target changes, and
installed-package parity remain separate gates. No release, BOM, publication,
merge, or receiver pin is included.
