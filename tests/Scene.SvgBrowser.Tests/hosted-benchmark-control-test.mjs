// Inert controls for the hosted benchmark states, run validation and tolerance arithmetic.
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { benchmarkMotions, benchmarkState, selectedTicks } from "./hosted-benchmark-page.js";
import { analyse, median, validateWorkload, workloadRecords, runSchema, sustainedKey } from "./hosted-benchmark-analysis.mjs";

const workload = validateWorkload(JSON.parse(readFileSync(new URL("./hosted-benchmark-workload.json", import.meta.url), "utf8")));

// Closed-form tick selection equals brute force.
for (const stride of [2, 10]) for (let index = 0; index < 40; index++) for (let tick = 0; tick < 45; tick++) {
  let brute = 0; for (let t = 1; t <= tick; t++) if (t % stride === index % stride) brute++;
  assert.equal(selectedTicks(index, tick, stride), brute);
}

// Every tick changes exactly the declared share, inside the common viewBox.
const fraction = { static: 0, "camera-only": 0, "sparse-10": 0.1, "sparse-50": 0.5, "churn-10": 0.1 };
for (const count of [100, 250, 500]) for (const motion of benchmarkMotions) {
  let previous = benchmarkState({ count, motion }, 0);
  assert.equal(previous.units.length, count);
  for (let tick = 1; tick <= 121; tick++) {
    const state = benchmarkState({ count, motion }, tick);
    const moved = state.units.filter((u, i) => u.x !== previous.units[i].x || u.y !== previous.units[i].y).length;
    const respawned = state.units.filter((u, i) => u.id !== previous.units[i].id).length;
    assert.equal(motion.startsWith("sparse") ? moved : respawned, count * fraction[motion], `${count}/${motion}/${tick} change share`);
    assert.equal(motion.startsWith("sparse") ? respawned : moved, 0, `${count}/${motion}/${tick} unrelated change`);
    assert.equal(state.cameraX !== previous.cameraX || state.cameraY !== previous.cameraY, motion === "camera-only", `${motion} camera change`);
    assert.equal(new Set(state.units.map((u) => u.id)).size, count, "unique unit identity");
    for (const u of state.units) {
      assert.ok(u.x + state.cameraX - 6 >= 0 && u.x + state.cameraX + 6 <= 410 && u.y + state.cameraY - 9 >= 0 && u.y + state.cameraY + 6 <= 510, "unit visible in viewBox");
      assert.ok(u.health >= 1 && u.health <= 10 && [0, 1].includes(u.faction));
    }
    assert.equal(state.units.filter((u) => u.selected).length, count / 10, "selection outline share");
    previous = state;
  }
}
assert.throws(() => benchmarkState({ count: 1000, motion: "static" }, 0), /count/);
assert.throws(() => benchmarkState({ count: 100, motion: "sparse-100" }, 0), /motion/);
console.log("hosted benchmark states: exact change shares, unique identity and visible bounds: passed");

// Workload guard: the hosted harness is public-fixture only.
for (const mutate of [(w) => { w.fixture.glyph = "licensed-rifleman"; }, (w) => { w.fixture.licensedGlyphPermitted = true; }, (w) => { w.method.trials = 2; },
  (w) => { w.method.armOrders[1] = "baseline-first"; }, (w) => { w.method.browser.args = []; }, (w) => { w.budgets.hostedVerdict = "pass"; }, (w) => { w.tolerance.minimum = 0.05; }]) {
  const copy = structuredClone(workload); mutate(copy);
  assert.throws(() => validateWorkload(copy), /hosted workload/);
}

// Synthetic arm runs: per-record scale lets the controls choose each ratio.
const digest = (c) => `sha256:${c.repeat(64)}`;
const harness = { driverSha256: digest("1"), pageSha256: digest("2"), analysisSha256: digest("3"), workloadSha256: digest("4") };
const records = workloadRecords(workload);
assert.equal(records.length, 20);
const makeRun = ({ trial, arm, position, scale = () => 1, manifest = digest("a"), sustainedSeconds = 0 }) => {
  const order = workload.method.recordOrders[trial - 1], rows = [...records];
  if (order === "reverse") rows.reverse();
  const start = Date.UTC(2026, 9, 23, 0, trial * 10 + position * 5);
  const series = (name) => Array.from({ length: 100 }, (_, i) => 2 * scale(name) * (1 + (i % 5) / 100));
  return { schema: runSchema, trial, arm, position, recordOrder: order, harness, publicFixture: true, outcome: "completed", integrity: "verified-before-and-after", manifestSha256: manifest,
    clockResolutionMs: 0.005, startedAtUtc: new Date(start).toISOString(), completedAtUtc: new Date(start + 60000).toISOString(),
    claims: { budgetsEvaluated: false, gpuQualified: false, licensedGlyphMeasured: false, performanceAccepted: false },
    records: rows.map((r) => ({ ...r, warmupTicks: 20, updateMs: series(`${r.representation}/${r.count}/${r.motion}`), frameMs: Array(100).fill(16.7), nodes: { initial: 10, final: 10 } })),
    sustained: sustainedSeconds ? { ...workload.sustainedChurn, sustainedMilliseconds: sustainedSeconds * 1000, ticks: 3, updateMs: series(sustainedKey(workload)), frameMs: [16, 17, 30], nodes: { initial: 9, final: 9 } } : null };
};
const trialRuns = (candidateScale = () => () => 1, options = {}) => [1, 2, 3].flatMap((trial) => {
  const baselineFirst = trial !== 2;
  return [makeRun({ trial, arm: "baseline", position: baselineFirst ? 0 : 1, ...options }), makeRun({ trial, arm: "candidate", position: baselineFirst ? 1 : 0, scale: candidateScale(trial), ...options })];
});

// Identical arms: spread 0, the 10% minimum binds.
let summary = analyse(trialRuns(), workload, { mode: "calibration", sustainedSeconds: 0 });
assert.equal(summary.observedSpread.value, 0);
assert.deepEqual([summary.tolerance.value, summary.tolerance.bindingTerm], [0.10, "minimum"]);
assert.equal(summary.budgets.verdict, "not-evaluated");
// Spread binds: one record differs by 4%, 7%, 9% across trials; the median over trials is 7%.
const target = "shared-symbol/250/sparse-10";
const spreadScale = (trial) => (name) => (name === target ? [1.04, 1.07, 1.09][trial - 1] : 1);
summary = analyse(trialRuns(spreadScale), workload, { mode: "calibration", sustainedSeconds: 0 });
assert.ok(Math.abs(summary.observedSpread.value - 0.07) < 1e-12 && summary.observedSpread.record === target);
assert.ok(Math.abs(summary.observedSpread.perTrialMaximum - 0.09) < 1e-12);
assert.ok(Math.abs(summary.tolerance.value - 0.14) < 1e-12 && summary.tolerance.bindingTerm === "spread");
// A faster arm counts as spread too.
summary = analyse(trialRuns(() => (name) => (name === target ? 0.94 : 1)), workload, { mode: "calibration", sustainedSeconds: 0 });
assert.ok(Math.abs(summary.tolerance.value - 0.12) < 1e-12);
// Sustained churn is a gated record when present.
summary = analyse(trialRuns(() => (name) => (name === sustainedKey(workload) ? 1.2 : 1), { sustainedSeconds: 5 }), workload, { mode: "calibration", sustainedSeconds: 5 });
assert.ok(Math.abs(summary.tolerance.value - 0.4) < 1e-12 && summary.records.length === 21);
// Resolution floor excludes records whose medians cannot be resolved.
assert.throws(() => analyse(trialRuns().map((r) => ({ ...r, clockResolutionMs: 0.5 })), workload, { mode: "calibration", sustainedSeconds: 0 }), /resolution floor/);
const floorRuns = trialRuns().map((r) => ({ ...r, records: r.records.map((row) => (row.motion === "static" ? { ...row, updateMs: row.updateMs.map(() => 0.01) } : row)) }));
summary = analyse(floorRuns, workload, { mode: "calibration", sustainedSeconds: 0 });
assert.equal(summary.excludedRecordCount, 4);
// Gate verdicts.
summary = analyse(trialRuns(() => (name) => (name === target ? 1.2 : 1)), workload, { mode: "gate", tolerance: 0.14, sustainedSeconds: 0 });
assert.deepEqual([summary.gate.verdict, summary.gate.regressions], ["regression", [target]]);
summary = analyse(trialRuns(() => (name) => (name === target ? 1.1 : 1)), workload, { mode: "gate", tolerance: 0.14, sustainedSeconds: 0 });
assert.equal(summary.gate.verdict, "pass");
// One-trial smoke sets nothing.
const smoke = [makeRun({ trial: 1, arm: "baseline", position: 0 }), makeRun({ trial: 1, arm: "candidate", position: 1, manifest: digest("b") })];
summary = analyse(smoke, workload, { mode: "smoke", sustainedSeconds: 0 });
assert.ok(!summary.tolerance && !summary.gate && summary.smoke.verdict === "none");
console.log("hosted benchmark tolerance max(2 x spread, 10%), resolution floor and gate verdicts: passed");

// Refusals.
const runs = trialRuns();
const refuse = (mutated, pattern, options = { mode: "calibration", sustainedSeconds: 0 }) => assert.throws(() => analyse(mutated, workload, options), pattern);
refuse(runs.slice(1), /run count/);
refuse(runs.map((r) => (r.trial === 2 && r.arm === "baseline" ? { ...r, position: 0 } : r)), /arm order/);
refuse(runs.map((r, i) => (i === 3 ? { ...r, harness: { ...harness, pageSha256: digest("9") } } : r)), /harness differs/);
refuse(runs.map((r, i) => (i === 1 ? { ...r, manifestSha256: digest("b") } : r)), /one artifact/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, records: [...r.records].reverse() } : r)), /record order|record identity/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, records: r.records.map((row, j) => (j === 0 ? { ...row, updateMs: row.updateMs.slice(1) } : row)) } : r)), /samples/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, records: r.records.map((row, j) => (j === 0 ? { ...row, nodes: { initial: 10, final: 11 } } : row)) } : r)), /live node growth/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, outcome: "failed" } : r)), /incomplete/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, claims: { ...r.claims, budgetsEvaluated: true } } : r)), /claims/);
refuse(runs.map((r, i) => (i === 0 ? { ...r, publicFixture: false } : r)), /claims/);
refuse(runs.map((r, i) => (i === 1 ? { ...r, startedAtUtc: runs[0].startedAtUtc } : r)), /overlapped/);
refuse(runs, /sustained/, { mode: "calibration", sustainedSeconds: 5 });
refuse(runs, /calibrated tolerance/, { mode: "gate", sustainedSeconds: 0 });
refuse(runs, /calibrated tolerance/, { mode: "gate", tolerance: 0.05, sustainedSeconds: 0 });
refuse(runs, /only a gate/, { mode: "calibration", tolerance: 0.2, sustainedSeconds: 0 });
assert.equal(median([3, 1, 2]), 2); assert.equal(median([4, 1, 2, 3]), 2.5);
console.log("hosted benchmark run identity, alternation, coverage, A/A artifact and claim refusals: passed (inert)");
