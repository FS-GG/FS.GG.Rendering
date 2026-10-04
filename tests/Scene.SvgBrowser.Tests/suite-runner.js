import { documentReconciliationControl } from "./document-reconciliation-control.js";
import { suiteContract, validateRebuild, validateLifecycle, validateExcessive, summarize } from "./suite-contract.js";
import { hash } from "./fixture-hash.js";

const ui = Object.fromEntries(["run", "resume", "download", "progress", "results", "case"].map((id) => [id, document.getElementById(id)]));
const delay = (ms) => new Promise((done) => setTimeout(done, ms));
const method = suiteContract.method;
let manifest, manifestHash, result, checkpointKey, running = false;
const queue = suiteContract.cases.flatMap((kind) => Array.from({ length: ["ordinary", "dense", "extent-near", "extent-far"].includes(kind) ? method.repetitions : 1 }, (_, repetition) => ({ kind, repetition })));
const require = (condition, message) => { if (!condition) throw new Error(message); };
async function fetchBytes(path, signal = AbortSignal.timeout(5000)) {
  const response = await fetch(new URL(path, location.href), { cache: "no-store", signal });
  require(response.ok, `Artifact unavailable: ${path} (${response.status})`);
  return response.arrayBuffer();
}
async function verifyArtifact() {
  const signal = AbortSignal.timeout(5000);
  const bytes = await fetchBytes("artifact-manifest.json", signal);
  require(hash(bytes) === manifestHash, "Manifest changed during run");
  for (const file of manifest.files) {
    const url = new URL(file.path, location.href);
    require(url.origin === location.origin && !file.path.startsWith("/") && !file.path.split("/").includes(".."), "Invalid artifact path");
    require(hash(await fetchBytes(file.path, signal)) === file.sha256, `Artifact hash mismatch: ${file.path}`);
  }
}
function environment() {
  return {
    userAgent: navigator.userAgent, platform: navigator.platform, hardwareConcurrency: navigator.hardwareConcurrency,
    deviceMemoryGiB: navigator.deviceMemory ?? "unknown", viewport: { width: innerWidth, height: innerHeight }, devicePixelRatio,
    origin: location.origin, secureContext: isSecureContext, visibility: document.visibilityState,
    hardwareAcceleration: "unknown", gpu: "unknown", driver: "unknown", gpuTiming: "unknown", physicalInputLatency: "unknown",
    serviceWorkerController: navigator.serviceWorker?.controller?.scriptURL ?? null,
    cache: "artifact verification fetches use no-store; frame cold-load timing is separate from warmed measurements",
  };
}
function display() {
  ui.results.textContent = JSON.stringify(result, null, 2);
  ui.download.disabled = !result;
}
function checkpoint() {
  try { localStorage.setItem(checkpointKey, JSON.stringify(result)); result.checkpointStorage = "available"; }
  catch (error) { result.checkpointStorage = `unavailable: ${error.message}`; }
  display();
}
async function withFrame(entry) {
  const frame = document.createElement("iframe");
  frame.title = `${entry.kind} control fixture`;
  frame.width = "1280"; frame.height = "720";
  ui.case.replaceChildren(frame);
  const started = performance.now();
  let timer;
  let expired = false;
  const operation = async () => {
    frame.src = new URL("index.html", location.href).href;
    while (!frame.contentWindow?.svgFoundation) { if (expired) throw new Error("Case expired"); await delay(20); }
    const w = frame.contentWindow, f = w.svgFoundation, doc = w.document;
    await doc.fonts.ready;
    const coldLoadMilliseconds = performance.now() - started;
    const check = () => { require(!expired, "Case expired"); require(document.visibilityState === "visible", "Page backgrounded; rerun in a foreground tab"); };
    const nextFrame = () => new Promise((done) => w.requestAnimationFrame(done));
    const update = (index) => { check(); const t = w.performance.now(); const error = f.performanceSelect(index); require(error === null, `Update refused: ${error}`); return w.performance.now() - t; };
    if (entry.kind === "animation") {
      f.animationMount(); f.animationStartContinuous();
      let previous = await nextFrame(); const t = previous; const intervals = [];
      while (previous - t < method.continuousMilliseconds) { check(); const now = await nextFrame(); intervals.push(now - previous); previous = now; }
      const observed = f.animationObserve(); const disposed = f.animationDispose();
      validateLifecycle(disposed); require(disposed.disposed, "Animation disposal missing");
      require(observed.semanticId === "alpha" && observed.events.length > 0, "Animation semantic target/samples missing");
      return { coldLoadMilliseconds, intervals, summary: summarize(intervals), observed, disposed };
    }
    if (entry.kind === "document-reconciliation") return { coldLoadMilliseconds, ...documentReconciliationControl(w) };
    f.performanceMount(entry.kind === "lifecycle" || entry.kind === "excessive-document" ? "ordinary" : entry.kind);
    if (entry.kind === "excessive-document") { const refusal = f.performanceExcessiveDocument(); validateExcessive(refusal); return { coldLoadMilliseconds, refusal }; }
    if (entry.kind === "lifecycle") {
      const roots = [];
      for (let i = 0; i < method.lifecycleCycles; i++) { check(); f.performanceMount(i % 2 ? "ordinary" : "dense"); update(i % 100); roots.push(f.performanceDispose()); await delay(0); }
      validateLifecycle({ roots: Math.max(...roots) });
      return { coldLoadMilliseconds, cycles: method.lifecycleCycles, roots, unavailable: "Performance fixture exposes root ownership only; animation lifecycle has actual frame/listener counters" };
    }
    const root = doc.querySelector("#performance-fixture svg");
    const objects = new Map([...root.querySelectorAll("[data-fsgg-element-id]")].filter((n) => /^object-\d+$/.test(n.getAttribute("data-fsgg-element-id"))).map((n) => [n.getAttribute("data-fsgg-element-id"), n]));
    const definitions = new Map([...root.querySelectorAll("defs > [id]")].map((n) => [n.id, n]));
    if (entry.kind === "gallery") {
      const counts = { paths: root.querySelectorAll("path").length, gradients: root.querySelectorAll("linearGradient, radialGradient").length, masks: root.querySelectorAll("mask").length, text: root.querySelectorAll("text").length, uses: root.querySelectorAll("use").length };
      require(JSON.stringify(counts) === JSON.stringify({ paths: 200, gradients: 64, masks: 32, text: 100, uses: 100 }), "Gallery semantic gate");
      const exported = f.performanceExport();
      return { coldLoadMilliseconds, counts, observation: f.performanceObserve(), exportedSvgSha256: hash(exported), exportedSvgBytes: new TextEncoder().encode(exported).length };
    }
    for (let i = 0; i < method.warmups; i++) { update(i); await nextFrame(); }
    const samples = [];
    for (let i = 0; i < method.samples; i++) {
      const updateCpuMilliseconds = update(i % (entry.kind === "dense" ? 200 : 100));
      const t = w.performance.now(); await nextFrame();
      samples.push({ updateCpuMilliseconds, callbackToNextFrameMilliseconds: w.performance.now() - t });
    }
    const identity = {
      rebuiltObjects: [...objects].filter(([id,n]) => doc.querySelector(`[data-fsgg-element-id='${id}']`) !== n).length,
      rebuiltDefinitions: [...definitions].filter(([id,n]) => doc.getElementById(id) !== n).length,
    };
    validateRebuild(identity);
    return { coldLoadMilliseconds, warmupsDiscarded: method.warmups, samples, updateCpu: summarize(samples.map((s) => s.updateCpuMilliseconds)), frameCallback: summarize(samples.map((s) => s.callbackToNextFrameMilliseconds)), identity, observation: f.performanceObserve() };
  };
  try {
    return await Promise.race([operation(), new Promise((_, reject) => { timer = setTimeout(() => { expired = true; reject(new Error(`Case exceeded ${method.caseTimeoutMilliseconds}ms`)); }, method.caseTimeoutMilliseconds); })]);
  } finally { expired = true; clearTimeout(timer); frame.remove(); }
}
async function run(resume) {
  if (running) return;
  running = true; ui.run.disabled = ui.resume.disabled = true;
  if (!resume) result = { schema: "fsgg.svg-coherence.page-results/v1", runId: `${Date.now()}-${Math.random().toString(16).slice(2)}`, artifact: manifest, manifestSha256: manifestHash, environment: environment(), startedAtUtc: new Date().toISOString(), outcome: "running", cases: [], claims: { gpuQualified: false, performanceAccepted: false, representativeSoldierQualified: false } };
  else result.resumedAtUtc = new Date().toISOString();
  try {
    await verifyArtifact();
    for (let index = result.cases.length; index < queue.length; index++) {
      const entry = queue[index]; ui.progress.textContent = `${index + 1}/${queue.length}: ${entry.kind}, repetition ${entry.repetition + 1}`;
      const started = performance.now();
      try { await verifyArtifact(); const observations = await withFrame(entry); await verifyArtifact(); result.cases.push({ ...entry, outcome: "pass", observations, integrity: "verified-before-and-after" }); }
      catch (error) { result.cases.push({ ...entry, outcome: "failed", error: error.message }); }
      result.cases.at(-1).durationMilliseconds = performance.now() - started;
      checkpoint();
    }
    await verifyArtifact(); result.integrity = "verified-before-and-after";
    result.outcome = result.cases.some((c) => c.outcome === "failed") ? "failed" : "container-controls-completed";
  } catch (error) { result.outcome = "failed"; result.error = error.message; }
  finally { result.completedAtUtc = new Date().toISOString(); checkpoint(); running = false; ui.run.disabled = false; ui.progress.textContent = `${result.outcome}: ${result.cases.length}/${queue.length} cases. Download combined results.`; }
}
ui.run.onclick = () => run(false);
ui.resume.onclick = () => run(true);
ui.download.onclick = () => {
  const url = URL.createObjectURL(new Blob([JSON.stringify(result, null, 2)], { type: "application/json" }));
  const link = document.createElement("a"); link.href = url; link.download = `svg-controls-${result.runId}.json`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
};
window.svgSuite = { run: () => run(false), get result() { return result; }, get running() { return running; } };
try {
  const bytes = await fetchBytes("artifact-manifest.json"); manifestHash = hash(bytes); manifest = JSON.parse(new TextDecoder().decode(bytes));
  require(manifest.schema === "fsgg.svg-coherence.fixture/v1", "Unsupported artifact manifest");
  require(manifest.workloadSha256 === hash(JSON.stringify(suiteContract)), "Workload contract hash mismatch");
  require(manifest.artifactId === hash(JSON.stringify({ files: manifest.files, packages: manifest.packages, sourceSha256: manifest.sourceSha256, workloadSha256: manifest.workloadSha256 })), "Artifact identity mismatch");
  await verifyArtifact(); checkpointKey = `svg-suite:${manifest.artifactId}`;
  try {
    const saved = JSON.parse(localStorage.getItem(checkpointKey));
    if (saved?.manifestSha256 === manifestHash && saved.cases?.length < queue.length && saved.cases.every((c,i) => c.kind === queue[i].kind && c.repetition === queue[i].repetition)) { result = saved; ui.resume.disabled = false; display(); }
  } catch { /* Storage unavailable: fresh runs and download remain usable. */ }
  ui.run.disabled = false; ui.progress.textContent = `Ready: ${queue.length} sequential cases. ${manifest.artifactId}`;
} catch (error) {
  result = { schema: "fsgg.svg-coherence.page-results/v1", runId: `${Date.now()}-capability`, outcome: "capability-check-failed", error: error.message, environment: environment(), cases: [], claims: { gpuQualified: false, performanceAccepted: false, representativeSoldierQualified: false } };
  display();
  ui.progress.textContent = `Capability/artifact check failed: ${error.message}. Rebuild or serve the complete static fixture. Download this report.`;
}
