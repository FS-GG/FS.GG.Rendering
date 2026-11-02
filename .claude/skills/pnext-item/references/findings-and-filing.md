# Findings and filing

Apply [canonical intake](https://github.com/FS-GG/.github/blob/main/.agents/skills/work-board/references/intake.md). Establish actual scope/cause, semantic
matches, materiality and whether an in-scope repair belongs on the current PR. Reuse existing issues;
a distinct cause does not automatically require a new row. A legitimate capability request need not
be red today. Retain rejected/duplicate/deferred findings and their evidence in existing native context.

Report material process problems promptly when they block progress, risk incorrect effects or acceptance,
or cause substantial avoidable work. Distinguish observation from hypothesis; include bounded evidence,
delivery impact, the workaround used and a proposed improvement when known. A missing solution does not
suppress a valid report. If an environment interruption (fdev reboot, runner congestion, external mirror outage)
recurs and costs material time, the affected owner files or extends one owned finding on the coordination-efficiency owning issue (currently #4730) with timestamps
and impact, instead of mentioning it only in a report. Use existing owning issues/PRs and worker returns; do not invent findings,
repeat unchanged reports or create a separate reporting worker, packet, ledger or measurement campaign.
The root ensures [reasoned, owned disposition](https://github.com/FS-GG/.github/blob/main/.agents/skills/work-board/references/host-loop.md#own-process-findings-and-disposition),
retaining unresolved owner, next action and reconsideration condition. Reporting or disposition grants
no dispatch, reservation release, uncertain-effect retry, Status write, weaker acceptance or closure.

**The current owner may dispose and file the finding directly.** An analyst is optional when authorized
and useful; availability never mandates a second actor, packet, minted identity or synchronous filing
gate. Existing packets remain usable evidence. Keep one owner for unresolved findings; do not strand
them in a private worker queue or off-board rejected-findings ledger. Native issue creation, linking,
board membership, protected enrollment and planning are separate admitted/read-back effects.

Independent reviewers own the disposition of material findings they raise. Review does not require
third-party filing permission; nonmaterial observations stay in the review. This reference does not
activate the conditional heavyweight [independent-review](independent-review.md) protocol.

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
unbounded "known issue" remains a finding. This decides whether a measured limitation is still a finding. Canonical intake separately decides
whether it warrants distinct work; user requests do not need a failing test today.

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
