# SVG scale, cache and complexity qualification

The packed Chromium harness now consumes the frozen SVG-SCALE-01.1 contract directly. It retains the Preview A
ordinary, dense, startup, idle, gallery and 100-cycle disposal observations and adds two release workloads:

- a 100-entry world and a 1,000-entry world render the same indexed 100-object viewport through identical
  updated-surface captures; the receipt records all 3×200 raw samples, the maximum p95 ratio, spatial counters
  and live SVG node growth;
- a retained `SvgAnimationHost` applies continuous coordinate samples for 30 seconds while a separate animation
  frame observer records every interval and the missed-frame ratio, followed by screenshot and disposal checks.

The gallery still records paths, segment-heavy content, gradients, masks, text and repeated symbol uses rather
than treating entity or node count as a complete complexity proxy. Stable object/definition identity checks,
unchanged-camera/revision checks and lifecycle ownership counters cover generation-scoped cache retention and
disposal. The excessive-document control refuses complexity without replacing the mounted valid document.

Chromium-only stages and unavailable retained-heap/compositor observations remain explicit in the JSON. The
workflow’s hosted run is diagnostic; only a receipt whose environment satisfies every frozen reference-host
predicate is allowed to set `scaleBudgetsMet`.

## Reference-isolation correction

The first reference attempt correctly refused to start because the machine-wide one-minute load average stayed
near 5. The candidate cgroup simultaneously reported `cpu.pressure` `avg10=0.00`, zero throttled periods and zero
throttled microseconds. In this shared environment the global load includes work outside the candidate cgroup and
cannot establish contention for this measurement. Contract v2 therefore gates pre-run cgroup CPU pressure and
the across-run throttled-time delta, while retaining global load averages as diagnostic fields. This is an
explicit measurement-boundary correction; no product threshold, workload, sample count or percentile changed.

## Accepted reference result

The exact contract-v2 host receipt passed. Ordinary p95 values were 83.396, 67.090 and 83.289 ms against
100 ms; dense values were 133.185, 133.267 and 117.688 ms against 150 ms. The 10× world’s maximum p95 ratio
was 0.988 with zero live-node growth. The 30-second retained animation delivered 1,800 observed frames with
zero missed frames and disposed with zero active effects, listeners or scheduled frames. The gallery retained
200 paths, 64 gradients, 32 masks, 100 text runs and 100 symbol uses; all 100 lifecycle cycles cleared their
roots and owned resources. Startup reached its first usable captured surface in at most 227.719 ms and the
executable JS/CSS closure was 124,221 gzip bytes.
