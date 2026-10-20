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

// Run the driver's actual acceptance predicate without importing or launching Playwright.
const driverSource = fs.readFileSync(new URL("./drive-suite.mjs", import.meta.url), "utf8");
const resumeFunction = driverSource.slice(driverSource.indexOf("function requireObservedResume("), driverSource.indexOf("const argument ="));
const requireObservedResume = runInNewContext(`(${resumeFunction})`);
requireObservedResume({ driver: { resumeObserved: true } }, true);
for (const driver of [{ resumeObserved: false }, {}, undefined]) {
  assert.throws(() => requireObservedResume({ driver }, true), /resume was not observed/);
}
requireObservedResume({ driver: { resumeObserved: false } }, false);
assert.match(driverSource, /requireObservedResume\(result, !mutant\)/);
console.log("staged positive driver refuses unobserved resume: passed (inert)");

// Execute the maintained command oracle and downloaded-result gates without a renderer.
const { instanceResearchStates, assertInstanceResearchHit } = await import("./document-reconciliation-control.js");
{
  let visible = false;
  const point = {id:"offscreen-instance",x:10,y:10};
  const node = {scrollIntoView:()=>{visible=true;}};
  const requireHit = (ok, detail) => {if(!ok)throw Error(detail);};
  const f = {documentHit:(x,y)=>visible && x===10 && y===10 ? point.id : null};
  assertInstanceResearchHit(f,node,point,requireHit);
  visible=false;
  assert.throws(()=>assertInstanceResearchHit(f,{scrollIntoView:()=>{}},point,requireHit),/instance hit identity/);
  assert.throws(()=>assertInstanceResearchHit({documentHit:()=>"wrong-instance"},node,point,requireHit),/instance hit identity/);
}
const { suiteContract, instanceResearchOrder, validateInstanceResearchResult, validateInstanceResearchSelection } = await import("./suite-contract.js");
const corpus = suiteContract.instanceResearch;
assert.deepEqual(corpus.counts, [100,250,500]);
assert.deepEqual(corpus.viewBox, [0,0,410,510]);
assert.deepEqual(corpus.trialOrders, ["shared-first","expanded-first","shared-first"]);
for (const count of corpus.counts) {
  const states = instanceResearchStates(corpus,count);
  assert.equal(states.length,6);
  const [initial,moved,defined,removed,created,reset] = states;
  assert.equal(initial.instances.length,count);
  assert.equal(moved.instances.filter((v,i) => v.x !== initial.instances[i].x || v.y !== initial.instances[i].y).length,count/10);
  assert(moved.instances.every((v,i) => v.x-initial.instances[i].x === (i%10 === 0 ? 7 : 0) && v.y-initial.instances[i].y === (i%10 === 0 ? 2 : 0)));
  assert.equal(defined.radius,5); assert.equal(removed.instances.length,count-5);
  assert.deepEqual(removed.instances.map(v=>v.id),defined.instances.filter(v=>![1,21,41,61,81].includes(v.index)).map(v=>v.id));
  assert.deepEqual(created.instances.slice(-5).map(v=>v.id),[1,21,41,61,81].map(i=>`research-${i}-1`));
  assert.equal(reset.radius,3);
  for (const state of states) assert(state.instances.every(v=>v.x-state.radius>=0 && v.y-state.radius>=0 && v.x+state.radius<=410 && v.y+state.radius<=510));
}
for (const count of [0,249,501,1000]) assert.throws(()=>instanceResearchStates(corpus,count),/count/);
const clipped = structuredClone(corpus);clipped.commands[1].dy=500;
assert.throws(()=>instanceResearchStates(clipped,500),/visible geometry bounds/);
const badViewport=structuredClone(corpus);badViewport.viewBox[3]=270;
assert.throws(()=>instanceResearchStates(badViewport,500),/common viewBox/);
const nonFinite=structuredClone(corpus);nonFinite.commands[1].dx=NaN;
assert.throws(()=>instanceResearchStates(nonFinite,500),/sparse motion/);
for (const order of [undefined,null,"","arbitrary","--out"]) assert.throws(()=>instanceResearchOrder(order),/Unknown instance research order/);
assert.deepEqual(instanceResearchOrder("expanded-first"),["expanded-geometry","shared-symbol"]);
const rowsFor = order => corpus.counts.flatMap(count=>instanceResearchOrder(order).flatMap(variant=>instanceResearchStates(corpus,count).map(state=>({count,variant,command:state.name,instances:state.instances.length,timing:{...corpus.measurement,samplesMs:[0,1,2,3,4]}}))));
for (const order of corpus.trialOrders) {
  const rows=rowsFor(order);validateInstanceResearchResult(rows,order);
  assert.equal(rows.length,36);assert.equal(rows.reduce((n,r)=>n+r.timing.samplesMs.length,0),180);
  validateInstanceResearchSelection({environment:{instanceResearchOrder:order}},order);
  assert.throws(()=>validateInstanceResearchSelection({environment:{instanceResearchOrder:order}},order === "shared-first" ? "expanded-first" : "shared-first"),/selection mismatch/);
  assert.throws(()=>validateInstanceResearchResult(rows.slice(1),order),/coverage/);
  assert.throws(()=>validateInstanceResearchResult([...rows].reverse(),order),/identity/);
  for (const mutate of [r=>r[0].count=250,r=>r[0].instances--,r=>r[0].command="unknown",r=>r[0].timing.samplesMs.pop(),r=>r[0].timing.samplesMs[0]=NaN,r=>r[0].timing.samplesMs[0]=-1,r=>r[0].timing.warmupPasses=0,r=>r[0].timing.scope="CPU"]) {
    const altered=structuredClone(rows);mutate(altered);assert.throws(()=>validateInstanceResearchResult(altered,order));
  }
}
assert.equal(corpus.trialOrders.flatMap(rowsFor).length,108);
assert.equal(corpus.trialOrders.flatMap(rowsFor).flatMap(row=>row.timing.samplesMs).length,540);
console.log("count-scaling command oracle, visible bounds, 108/540 census, order and result refusals: passed (inert; no renderer/timing acceptance)");

// Parse the actual driver's pre-launch option block; no Playwright import or launch.
const optionBlock=driverSource.slice(driverSource.indexOf("const argument ="),driverSource.indexOf("// This driver is explicitly"));
const parseDriver = extra => runInNewContext(`${optionBlock}\n({ order:selectedInstanceOrder, url:url.href })`,{process:{argv:["node","drive-suite.mjs","--base-url","http://inert.invalid/","--out","unused.json",...extra]},URL,instanceResearchOrder});
assert.equal(parseDriver([]).order,"shared-first");
assert.equal(new URL(parseDriver(["--instance-order","expanded-first"]).url).searchParams.get("instance-order"),"expanded-first");
for (const extra of [["--instance-order"],["--instance-order","arbitrary"],["--profile","soldier-functional","--instance-order","shared-first"]]) assert.throws(()=>parseDriver(extra));
// The actual existing packager must seal this exact public corpus and trial protocol.
const controlsRoot=fs.mkdtempSync(path.join(os.tmpdir(),"svg-count-controls-"));
try {
  fs.mkdirSync(path.join(controlsRoot,"Browser/dist"),{recursive:true});fs.mkdirSync(path.join(controlsRoot,"feed"));
  fs.writeFileSync(path.join(controlsRoot,"Browser/dist/suite.html"),"inert harness");fs.writeFileSync(path.join(controlsRoot,"source-manifest.tsv"),"inert source identity");
  const packaged=child.spawnSync(process.execPath,[new URL("./package-fixture.mjs",import.meta.url).pathname,"--work",controlsRoot],{encoding:"utf8"});
  assert.equal(packaged.status,0,packaged.stderr);
  const manifest=JSON.parse(fs.readFileSync(path.join(controlsRoot,"Browser/dist/artifact-manifest.json")));
  assert.deepEqual(manifest.workload.instanceResearch,corpus);
  assert.equal(manifest.workloadSha256,portableSha256(new TextEncoder().encode(JSON.stringify(suiteContract))));
  const altered=structuredClone(suiteContract);altered.instanceResearch.counts.pop();
  assert.notEqual(manifest.workloadSha256,portableSha256(new TextEncoder().encode(JSON.stringify(altered))));
} finally { fs.rmSync(controlsRoot,{recursive:true,force:true}); }
console.log("actual pre-launch option parser and controls packager bind count/order selection: passed (inert)");

// Exercise the real caller's lexical scope up to its existing DOM-order declaration.
// Only the downstream renderer boundary is inert; both research call expressions
// are copied verbatim from the maintained caller. This is no DOM/runtime acceptance.
const reconciliationSource=fs.readFileSync(new URL("./document-reconciliation-control.js",import.meta.url),"utf8");
function researchCallerBinding(source, selectedOrder) {
  const start=source.indexOf("export function documentReconciliationControl(");
  const end=source.indexOf("export function countInstanceResearchUpdate(",start);
  assert(start>=0 && end>start,"actual caller source boundary missing");
  const caller=source.slice(start,end).trim();
  const calls=caller.match(/    const instanceResearch = instanceResearchControl[^\n]+;\n    validateInstanceResearchResult[^\n]+;/)?.[0];
  assert(calls,"actual research calls missing");
  const cut="    const originalOrder = order();";
  assert.equal(caller.split(cut).length,2,"actual DOM-order binding boundary missing/ambiguous");
  const instrumented=caller.replace("export function","function").replace(cut,`${cut}\n    return (() => {\n${calls}\n      return instanceResearch;\n    })();`);
  const svg={namespaceURI:"http://www.w3.org/2000/svg",localName:"svg",attributes:[],children:[],textContent:"",querySelectorAll:()=>[]};
  const add=()=>{}, remove=()=>{}, raf=()=>{throw Error("unexpected frame request");}, cancel=()=>{};
  class InertEventTarget {}
  InertEventTarget.prototype.addEventListener=add;InertEventTarget.prototype.removeEventListener=remove;
  let mounts=0,disposals=0;
  const w={
    svgFoundation:{documentMount(){mounts++;},documentExport:()=>"<svg/>",documentDispose(){disposals++;}},
    document:{querySelector:()=>svg},EventTarget:InertEventTarget,
    DOMParser:class {parseFromString(){return {querySelector:()=>null,documentElement:svg};}},
    performance:{now:()=>0},requestAnimationFrame:raf,cancelAnimationFrame:cancel,
  };
  const execute=runInNewContext(`(${instrumented})`,{
    instanceResearchControl:(...args)=>rowsFor(args.at(-1)),validateInstanceResearchResult,
  });
  try { return execute(w,selectedOrder); }
  finally {
    assert.equal(mounts,1);assert.equal(disposals,1);
    assert.equal(InertEventTarget.prototype.addEventListener,add);assert.equal(InertEventTarget.prototype.removeEventListener,remove);
    assert.equal(w.requestAnimationFrame,raf);assert.equal(w.cancelAnimationFrame,cancel);
  }
}
for (const order of [undefined,"shared-first","expanded-first"]) {
  assert.deepEqual(researchCallerBinding(reconciliationSource,order),rowsFor(order ?? "shared-first"));
}
assert.throws(()=>researchCallerBinding(reconciliationSource,"unknown"),/Unknown instance research order/);
// Reintroduce the exact original caller shadowing, retaining the local order() reader.
const originalShadowing=reconciliationSource.replaceAll("representationOrder","order");
assert.notEqual(originalShadowing,reconciliationSource);
for (const order of [undefined,"shared-first","expanded-first"]) assert.throws(()=>researchCallerBinding(originalShadowing,order),/Unknown instance research order/);
console.log("actual lexical caller: default + both orders pass; original shadowing mutant refused for all three (inert)");

const maintainedRunner=fs.readFileSync(new URL("./run.sh",import.meta.url),"utf8");
assert(maintainedRunner.includes('node "$repo/tests/Scene.SvgBrowser.Tests/suite-control-test.mjs"'),"normal packed runner must execute the inexpensive caller regression");
