import assert from "node:assert/strict";
import { createHash, randomBytes } from "node:crypto";
import { portableSha256 } from "./fixture-hash.js";
import { validateRebuild, validateLifecycle, validateExcessive, summarize } from "./suite-contract.js";

for (const length of [0, 1, 55, 56, 63, 64, 65, 1024, 100000]) {
  const bytes = randomBytes(length);
  assert.equal(portableSha256(bytes), `sha256:${createHash("sha256").update(bytes).digest("hex")}`);
}
validateRebuild({ rebuiltObjects: 0, rebuiltDefinitions: 0 });
assert.throws(() => validateRebuild({ rebuiltObjects: 1 }), /unnecessary-rebuild gate/);
validateLifecycle({ roots: 0, active: 0, frames: 0, listeners: 0 });
assert.throws(() => validateLifecycle({ listeners: 1 }), /listener\/resource-leak gate/);
validateExcessive({ rootRetained: true, error: "node-limit" });
assert.throws(() => validateExcessive({ rootRetained: false, error: null }), /excessive-document gate/);
assert.equal(summarize(Array.from({ length: 30 }, (_, i) => i)).p99, null);
console.log("page control predicates and portable artifact hashes: passed");

const { soldierFunctionalContract, workloadForProfile, validateSoldierInput, validateSoldierCase, validateSoldierResult } = await import("./suite-contract.js");
const digest = "sha256:" + "a".repeat(64);
const input = { schema: "fsgg.soldier-staged-input/v1", renderingRevision: "b".repeat(40), templatesRevision: "c".repeat(40), producerManifestSha256: digest, restoreAssetsSha256: digest, actualProgramSha256: digest, referenceSha256: digest, provenanceSha256: digest, assetSha256: digest, adapterSha256: digest, actualEntry: "soldier-player/index.html", adapterEntry: "soldier-staged-journey.js", candidateVersion: "0.32.0", evaluatedProperties: { FsGgExternalReferenceCandidate: "true", FsGgSvgRuntimeCandidate: "true", FsGgSvgInputCandidate: "true", FsGgSvgNetworkCandidate: "true", FsGgLegacyPreview: "" }, packageArchives: ["Scene", "KeyboardInput", "Scene.SvgBrowser"].map((name) => ({ name, sha256: digest })) };
validateSoldierInput(input);
assert.equal(workloadForProfile("controls"), (await import("./suite-contract.js")).suiteContract);
assert.equal(workloadForProfile("soldier-functional", input).cases.length, 8);
assert.equal(soldierFunctionalContract.method.caseTimeoutMilliseconds, 60000);
for (const field of ["renderingRevision", "templatesRevision", "producerManifestSha256", "restoreAssetsSha256", "actualProgramSha256", "referenceSha256", "provenanceSha256", "assetSha256", "adapterSha256", "actualEntry", "packageArchives"]) assert.throws(() => validateSoldierInput({ ...input, [field]: null }));
assert.throws(() => validateSoldierInput({ ...input, evaluatedProperties: { ...input.evaluatedProperties, FsGgExternalReferenceCandidate: "false" } }), /opt-in/);
assert.notEqual(JSON.stringify(workloadForProfile("soldier-functional", input)), JSON.stringify(workloadForProfile("soldier-functional", { ...input, assetSha256: "sha256:" + "d".repeat(64) })));
const observation = { kind: "soldier-local", assertions: [{ name: "actual product assertion", passed: true }], skipped: false, retried: false, flaky: false, cleanup: { owned: 0, frameDisposed: true } };
validateSoldierCase(observation);
for (const value of [{ ...observation, assertions: [] }, { ...observation, skipped: true }, { ...observation, retried: true }, { ...observation, flaky: true }, { ...observation, cleanup: { owned: 1, frameDisposed: true } }, { ...observation, cleanup: { owned: 0, frameDisposed: false } }]) assert.throws(() => validateSoldierCase(value));
const result = { outcome: "soldier-functional-completed", integrity: "verified-before-and-after", claims: { gpuQualified: false, performanceAccepted: false, representativeSoldierQualified: false }, cases: soldierFunctionalContract.cases.map((kind) => ({ kind, repetition: 0, outcome: "pass", integrity: "verified-before-and-after", observations: { ...observation, kind } })) };
validateSoldierResult(result);
assert.throws(() => validateSoldierResult({ ...result, cases: result.cases.slice(1) }), /case missing/);
assert.throws(() => validateSoldierResult({ ...result, cases: [...result.cases].reverse() }), /identity/);
assert.throws(() => validateSoldierResult({ ...result, claims: { ...result.claims, gpuQualified: true } }), /unsupported/);
console.log("soldier functional contract, identity, complete cases and negative controls: passed (inert)");

// Exercise the real packager on inert files: metadata must affect the workload/artifact.
const fs = await import("node:fs"), os = await import("node:os"), path = await import("node:path"), child = await import("node:child_process");
const fixtureRoot = fs.mkdtempSync(path.join(os.tmpdir(), "soldier-stage-control-"));
try {
  const adapter = path.join(fixtureRoot, "adapter.js"), player = path.join(fixtureRoot, "player");
  fs.writeFileSync(adapter, "export const inert = true;\n"); fs.mkdirSync(player);
  fs.writeFileSync(path.join(player, "index.html"), '<script type="module" src="/soldier-player/assets/player.js"></script>');
  const stage = (label, metadata) => {
    const work = path.join(fixtureRoot, label); fs.mkdirSync(path.join(work, "Browser/dist"), { recursive: true }); fs.mkdirSync(path.join(work, "feed"));
    fs.writeFileSync(path.join(work, "Browser/dist/suite.html"), "inert harness"); fs.writeFileSync(path.join(work, "source-manifest.tsv"), "inert source identity");
    const metadataPath = path.join(work, "input.json"); fs.writeFileSync(metadataPath, JSON.stringify(metadata));
    const result = child.spawnSync(process.execPath, [new URL("./package-fixture.mjs", import.meta.url).pathname, "--work", work, "--profile", "soldier-functional", "--player-dist", player, "--adapter", adapter, "--input-metadata", metadataPath], { encoding: "utf8" });
    return { result, work };
  };
  const metadata = { ...input, adapterSha256: `sha256:${createHash("sha256").update(fs.readFileSync(adapter)).digest("hex")}` };
  const first = stage("first", metadata); assert.equal(first.result.status, 0, first.result.stderr);
  const firstManifest = JSON.parse(fs.readFileSync(path.join(first.work, "Browser/dist/artifact-manifest.json")));
  const second = stage("second", { ...metadata, templatesRevision: "e".repeat(40) }); assert.equal(second.result.status, 0, second.result.stderr);
  const secondManifest = JSON.parse(fs.readFileSync(path.join(second.work, "Browser/dist/artifact-manifest.json")));
  assert.notEqual(firstManifest.workloadSha256, secondManifest.workloadSha256); assert.notEqual(firstManifest.artifactId, secondManifest.artifactId);
  assert(firstManifest.files.some((file) => file.path === "soldier-player/index.html")); assert(firstManifest.files.some((file) => file.path === "soldier-staged-journey.js"));
  assert(!firstManifest.files.some((file) => file.path === "artifact-manifest.json"));
  assert.notEqual(stage("wrong-entry", { ...metadata, actualEntry: "index.html" }).result.status, 0);
  assert.notEqual(stage("wrong-adapter", { ...metadata, adapterSha256: digest }).result.status, 0);
  assert.notEqual(stage("missing-optin", { ...metadata, evaluatedProperties: {} }).result.status, 0);
} finally { fs.rmSync(fixtureRoot, { recursive: true, force: true }); }
console.log("actual staged packager rejects entry/adapter/opt-in drift and hashes both source identities: passed (inert)");

// Execute the real withFrame function with inert DOM ownership, retaining the first failure.
const { runInNewContext } = await import("node:vm");
const runnerSource = fs.readFileSync(new URL("./suite-runner.js", import.meta.url), "utf8");
const frameFunction = runnerSource.slice(runnerSource.indexOf("async function withFrame(entry)"), runnerSource.indexOf("async function run(resume)"));
for (const removed of [true, false]) {
  const firstFailure = new Error("first product assertion failed");
  firstFailure.cleanup = { owned: 3, frameDisposed: false };
  firstFailure.observations = { assertions: [{ name: "first product assertion", passed: false }], cleanup: { owned: 3, frameDisposed: false } };
  const frame = { isConnected: true, remove() { this.isConnected = !removed; }, contentDocument: { fonts: { ready: Promise.resolve() } }, contentWindow: { svgFoundation: { performanceMount() { throw firstFailure; } } } };
  frame.contentWindow.document = frame.contentDocument;
  const execute = runInNewContext(`(${frameFunction})`, {
    document: { createElement: () => frame }, ui: { case: { replaceChildren() {} } },
    performance: { now: () => 0 }, profile: "controls", location: { href: "http://inert.invalid/suite.html" },
    method: { caseTimeoutMilliseconds: 1000 }, URL, setTimeout, clearTimeout,
  });
  await assert.rejects(execute({ kind: "lifecycle" }), (error) => {
    assert.equal(error, firstFailure);
    assert.equal(error.message, "first product assertion failed");
    assert.equal(error.cleanup.owned, 3);
    assert.equal(error.cleanup.frameDisposed, removed);
    assert.equal(error.observations.cleanup.frameDisposed, removed);
    assert.equal(error.observations.assertions[0].passed, false);
    return true;
  });
}
console.log("failed frame diagnostic follows actual removal and retains first failure: passed (inert)");
