// Existing specimens are controls; realistic licensed glyphs join milestone .3.
export const suiteContract = {
  version: "svg-coherence-container-controls/1",
  fixture: "historical control workloads; shared symbol is a single circle",
  representativeAsset: { status: "pending-milestone-.1/.3", sha256: null, complexity: null },
  method: { warmups: 5, samples: 30, repetitions: 3, continuousMilliseconds: 1000, lifecycleCycles: 20, caseTimeoutMilliseconds: 30000 },
  cases: ["ordinary", "dense", "extent-near", "extent-far", "gallery", "lifecycle", "excessive-document", "animation"],
  timing: "browser update CPU and rAF cadence only; no pixel presentation or GPU claim",
};

export function validateRebuild(value) {
  if (value.rebuiltObjects || value.rebuiltDefinitions) throw new Error("unnecessary-rebuild gate");
}
export function validateLifecycle(value) {
  if (value.roots || value.active || value.frames || value.listeners) throw new Error("listener/resource-leak gate");
}
export function validateExcessive(value) {
  if (!value.rootRetained || !String(value.error).includes("node-limit")) throw new Error("excessive-document gate");
}
export function summarize(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const percentile = (fraction) => sorted[Math.ceil(sorted.length * fraction) - 1];
  return { sampleCount: values.length, p50: percentile(.5), p95: percentile(.95), p99: values.length >= 100 ? percentile(.99) : null, maximum: sorted.at(-1), percentileMethod: "nearest rank", p99Disposition: values.length >= 100 ? "observed" : "insufficient samples" };
}
