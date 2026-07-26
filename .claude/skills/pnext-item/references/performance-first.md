# Performance-first planning gate

Use this gate for interactive/game work before implementation. A non-interactive product with no
active typed performance intent has no performance gate; do not invent an irrelevant timing target.
A bounded headless workload can prove its declared route, but never substitutes for required
live-compositor or swapchain evidence.

## PERF-PLAN — declare the route before implementation

1. Read the producer-owned typed performance intent. Treat its target FPS, representative workload
   identities and definition digests, maximum expected scale, timing and structural limits,
   measurement capability, live-compositor posture, evidence references, and disposition as the
   single declaration. Do not maintain a second performance contract.
2. Invoke each focused product or subsystem skill by name and load its performance section. In the
   rendering workspace this includes the applicable `fs-gg-*` skill, such as `fs-gg-scene` or
   `fs-gg-skiaviewer`. If no focused skill exists, use the project's playtest guidance and the
   performance sections for every touched subsystem.
3. Author or update executable representative workloads that traverse the production `update` +
   `view` or equivalent real route. State expected scale and structural budgets before code changes.
   A `Placeholder`, synthetic-only, missing, or stale workload cannot pass this gate.

## PERF-SMOKE — establish the fast baseline

Capture the initial focused smoke before implementation. Record the workload definition digest,
declared budgets, observed counters, host/capability facts, and any existing headroom or debt. Smoke
is iteration evidence only; it is never ship evidence.

If worker-created scope changes the route, workload, expected scale, budget, or touched subsystem,
return to PERF-PLAN, refresh the declaration and workload, then rerun PERF-SMOKE before continuing.

## PERF-IMPLEMENT — keep cost observable

While implementing, expose deterministic cost counters for affected hot paths. Use the counters that
fit the route: scene nodes, search expansions, blocker-index builds, allocation/update counts,
raw-input-to-applied ratio, and moving-versus-interpolated actors. Re-run the focused workload while
cost shape changes; do not replace structural counters with noisy wall-clock assertions.

## PERF-RELEASE — prove the exact candidate

Before opening or updating the PR for review, run the full Release `Test`/`Verify` performance route
against the exact candidate and retain its typed performance evidence. Then obtain the independent
Governance verdict when that capability is available. Required live-compositor evidence must come
from a capable protected host; bounded headless results stay separately labelled.

An exceeded budget, invalid binding, stale definition, missing required host capability, or failed
independent verdict blocks shipping. A linked performance-debt issue may explain a baseline, but
never turns it green.

## PERF-REPORT — make the decision durable

Report workload and budget changes, observed headroom or debt, deterministic counters, capability
limits, Release evidence, Governance verdict, and every linked blocking performance-debt issue.
Surface a human decision or environment/capability blocker once with the next action and stop
retrying it; do not spin.
