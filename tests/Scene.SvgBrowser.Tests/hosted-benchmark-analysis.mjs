// Pure validation and statistics for the hosted public-fixture software benchmark.
// Budgets B1-B8 are never judged here: they are judged only at SVG-COHERENCE-01.7 on Home.

export const runSchema = "fsgg.svg-hosted-benchmark-run/v1";
export const summarySchema = "fsgg.svg-hosted-benchmark-summary/v1";
const sha = /^sha256:[0-9a-f]{64}$/;

export function validateWorkload(workload) {
  const ensure = (ok, detail) => { if (!ok) throw new Error(`hosted workload: ${detail}`); };
  ensure(workload?.schema === "fsgg.svg-hosted-software-benchmark/v1", "schema");
  // The licensed tactical/rifleman geometry never reaches a hosted runner.
  ensure(workload.fixture?.glyph === "public-circle" && workload.fixture.licence === "public" && workload.fixture.licensedGlyphPermitted === false, "public fixture only");
  const m = workload.method;
  ensure(m?.trials === 3 && m.armOrders?.length === 3 && m.recordOrders?.length === 3, "three trials");
  ensure(JSON.stringify(m.armOrders) === JSON.stringify(["baseline-first", "candidate-first", "baseline-first"]), "alternated arm order");
  ensure(JSON.stringify(m.recordOrders) === JSON.stringify(["forward", "reverse", "forward"]), "alternated record order");
  ensure(m.warmupTicks === 20 && m.measuredTicks === 100, "20 warm-up and 100 measured ticks");
  ensure(JSON.stringify(m.browser?.args) === JSON.stringify(["--disable-gpu"]), "software browser profile");
  ensure(workload.tolerance?.spreadMultiplier === 2 && workload.tolerance.minimum === 0.10, "tolerance rule");
  ensure(workload.budgets?.hostedVerdict === "not-evaluated", "budgets are not judged on hosted runners");
  ensure(workload.sustainedChurn?.motion === "churn-10" && workload.sustainedChurn.count === 250, "sustained churn record");
  return workload;
}

export function workloadRecords(workload) {
  return workload.records.flatMap(({ representation, counts }) => counts.flatMap((count) => workload.motions.map((motion) => ({ representation, count, motion }))));
}

const key = (r) => `${r.representation}/${r.count}/${r.motion}`;
export const sustainedKey = (w) => `sustained/${key(w.sustainedChurn)}`;

export function median(values) {
  const sorted = [...values].sort((a, b) => a - b), middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}
export function percentile(values, fraction) {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.max(0, Math.ceil(sorted.length * fraction) - 1)];
}

export function validateRun(run, workload, { trial, arm, sustainedSeconds }) {
  const ensure = (ok, detail) => { if (!ok) throw new Error(`hosted run trial ${trial} ${arm}: ${detail}`); };
  ensure(run?.schema === runSchema && run.trial === trial && run.arm === arm, "identity");
  ensure(run.integrity === "verified-before-and-after" && run.outcome === "completed", "incomplete or unverified run");
  ensure(sha.test(run.manifestSha256) && ["driverSha256", "pageSha256", "workloadSha256"].every((name) => sha.test(run.harness?.[name])), "artifact/harness digests");
  ensure(run.publicFixture === true && run.claims?.budgetsEvaluated === false && run.claims?.gpuQualified === false, "claims");
  ensure(Number.isFinite(run.clockResolutionMs) && run.clockResolutionMs > 0, "clock resolution");
  const expected = workloadRecords(workload);
  if (run.recordOrder === "reverse") expected.reverse();
  ensure(run.recordOrder === workload.method.recordOrders[trial - 1], "record order");
  ensure(Array.isArray(run.records) && run.records.length === expected.length, "record coverage");
  run.records.forEach((row, index) => {
    ensure(key(row) === key(expected[index]), `record identity/order at ${index}`);
    ensure(row.updateMs?.length === workload.method.measuredTicks && row.warmupTicks === workload.method.warmupTicks, `samples ${key(row)}`);
    ensure(row.updateMs.every((v) => Number.isFinite(v) && v >= 0), `sample values ${key(row)}`);
    ensure(row.nodes?.initial === row.nodes?.final, `live node growth ${key(row)}`);
  });
  if (sustainedSeconds > 0) {
    const s = run.sustained;
    ensure(s && key(s) === key(workload.sustainedChurn) && s.sustainedMilliseconds === sustainedSeconds * 1000, "sustained churn identity");
    ensure(s.updateMs?.length > 0 && s.updateMs.every((v) => Number.isFinite(v) && v >= 0) && s.nodes?.initial === s.nodes?.final, "sustained churn samples");
  } else ensure(run.sustained === null, "unexpected sustained run");
}

// mode: calibration (A/A, three trials), gate (A/B, three trials, tolerance required) or smoke (one trial, no verdict).
export function analyse(runs, workload, { mode, tolerance, sustainedSeconds }) {
  validateWorkload(workload);
  if (!["calibration", "gate", "smoke"].includes(mode)) throw new Error("hosted analysis: unknown mode");
  const trials = mode === "smoke" ? 1 : workload.method.trials;
  if (mode === "gate" && !(Number.isFinite(tolerance) && tolerance >= workload.tolerance.minimum)) throw new Error("hosted analysis: gate requires a calibrated tolerance");
  if (mode !== "gate" && tolerance !== undefined) throw new Error("hosted analysis: only a gate consumes a tolerance");
  if (runs.length !== trials * 2) throw new Error("hosted analysis: run count");
  const harness = JSON.stringify(runs[0].harness);
  const byTrial = [];
  for (let trial = 1; trial <= trials; trial++) {
    const pair = runs.filter((r) => r.trial === trial);
    const baseline = pair.find((r) => r.arm === "baseline"), candidate = pair.find((r) => r.arm === "candidate");
    if (pair.length !== 2 || !baseline || !candidate) throw new Error(`hosted analysis: trial ${trial} needs one run per arm`);
    for (const [run, arm] of [[baseline, "baseline"], [candidate, "candidate"]]) validateRun(run, workload, { trial, arm, sustainedSeconds });
    const baselineFirst = workload.method.armOrders[trial - 1] === "baseline-first";
    if (baseline.position !== (baselineFirst ? 0 : 1) || candidate.position !== (baselineFirst ? 1 : 0)) throw new Error(`hosted analysis: trial ${trial} arm order`);
    if (Date.parse(pair.find((r) => r.position === 0).completedAtUtc) > Date.parse(pair.find((r) => r.position === 1).startedAtUtc)) throw new Error(`hosted analysis: trial ${trial} runs overlapped`);
    if (JSON.stringify(baseline.harness) !== harness || JSON.stringify(candidate.harness) !== harness) throw new Error("hosted analysis: harness differs between runs");
    if (mode === "calibration" && baseline.manifestSha256 !== candidate.manifestSha256) throw new Error("hosted analysis: A/A arms must serve one artifact");
    byTrial.push({ baseline, candidate });
  }
  const floor = workload.method.resolutionFloorMultiple * Math.max(...runs.map((r) => r.clockResolutionMs));
  const series = (run, name) => name.startsWith("sustained/") ? run.sustained.updateMs : run.records.find((row) => key(row) === name).updateMs;
  const names = [...workloadRecords(workload).map(key), ...(sustainedSeconds > 0 ? [sustainedKey(workload)] : [])];
  const records = names.map((name) => {
    const trialsOut = byTrial.map(({ baseline, candidate }, index) => {
      const b = series(baseline, name), c = series(candidate, name);
      const baselineMedian = median(b), candidateMedian = median(c);
      return { trial: index + 1, baselineMedian, candidateMedian, baselineP95: percentile(b, .95), candidateP95: percentile(c, .95), ratio: baselineMedian > 0 ? candidateMedian / baselineMedian : null };
    });
    const gated = trialsOut.every((t) => t.baselineMedian >= floor && t.candidateMedian >= floor && t.ratio !== null);
    const statistic = gated ? median(trialsOut.map((t) => t.ratio)) : null;
    return { record: name, gated, excluded: gated ? null : `a trial median is below ${floor.toFixed(4)} ms (resolution floor)`, statistic, deviation: gated ? Math.abs(statistic - 1) : null, trials: trialsOut };
  });
  const gatedRecords = records.filter((r) => r.gated);
  if (gatedRecords.length === 0) throw new Error("hosted analysis: no record above the resolution floor");
  const summary = {
    schema: summarySchema, mode, trials, decision: workload.decision, harness: runs[0].harness,
    artifacts: { baseline: [...new Set(runs.filter((r) => r.arm === "baseline").map((r) => r.manifestSha256))], candidate: [...new Set(runs.filter((r) => r.arm === "candidate").map((r) => r.manifestSha256))] },
    resolutionFloorMs: floor, gatedRecordCount: gatedRecords.length, excludedRecordCount: records.length - gatedRecords.length,
    budgets: { version: workload.budgets.version, verdict: "not-evaluated", judgedOnly: workload.budgets.judgedOnly },
    claims: { budgetsEvaluated: false, gpuQualified: false, licensedGlyphMeasured: false, performanceAccepted: false },
    records,
  };
  const worst = gatedRecords.reduce((a, b) => (b.deviation > a.deviation ? b : a));
  summary.observedSpread = { value: worst.deviation, record: worst.record, perTrialMaximum: Math.max(...gatedRecords.flatMap((r) => r.trials.map((t) => Math.abs(t.ratio - 1)))) };
  if (mode === "calibration") {
    const derived = workload.tolerance.spreadMultiplier * worst.deviation;
    summary.tolerance = { value: Math.max(derived, workload.tolerance.minimum), derivedFromSpread: derived, minimum: workload.tolerance.minimum, rule: workload.tolerance.rule, bindingTerm: derived > workload.tolerance.minimum ? "spread" : "minimum" };
  }
  if (mode === "gate") {
    const regressions = gatedRecords.filter((r) => r.statistic > 1 + tolerance).map((r) => r.record);
    summary.gate = { tolerance, verdict: regressions.length ? "regression" : "pass", regressions, improvements: gatedRecords.filter((r) => r.statistic < 1 - tolerance).map((r) => r.record) };
  }
  if (mode === "smoke") summary.smoke = { verdict: "none", note: "one trial exercises the harness end to end; it sets no tolerance and judges nothing" };
  if (sustainedSeconds > 0) summary.sustainedObservation = runs.map((r) => ({ trial: r.trial, arm: r.arm, ticks: r.sustained.ticks, updateP50: median(r.sustained.updateMs), updateP95: percentile(r.sustained.updateMs, .95), frameIntervalsOver25Ms: r.sustained.frameMs.filter((v) => v > 25).length / r.sustained.frameMs.length, note: "software control observation, not a B5/B7 verdict" }));
  return summary;
}

export function markdown(summary) {
  const pct = (v) => (v === null || v === undefined ? "—" : `${(v * 100).toFixed(2)}%`);
  const lines = [`## Hosted public-fixture SVG software benchmark (${summary.mode})`, "",
    `Public fixture only; software profile (\`--disable-gpu\`, one logical CPU). Budgets ${summary.budgets.version}: ${summary.budgets.verdict} here (${summary.budgets.judgedOnly}).`, "",
    `Gated records: ${summary.gatedRecordCount}; excluded below the ${summary.resolutionFloorMs.toFixed(4)} ms resolution floor: ${summary.excludedRecordCount}.`,
    `Observed spread: ${pct(summary.observedSpread.value)} (${summary.observedSpread.record}); largest single-trial deviation ${pct(summary.observedSpread.perTrialMaximum)}.`];
  if (summary.tolerance) lines.push(`**Tolerance: ${pct(summary.tolerance.value)}** = max(2 × ${pct(summary.observedSpread.value)}, ${pct(summary.tolerance.minimum)}); binding term: ${summary.tolerance.bindingTerm}.`);
  if (summary.gate) lines.push(`**Gate: ${summary.gate.verdict}** at tolerance ${pct(summary.gate.tolerance)}; regressions: ${summary.gate.regressions.join(", ") || "none"}.`);
  lines.push("", "| Record | Gated | Statistic | Per-trial ratios | Baseline medians (ms) |", "| --- | --- | --- | --- | --- |");
  for (const r of summary.records) lines.push(`| ${r.record} | ${r.gated ? "yes" : "no"} | ${r.statistic === null ? "—" : r.statistic.toFixed(4)} | ${r.trials.map((t) => (t.ratio === null ? "—" : t.ratio.toFixed(4))).join(", ")} | ${r.trials.map((t) => t.baselineMedian.toFixed(3)).join(", ")} |`);
  return `${lines.join("\n")}\n`;
}
