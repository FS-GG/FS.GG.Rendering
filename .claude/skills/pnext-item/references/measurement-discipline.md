# Measurement discipline

A command can be sound and still answer the wrong question. Every assertion of absence, a count, or
"unchanged" therefore carries the control that would have detected the opposite **and the control's
result**. A `Verification:` line that names only the measurement command does not satisfy this rule.

Record the observation envelope with the result:

- `subject`: the exact file, artifact, tree, query, run, or response measured;
- `authority`: the source that owns the fact, rather than a convenient proxy;
- `revision`: an immutable digest, commit SHA, run/tree SHA, or response cursor;
- `census`: how completeness was established, including pagination and truncation;
- `positive control`: a known-present or known-changing case the same instrument observed; and
- `result`: the assertion, including `Unknown` when any envelope field is unreadable.

Repository and pull-request observations bind both the exact head SHA and the effective base SHA.
Derive the base once with `git merge-base <base-ref> <head-sha>` and record the returned SHA. A two-dot
diff against a moving name such as `origin/main` is invalid evidence: it compares two current trees and
can add base-only changes to a count. Before review acceptance and again before landing, resolve the
effective base. If it changed, the earlier CI or review observation is `Stale`; its green result remains
historical evidence, not authorization.

For paged APIs, follow every page until the authority reports `has_next: false` (or the equivalent
terminal cursor). A response whose item count equals the page size is a truncation warning, never proof
of completeness. An unreadable cursor, missing pagination field, or truncated history is `Unknown`,
not an empty census.

## What the control would have caught

These controls come from measured wrong-reference incidents, not hypothetical misuse:

| incident | naive subject | required control |
|---|---|---|
| history reported a commit had no parent while its object named one | a history walk truncated at a shallow boundary | walk to a known-parent commit and show that the walk sees its parent; record shallow-boundary state |
| a reverted mutation looked clean while the built DLL still carried it | source status used as the artifact authority | verify reverted source bytes and the suite; use artifact hashes only forward, showing that the mutation changed the intended compilation unit |
| green registry runs were correlated with drift absent from their trees | completed runs used without their tree revisions | show that each run's exact tree contains the alleged drift before interpreting its result |
| a PR diff and an obligation counted files from a moved `origin/main` | two current trees rather than the PR's effective base and head | record `merge-base`, require a known touch-set file in the diff, and recheck the effective base before acceptance and landing |
| a DLL string search reported absence | UTF-8 decoding of a .NET `#US` heap | first find a known-present string with the identical decoder/encoding, then search the target |
| engine-freshness counts disagreed | branch commits and landed-main commits treated as one census | name the authority and immutable revision for each count; do not compare differently scoped counts |
| an epic link was reported absent from 30 returned children | the first API page | show the terminal pagination state, not merely the returned length |

The shallow-history incident was time-bound: the repository was shallow when the wrong answer was
measured and later deepening removed `.git/shallow`. A later `is-shallow-repository: false` describes a
repaired revision; it does not refute the earlier observation. The revision and control result prevent
those two valid measurements from being collapsed into one contradiction.

## `Verification: by parse`

`Verification: by parse` means the assertion was re-derived from the named artifact by a parser and the
parser's output is shown. It does not mean that a person read the artifact, counted rows visually, or
ran a command without reporting its output and control. State the parser, exact subject revision,
completeness result, positive-control output, and parsed result.

For example:

```text
Verification: by parse
subject: review.json
authority: structured review ledger
revision: sha256:2d4e...
census: jq reported has_next=false after 3 pages
positive control: jq selected the known revision=1 record (count=1)
result: jq '[.records[] | select(.verdict == "pass")] | length' -> 2
```

## Negative control: the naive count passes and the completeness control fires

Given this valid first page:

```json
{"nodes":[{"number":1},{"number":2}],"page_size":2,"has_next":true,"end_cursor":"c2"}
```

the naive parser exits successfully and publishes a plausible count:

```console
$ jq '.nodes | length' first-page.json
2
```

The required control fails the same input because the census is incomplete:

```console
$ jq -e '(.has_next == false) and ((.nodes | length) <= .page_size)' first-page.json
false
$ echo $?
1
```

That red result is the evidence that the rule can reject the failure it describes. Fetch the next page
and repeat until `has_next` is false; if that cannot be done, report `Unknown` rather than `2`.

## Base-change control

Keep the recorded base SHA beside every review observation. Immediately before acceptance or landing:

```sh
effective_base="$(git merge-base origin/main "$HEAD_SHA")"
test "$effective_base" = "$RECORDED_BASE_SHA"
```

A non-zero result turns the formerly green observation `Stale`. Re-run the diff and affected gates on
the new base/head pair; never carry the old green forward as authorization.
