// Existing specimens are controls; realistic licensed glyphs join milestone .3.
export const suiteContract = {
  version: "svg-coherence-container-controls/2",
  fixture: "historical control workloads; shared symbol is a single circle",
  representativeAsset: { status: "pending-milestone-.1/.3", sha256: null, complexity: null },
  method: { warmups: 5, samples: 30, repetitions: 3, continuousMilliseconds: 1000, lifecycleCycles: 20, caseTimeoutMilliseconds: 30000 },
  cases: ["ordinary", "dense", "extent-near", "extent-far", "gallery", "lifecycle", "excessive-document", "animation", "document-reconciliation"],
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

// Functional application coverage is a separate, explicitly selected workload.
export const soldierFunctionalContract = {
  version: "svg-coherence-soldier-functional/1",
  profile: "soldier-functional",
  fixture: "genuine opt-in Templates Complete production player",
  method: { caseTimeoutMilliseconds: 60000 },
  cases: ["soldier-local", "soldier-external", ...[1, 100, 250, 500, 1000].map((count) => `soldier-population-${count}`), "soldier-churn"],
  timing: "functional only; no timing samples, CPU/allocation trace, benchmark or GPU claim",
};

export function validateSoldierInput(value) {
  const require = (condition, message) => { if (!condition) throw new Error(message); };
  require(value?.schema === "fsgg.soldier-staged-input/v1", "soldier input schema");
  for (const field of ["renderingRevision", "templatesRevision"]) require(/^[0-9a-f]{40}$/.test(value[field]), `soldier source identity: ${field}`);
  for (const field of ["producerManifestSha256", "restoreAssetsSha256", "actualProgramSha256", "referenceSha256", "provenanceSha256", "assetSha256", "adapterSha256"]) require(/^sha256:[0-9a-f]{64}$/.test(value[field]), `soldier digest: ${field}`);
  require(value.actualEntry === "soldier-player/index.html" && value.adapterEntry === "soldier-staged-journey.js", "wrong actual player entry");
  require(value.candidateVersion === "0.32.0" && value.evaluatedProperties?.FsGgExternalReferenceCandidate === "true"
    && value.evaluatedProperties?.FsGgSvgRuntimeCandidate === "true" && value.evaluatedProperties?.FsGgSvgInputCandidate === "true"
    && value.evaluatedProperties?.FsGgLegacyPreview !== "true", "soldier opt-in/runtime flags missing");
  require(value.packageArchives?.length === 3 && value.packageArchives.every((row) => typeof row.name === "string" && /^sha256:[0-9a-f]{64}$/.test(row.sha256)), "soldier package identities missing");
  return value;
}

export function workloadForProfile(profile, input) {
  if (profile === "controls") return suiteContract;
  if (profile !== "soldier-functional") throw new Error("Unsupported suite profile");
  return { ...soldierFunctionalContract, input: validateSoldierInput(input) };
}

export function validateSoldierCase(value) {
  if (!Array.isArray(value?.assertions) || value.assertions.length === 0 || !value.assertions.every((row) => typeof row.name === "string" && row.name.length > 0 && row.passed === true)) throw new Error("soldier assertions incomplete");
  if (value.skipped !== false || value.retried !== false || value.flaky !== false) throw new Error("soldier skipped/retried/flaky case");
  if (value.cleanup?.frameDisposed !== true || value.cleanup?.owned !== 0) throw new Error("soldier listener/resource-leak gate");
}

export function validateSoldierResult(result) {
  if (result.integrity !== "verified-before-and-after" || result.outcome !== "soldier-functional-completed") throw new Error("soldier suite incomplete");
  if (result.cases?.length !== soldierFunctionalContract.cases.length) throw new Error("soldier case missing");
  result.cases.forEach((row, index) => {
    if (row.kind !== soldierFunctionalContract.cases[index] || row.repetition !== 0 || row.outcome !== "pass" || row.integrity !== "verified-before-and-after") throw new Error("soldier case identity/order/failure");
    validateSoldierCase(row.observations);
  });
  if (result.claims?.gpuQualified !== false || result.claims?.performanceAccepted !== false || result.claims?.representativeSoldierQualified !== false) throw new Error("unsupported soldier acceptance claim");
}
