import { createHash } from "node:crypto";
import { createServer } from "node:http";
import { cpus, platform, release } from "node:os";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { gzipSync } from "node:zlib";
import { chromium, firefox, webkit } from "playwright-core";
import { PNG } from "pngjs";

const fixture = dirname(fileURLToPath(import.meta.url));
const dist = resolve(fixture, "dist");
const outputIndex = process.argv.indexOf("--out");
if (outputIndex < 0) throw new Error("--out is required");
const output = resolve(process.argv[outputIndex + 1]);
const sourceDigestIndex = process.argv.indexOf("--source-digest");
const sourceDigest = sourceDigestIndex < 0 ? "unavailable" : process.argv[sourceDigestIndex + 1];
const packageDigestIndex = process.argv.indexOf("--package-digest");
const packageDigest = packageDigestIndex < 0 ? "unavailable" : process.argv[packageDigestIndex + 1];
const browserIndex = process.argv.indexOf("--browser");
const browserFamily = browserIndex < 0 ? "chromium" : process.argv[browserIndex + 1];
const browserTypes = { chromium, firefox, webkit };
const browserType = browserTypes[browserFamily];
if (!browserType) throw new Error(`unsupported browser family: ${browserFamily}`);

function mime(path) {
  if (extname(path) === ".html") return "text/html; charset=utf-8";
  if (extname(path) === ".js") return "text/javascript; charset=utf-8";
  if (extname(path) === ".css") return "text/css; charset=utf-8";
  if (extname(path) === ".woff2") return "font/woff2";
  return "application/octet-stream";
}
function files(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = resolve(directory, name);
    return statSync(path).isDirectory() ? files(path) : [path];
  });
}
function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.floor(sorted.length / 2)];
}
const server = createServer((request, response) => {
  const target = request.url === "/" ? resolve(dist, "index.html") : resolve(dist, request.url.replace(/^\//, ""));
  if (!target.startsWith(`${dist}/`) || !existsSync(target) || statSync(target).isDirectory()) {
    response.writeHead(404).end(); return;
  }
  response.writeHead(200, { "content-type": mime(target), "cache-control": "no-store" });
  response.end(readFileSync(target));
});
await new Promise((done) => server.listen(0, "127.0.0.1", done));
const browser = await browserType.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 800, height: 600 }, deviceScaleFactor: 1 });
const page = await context.newPage();
const consoleErrors = [];
page.on("console", (message) => {
  const location = message.location();
  const expectedFontFailure = location.url.endsWith("/fonts/missing-noto.woff2") || message.text().includes("fonts/missing-noto.woff2");
  if (message.type() === "error" && !expectedFontFailure) consoleErrors.push(message.text());
});
page.on("pageerror", (error) => consoleErrors.push(error.stack || error.message));
await page.addInitScript(() => {
  window.__svgListenerBalance = 0;
  const add = EventTarget.prototype.addEventListener;
  const remove = EventTarget.prototype.removeEventListener;
  EventTarget.prototype.addEventListener = function(type, listener, options) {
    if (this instanceof SVGElement && this.hasAttribute("data-scene-root-id")) window.__svgListenerBalance += 1;
    return add.call(this, type, listener, options);
  };
  EventTarget.prototype.removeEventListener = function(type, listener, options) {
    if (this instanceof SVGElement && this.hasAttribute("data-scene-root-id")) window.__svgListenerBalance -= 1;
    return remove.call(this, type, listener, options);
  };
});
const address = server.address();
const mountSamples = [];
const inputPaintSamples = [];
let observations;
let documentEvidence;
let browserMatrixEvidence;
let sessionEvidence;
let externalSessionEvidence;
let animationEvidence;
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined, undefined, { timeout: 5000 }).catch((error) => {
    throw new Error(`fixture API unavailable: ${consoleErrors.join("\n") || error.message}`);
  });
  const mountedSession = await page.evaluate(() => window.svgFoundation.sessionMount());
  if (mountedSession.listeners !== 2 || mountedSession.frames !== 1 || mountedSession.requests !== 0 || mountedSession.status !== "Running") {
    throw new Error(`session mount ownership failed: ${JSON.stringify(mountedSession)}`);
  }
  await page.evaluate(() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(resolve)))));
  let cadence = await page.evaluate(() => window.svgFoundation.sessionObserve());
  if (cadence.status === "Recovering") {
    await page.evaluate(() => window.svgFoundation.sessionResume());
    await page.evaluate(() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(resolve)))));
    cadence = await page.evaluate(() => window.svgFoundation.sessionObserve());
  }
  if (!cadence.events.some((value) => value.startsWith("advance:")) || cadence.requests !== 1) {
    throw new Error(`session variable cadence did not advance/coalesce: ${JSON.stringify(cadence)}`);
  }
  const pausedSession = await page.evaluate(() => window.svgFoundation.sessionPause());
  if (pausedSession.status !== "Paused" || pausedSession.frames !== 0 || pausedSession.requests !== 0 || !pausedSession.events.includes("pause")) {
    throw new Error(`session pause did not release clock/request ownership: ${JSON.stringify(pausedSession)}`);
  }
  const steppedSession = await page.evaluate(() => { window.svgFoundation.sessionStep(); window.svgFoundation.sessionDemand(); return window.svgFoundation.sessionObserve(); });
  if (steppedSession.requests !== 1 || !steppedSession.events.includes("step")) {
    throw new Error(`paused step/projection coalescing failed: ${JSON.stringify(steppedSession)}`);
  }
  const generation = steppedSession.generation;
  const projectedSession = await page.evaluate(() => window.svgFoundation.sessionCompleteCurrent(7, "scene-7"));
  if (projectedSession.revision !== 7 || projectedSession.sceneRevision !== 7 || projectedSession.requests !== 1 || !projectedSession.events.includes("apply:7:scene-7")) {
    throw new Error(`monotonic retained projection failed: ${JSON.stringify(projectedSession)}`);
  }
  const replacedSession = await page.evaluate(() => window.svgFoundation.sessionReplace());
  const staleSession = await page.evaluate(() => window.svgFoundation.sessionCompletePrevious(99, "stale"));
  if (staleSession.generation === generation || staleSession.events.includes("apply:99:stale") || !replacedSession.events.some((value) => value.startsWith("replace:"))) {
    throw new Error(`replacement accepted a stale generation: ${JSON.stringify(staleSession)}`);
  }
  const resetSession = await page.evaluate(() => window.svgFoundation.sessionReset());
  if (!resetSession.events.includes("reset") || resetSession.requests !== 1) {
    throw new Error(`session reset did not request fresh retained state: ${JSON.stringify(resetSession)}`);
  }
  await page.evaluate(() => window.dispatchEvent(new Event("blur")));
  const suspendedSession = await page.evaluate(() => window.svgFoundation.sessionObserve());
  if (suspendedSession.status !== "Recovering" || suspendedSession.frames !== 0 || suspendedSession.requests !== 0 || !suspendedSession.events.some((value) => value.startsWith("recover:"))) {
    throw new Error(`blur/suspension recovery failed: ${JSON.stringify(suspendedSession)}`);
  }
  const disposedSession = await page.evaluate(() => window.svgFoundation.sessionDispose());
  if (!disposedSession.disposed || disposedSession.listeners !== 0 || disposedSession.frames !== 0 || disposedSession.requests !== 0 || !disposedSession.events.includes("dispose")) {
    throw new Error(`session disposal leaked resources: ${JSON.stringify(disposedSession)}`);
  }
  sessionEvidence = { mounted: mountedSession, cadence, paused: pausedSession, stepped: steppedSession, projected: projectedSession, replaced: replacedSession, stale: staleSession, reset: resetSession, suspended: suspendedSession, disposed: disposedSession };
  const externalMounted = await page.evaluate(() => window.svgFoundation.externalMount("opaque-a"));
  if (externalMounted.listeners !== 2 || externalMounted.requests !== 0 || externalMounted.status !== "Connected" || externalMounted.events.some((value) => value.startsWith("advance:") || value === "pause" || value === "reset")) {
    throw new Error(`external presentation acquired authority-clock behavior: ${JSON.stringify(externalMounted)}`);
  }
  const externalBurst = await page.evaluate(() => { window.svgFoundation.externalDemand(); window.svgFoundation.externalDemand(); window.svgFoundation.externalDemand(); const root = document.getElementById("fixture"); root.dispatchEvent(new KeyboardEvent("keydown", { key: "k", ctrlKey: true, bubbles: true, cancelable: true })); window.svgFoundation.externalReceipt(true, "external.select"); window.svgFoundation.externalReceipt(false, "external.select"); return window.svgFoundation.externalObserve(); });
  if (externalBurst.requests !== 1 || externalBurst.acquisition !== 0 || !externalBurst.queued || externalBurst.gateway.join("|") !== "command:external.select|receipt:true:external.select|receipt:false:external.select") {
    throw new Error(`external burst or ordered gateway composition was unbounded: ${JSON.stringify(externalBurst)}`);
  }
  const externalGeneration = externalBurst.generation;
  const externalApplied = await page.evaluate(({ generation }) => window.svgFoundation.externalComplete(generation, 0, "opaque-a", 11, "external-11"), { generation: externalGeneration });
  if (externalApplied.revision !== 11 || externalApplied.requests !== 1 || externalApplied.acquisition !== 1 || externalApplied.sceneRevision <= externalBurst.sceneRevision || !externalApplied.events.includes("apply:opaque-a:11:external-11")) {
    throw new Error(`external projection did not apply then drain the queued demand: ${JSON.stringify(externalApplied)}`);
  }
  const sameEpoch = await page.evaluate(() => window.svgFoundation.externalBind("opaque-a"));
  if (sameEpoch.revision !== 11 || sameEpoch.generation === externalGeneration || !sameEpoch.events.some((value) => value.endsWith(":opaque-a:true"))) {
    throw new Error(`same-epoch reconnect lost its revision baseline: ${JSON.stringify(sameEpoch)}`);
  }
  await page.evaluate(() => window.svgFoundation.externalDemand());
  const oldGeneration = sameEpoch.generation;
  const replacedEpoch = await page.evaluate(() => window.svgFoundation.externalBind("opaque-b"));
  const staleExternal = await page.evaluate(({ oldGeneration }) => window.svgFoundation.externalComplete(oldGeneration, 2, "opaque-a", 99, "stale"), { oldGeneration });
  if (replacedEpoch.revision !== null || staleExternal.events.includes("apply:opaque-a:99:stale") || staleExternal.sceneRevision === 99) {
    throw new Error(`epoch replacement accepted stale presentation: ${JSON.stringify(staleExternal)}`);
  }
  const lowerEpochRevision = await page.evaluate(() => { window.svgFoundation.externalDemand(); const value = window.svgFoundation.externalObserve(); return window.svgFoundation.externalComplete(value.generation, value.acquisition, "opaque-b", 1, "external-b-1"); });
  if (lowerEpochRevision.revision !== 1 || lowerEpochRevision.sceneRevision <= staleExternal.sceneRevision || !lowerEpochRevision.events.includes("apply:opaque-b:1:external-b-1")) {
    throw new Error(`new epoch lower authority revision did not update retained DOM through its local presentation revision: ${JSON.stringify(lowerEpochRevision)}`);
  }
  await page.evaluate(() => { window.svgFoundation.externalDemand(); window.dispatchEvent(new Event("blur")); });
  const blurredExternal = await page.evaluate(() => window.svgFoundation.externalObserve());
  if (blurredExternal.requests !== 0 || blurredExternal.events.some((value) => value === "pause" || value.startsWith("advance:"))) {
    throw new Error(`external blur affected engine authority or retained acquisition: ${JSON.stringify(blurredExternal)}`);
  }
  const disposedExternal = await page.evaluate(() => window.svgFoundation.externalDispose());
  await page.evaluate(() => window.svgFoundation.externalDemand());
  const lateExternal = await page.evaluate(() => window.svgFoundation.externalObserve());
  if (!disposedExternal.disposed || disposedExternal.listeners !== 0 || disposedExternal.requests !== 0 || lateExternal.events.length !== disposedExternal.events.length) {
    throw new Error(`external disposal leaked or accepted late scheduling: ${JSON.stringify({ disposedExternal, lateExternal })}`);
  }
  const cancelThrows = await page.evaluate(() => { window.svgFoundation.externalMountControl("control", "cancel-throws"); window.svgFoundation.externalDemand(); return window.svgFoundation.externalDispose(); });
  if (!cancelThrows.disposed || cancelThrows.listeners !== 0 || cancelThrows.requests !== 0 || !cancelThrows.callbackFailure || !cancelThrows.cancellationUnknown) {
    throw new Error(`throwing cancellation hid resource settlement: ${JSON.stringify(cancelThrows)}`);
  }
  const disposeThrows = await page.evaluate(() => { window.svgFoundation.externalMountControl("control", "dispose-throws"); return window.svgFoundation.externalDispose(); });
  if (!disposeThrows.disposed || disposeThrows.listeners !== 0 || !disposeThrows.callbackFailure || disposeThrows.cancellationUnknown) {
    throw new Error(`throwing dispose callback skipped listener cleanup: ${JSON.stringify(disposeThrows)}`);
  }
  const applyDisposes = await page.evaluate(() => { window.svgFoundation.externalMountControl("control", "apply-dispose"); window.svgFoundation.externalDemand(); window.svgFoundation.externalDemand(); return window.svgFoundation.externalComplete(1, 0, "control", 1, "dispose"); });
  if (!applyDisposes.disposed || applyDisposes.listeners !== 0 || applyDisposes.requests !== 0 || applyDisposes.events.filter((value) => value.startsWith("request:")).length !== 1) {
    throw new Error(`reentrant apply disposal scheduled retired work: ${JSON.stringify(applyDisposes)}`);
  }
  const applyRebinds = await page.evaluate(() => { window.svgFoundation.externalMountControl("control", "apply-rebind"); window.svgFoundation.externalDemand(); window.svgFoundation.externalDemand(); return window.svgFoundation.externalComplete(1, 0, "control", 1, "rebind"); });
  if (applyRebinds.epoch !== "reentrant" || applyRebinds.requests !== 0 || applyRebinds.events.filter((value) => value.startsWith("request:")).length !== 1) {
    throw new Error(`reentrant apply replacement scheduled superseded work: ${JSON.stringify(applyRebinds)}`);
  }
  const requestCompletes = await page.evaluate(() => { window.svgFoundation.externalMountControl("control", "request-sync-complete"); return window.svgFoundation.externalDemand(); });
  if (requestCompletes.revision !== 1 || requestCompletes.requests !== 0 || !requestCompletes.events.includes("apply:control:1:sync")) {
    throw new Error(`synchronous request completion was not serialized: ${JSON.stringify(requestCompletes)}`);
  }
  const repeatedDispose = await page.evaluate(() => { const first = window.svgFoundation.externalDispose(); const second = window.svgFoundation.externalDispose(); return { first, second }; });
  if (repeatedDispose.first.listeners !== 0 || repeatedDispose.second.listeners !== 0 || repeatedDispose.second.events.length !== repeatedDispose.first.events.length) {
    throw new Error(`repeated disposal changed terminal ownership: ${JSON.stringify(repeatedDispose)}`);
  }
  externalSessionEvidence = { mounted: externalMounted, burst: externalBurst, applied: externalApplied, sameEpoch, replacedEpoch, stale: staleExternal, lowerEpochRevision, blurred: blurredExternal, disposed: disposedExternal, late: lateExternal, cancelThrows, disposeThrows, applyDisposes, applyRebinds, requestCompletes, repeatedDispose };
  const mountedAnimation = await page.evaluate(() => { const value = window.svgFoundation.animationMount(); return window.svgFoundation.animationMotion(false); });
  if (mountedAnimation.listeners !== 2 || mountedAnimation.frames !== 0 || mountedAnimation.active !== 0 || mountedAnimation.status !== "Running") {
    throw new Error(`animation mount ownership failed: ${JSON.stringify(mountedAnimation)}`);
  }
  const startedAnimation = await page.evaluate(() => window.svgFoundation.animationStartEssential("move", 7));
  if (startedAnimation.active !== 1 || startedAnimation.frames !== 1 || startedAnimation.semanticId !== "alpha") {
    throw new Error(`animation start did not preserve the retained semantic target: ${JSON.stringify(startedAnimation)}`);
  }
  await page.evaluate(() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  const pausedAnimation = await page.evaluate(() => window.svgFoundation.animationPause());
  if (pausedAnimation.status !== "Paused" || pausedAnimation.frames !== 0 || pausedAnimation.active !== 1) {
    throw new Error(`animation pause retained the browser clock: ${JSON.stringify(pausedAnimation)}`);
  }
  const cueCountBeforeSeek = pausedAnimation.events.filter((value) => value.startsWith("cue:")).length;
  const soughtAnimation = await page.evaluate(() => window.svgFoundation.animationSeek("move", 80));
  if (soughtAnimation.frames !== 0 || soughtAnimation.events.filter((value) => value.startsWith("cue:")).length !== cueCountBeforeSeek) {
    throw new Error(`animation seek replayed a historical cue: ${JSON.stringify(soughtAnimation)}`);
  }
  await page.evaluate(() => { window.svgFoundation.animationResume(); return new Promise((resolve) => setTimeout(resolve, 260)); });
  const completedAnimation = await page.evaluate(() => window.svgFoundation.animationObserve());
  const midpointCues = completedAnimation.events.filter((value) => value.includes(":midpoint:"));
  if (completedAnimation.active !== 0 || completedAnimation.frames !== 0 || midpointCues.length !== 1 || completedAnimation.presentationRevision <= soughtAnimation.presentationRevision) {
    throw new Error(`animation completion/cue delivery failed: ${JSON.stringify(completedAnimation)}`);
  }
  const reducedAnimation = await page.evaluate(() => { window.svgFoundation.animationMotion(true); return window.svgFoundation.animationStartDecorative("reduced"); });
  if (reducedAnimation.active !== 0 || reducedAnimation.frames !== 0 || reducedAnimation.transform !== "translate(40 0)" || reducedAnimation.opacity !== "1" || reducedAnimation.semanticId !== "alpha") {
    throw new Error(`reduced motion did not settle decoration while preserving semantics: ${JSON.stringify(reducedAnimation)}`);
  }
  const boundedAnimation = await page.evaluate(() => {
    window.svgFoundation.animationMotion(false);
    window.svgFoundation.animationStartDecorative("one");
    return window.svgFoundation.animationStartDecorative("two");
  });
  if (boundedAnimation.active !== 1 || !boundedAnimation.events.some((value) => value.includes("DecorativeLimitReached"))) {
    throw new Error(`decorative effect pressure was not bounded: ${JSON.stringify(boundedAnimation)}`);
  }
  const replacedAnimation = await page.evaluate(() => window.svgFoundation.animationReplace(8));
  const staleAnimation = await page.evaluate(() => window.svgFoundation.animationStartEssential("stale", 7));
  if (replacedAnimation.active !== 0 || replacedAnimation.frames !== 0 || staleAnimation.active !== 0 || !staleAnimation.events.some((value) => value.includes("StaleAuthorityRevision"))) {
    throw new Error(`authority replacement accepted stale animation: ${JSON.stringify(staleAnimation)}`);
  }
  const disposedAnimation = await page.evaluate(() => window.svgFoundation.animationDispose());
  if (!disposedAnimation.disposed || disposedAnimation.listeners !== 0 || disposedAnimation.frames !== 0 || disposedAnimation.active !== 0 || !disposedAnimation.events.includes("dispose")) {
    throw new Error(`animation disposal leaked resources: ${JSON.stringify(disposedAnimation)}`);
  }
  animationEvidence = { mounted: mountedAnimation, started: startedAnimation, paused: pausedAnimation, sought: soughtAnimation, completed: completedAnimation, reduced: reducedAnimation, bounded: boundedAnimation, replaced: replacedAnimation, stale: staleAnimation, disposed: disposedAnimation };
  await page.evaluate(() => window.svgFoundation.mount());
  const documentContract = await page.evaluate(() => {
    const root = document.querySelector("[data-fsgg-document-id='portable-document']");
    root.setAttribute("width", "240");
    root.setAttribute("height", "160");
    const references = [...root.querySelectorAll("[href], [fill^='url(#'], [stroke^='url(#'], [clip-path^='url(#'], [mask^='url(#']")]
      .flatMap((node) => [...node.attributes].map((attribute) => attribute.value))
      .flatMap((value) => value.startsWith("#") ? [value.slice(1)] : [...value.matchAll(/url\(#([^\)]+)\)/g)].map((match) => match[1]));
    const ids = [...root.querySelectorAll("[id]")].map((node) => node.id);
    const strokeOnly = root.querySelector("[data-fsgg-node='gallery-0']");
    const evenodd = root.querySelector("[data-fsgg-node='gallery-1']");
    const completeArc = root.querySelector("[data-fsgg-node='gallery-2']");
    const explicitGradient = root.querySelector("linearGradient[spreadMethod='reflect']");
    const nestedClip = [...root.querySelectorAll("g[clip-path]")].find((clip) => clip.querySelector(":scope > g[clip-path]") !== null);
    const symbolUse = root.querySelector("use");
    const symbolHitProxy = root.querySelector("[data-fsgg-symbol-hit]");
    const textNodes = [...root.querySelectorAll("text")];
    const exported = window.svgFoundation.documentExport();
    const beforeInvalid = { root, exported };
    const invalid = window.svgFoundation.replaceInvalidDocument();
    const duplicate = window.svgFoundation.duplicateDocumentMount();
    const fonts = window.svgFoundation.documentFonts();
    const invalidPreservedRoot = beforeInvalid.root === document.querySelector("[data-fsgg-document-id='portable-document']");
    const invalidPreservedExport = beforeInvalid.exported === window.svgFoundation.documentExport();
    return {
      definitionKinds: {
        gradients: root.querySelectorAll("linearGradient, radialGradient").length,
        symbols: root.querySelectorAll("symbol").length,
        clips: root.querySelectorAll("clipPath").length,
        alphaMasks: root.querySelectorAll("mask[style*='alpha']").length,
        luminanceMasks: root.querySelectorAll("mask[style*='luminance']").length,
        uses: root.querySelectorAll("use").length,
      },
      idsUnique: new Set(ids).size === ids.length,
      referencesLocalAndResolved: references.length > 0 && references.every((id) => root.querySelector(`[id='${CSS.escape(id)}']`)),
      strokeFill: strokeOnly?.getAttribute("fill"),
      strokeColor: strokeOnly?.getAttribute("stroke"),
      evenodd: evenodd?.getAttribute("fill-rule"),
      arcSegments: (completeArc?.getAttribute("d").match(/\bA\b/g) || []).length,
      selectedDefinitionDetails: {
        explicitGradientStops: explicitGradient?.querySelectorAll("stop").length,
        explicitGradientOffsets: [...(explicitGradient?.querySelectorAll("stop") || [])].map((stop) => stop.getAttribute("offset")),
        explicitGradientTransform: explicitGradient?.getAttribute("gradientTransform"),
        explicitGradientInterpolation: explicitGradient?.getAttribute("color-interpolation"),
        hasUserSpaceGradient: root.querySelector("[gradientUnits='userSpaceOnUse']") !== null,
        hasObjectBoundingBoxGradient: root.querySelector("[gradientUnits='objectBoundingBox']") !== null,
        nestedClip: nestedClip !== undefined,
        maskUnits: [...root.querySelectorAll("mask")].map((mask) => mask.getAttribute("maskUnits")).sort(),
      symbolViewport: symbolUse ? ["x", "y", "width", "height"].map((name) => symbolUse.getAttribute(name)) : [],
      symbolPointerEvents: symbolUse?.getAttribute("pointer-events"),
      symbolHitProxy: symbolHitProxy ? {
        id: symbolHitProxy.getAttribute("data-fsgg-symbol-hit"),
        fill: symbolHitProxy.getAttribute("fill"),
        stroke: symbolHitProxy.getAttribute("stroke"),
        pointerEvents: symbolHitProxy.getAttribute("pointer-events"),
        ariaHidden: symbolHitProxy.getAttribute("aria-hidden"),
      } : null,
        textLayoutExplicit: textNodes.length > 0 && textNodes.every((text) => text.getAttribute("text-anchor") === "start" && text.getAttribute("direction") === "auto"),
      },
      invalid, duplicate, fonts, exported,
      invalidPreservedRoot,
      invalidPreservedExport,
    };
  });
  if (documentContract.definitionKinds.gradients < 3 || documentContract.definitionKinds.symbols !== 1 || documentContract.definitionKinds.clips < 2 || documentContract.definitionKinds.alphaMasks !== 1 || documentContract.definitionKinds.luminanceMasks !== 1 || documentContract.definitionKinds.uses !== 1) {
    throw new Error(`definition mapping incomplete: ${JSON.stringify(documentContract.definitionKinds)}`);
  }
  if (!documentContract.idsUnique || !documentContract.referencesLocalAndResolved || documentContract.strokeFill !== "none" || documentContract.strokeColor !== "rgb(240 120 20)" || documentContract.evenodd !== "evenodd" || documentContract.arcSegments < 2) {
    throw new Error(`document DOM/geometry contract failed: ${JSON.stringify(documentContract)}`);
  }
  const details = documentContract.selectedDefinitionDetails;
  if (details.explicitGradientStops !== 3 || details.explicitGradientOffsets.join(",") !== "0,0.4,1" || !details.explicitGradientTransform?.startsWith("matrix(") || details.explicitGradientInterpolation !== "sRGB" || !details.hasUserSpaceGradient || !details.hasObjectBoundingBoxGradient || !details.nestedClip || details.maskUnits.join(",") !== "objectBoundingBox,userSpaceOnUse" || details.symbolViewport.join(",") !== "0,0,20,20" || details.symbolPointerEvents !== "all" || details.symbolHitProxy?.id !== "symbol-instance" || details.symbolHitProxy?.fill !== "transparent" || details.symbolHitProxy?.stroke !== "none" || details.symbolHitProxy?.pointerEvents !== "all" || details.symbolHitProxy?.ariaHidden !== "true" || !details.textLayoutExplicit) {
    throw new Error(`selected definition details incomplete: ${JSON.stringify(details)}`);
  }
  if (!documentContract.invalid.includes("duplicate-id:/children") || documentContract.duplicate !== "duplicate-mount-namespace:gallery-browser" || !documentContract.invalidPreservedRoot || !documentContract.invalidPreservedExport) {
    throw new Error(`pre-mutation refusal contract failed: ${JSON.stringify(documentContract)}`);
  }
  if (documentContract.fonts.length !== 1 || documentContract.fonts[0].ready || documentContract.fonts[0].diagnostic !== "font-unavailable:font:Noto Sans") {
    throw new Error(`explicit font failure missing: ${JSON.stringify(documentContract.fonts)}`);
  }
  const documentRoot = page.locator("[data-fsgg-document-id='portable-document']");
  const originalPng = PNG.sync.read(await documentRoot.screenshot());
  const pixel = (png, x, y) => {
    const index = (y * png.width + x) * 4;
    return [...png.data.subarray(index, index + 4)];
  };
  const visualReferences = {
    chromium: {
      tolerance: 28,
      evenoddHole: [255, 255, 255, 255],
      gradientLeft: [173, 51, 133, 255],
      gradientRight: [71, 51, 235, 255],
    },
    firefox: {
      tolerance: 28,
      evenoddHole: [255, 255, 255, 255],
      gradientLeft: [173, 51, 133, 255],
      gradientRight: [71, 51, 235, 255],
    },
    webkit: {
      tolerance: 28,
      evenoddHole: [255, 255, 255, 255],
      gradientLeft: [173, 51, 133, 255],
      gradientRight: [71, 51, 235, 255],
    },
  };
  const reference = visualReferences[browserFamily];
  const samples = {
    evenoddHole: pixel(originalPng, 40, 40),
    gradientLeft: pixel(originalPng, 16, 20),
    gradientRight: pixel(originalPng, 36, 20),
  };
  const assertColor = (name, actual, expected, tolerance) => {
    const distance = Math.max(...actual.map((value, index) => Math.abs(value - expected[index])));
    if (distance > tolerance) throw new Error(`visual reference ${name} exceeded tolerance ${tolerance}: actual=${actual} expected=${expected} distance=${distance}`);
  };
  Object.entries(samples).forEach(([name, actual]) => assertColor(name, actual, reference[name], reference.tolerance));

  const reload = await context.newPage();
  await reload.setContent(`<body style="margin:0;background:white">${documentContract.exported}</body>`, { waitUntil: "load" });
  const reloadedRoot = reload.locator("[data-fsgg-document-id='portable-document']");
  await reloadedRoot.evaluate((root) => { root.setAttribute("width", "240"); root.setAttribute("height", "160"); });
  const reloadRefs = await reloadedRoot.evaluate((root) => [...root.querySelectorAll("use")].every((node) => node.getAttribute("href")?.startsWith("#") && root.querySelector(`[id='${CSS.escape(node.getAttribute("href").slice(1))}']`)));
  if (!reloadRefs) throw new Error("isolated exported SVG lost local symbol references");
  const reloadPng = PNG.sync.read(await reloadedRoot.screenshot());
  Object.entries(samples).forEach(([name, expected]) => assertColor(`reload-${name}`, pixel(reloadPng, name === "evenoddHole" ? 40 : name === "gradientLeft" ? 16 : 36, name === "evenoddHole" ? 40 : 20), expected, 2));
  await reload.close();
  const hitSemantics = await page.evaluate(() => {
    const root = document.querySelector("[data-fsgg-document-id='portable-document']");
    const bounds = root.getBoundingClientRect();
    const domAt = (x, y) => {
      const target = document.elementFromPoint(bounds.left + x * bounds.width / 120, bounds.top + y * bounds.height / 80);
      return {
        node: target?.getAttribute("data-fsgg-node") ?? null,
        semantic: target?.closest("[data-fsgg-semantic-id]")?.getAttribute("data-fsgg-semantic-id") ?? null,
      };
    };
    return {
      path: { api: window.svgFoundation.documentHit(10, 10), dom: domAt(10, 10) },
      text: { api: window.svgFoundation.documentHit(8, 52), dom: domAt(8, 52) },
      symbol: { api: window.svgFoundation.documentHit(80, 55), dom: domAt(80, 55) },
      clippedSymbol: { api: window.svgFoundation.documentHit(71, 46), dom: domAt(71, 46) },
    };
  });
  if (hitSemantics.path.api !== "semantic:gallery" || hitSemantics.path.dom.node !== "gallery-1" || hitSemantics.text.api !== "semantic:gallery" || hitSemantics.text.dom.node !== "gallery-4" || hitSemantics.symbol.api !== "semantic:symbol-instance" || hitSemantics.symbol.dom.semantic !== "semantic:symbol-instance" || hitSemantics.clippedSymbol.api !== null) {
    throw new Error(`browser paint/path/text/symbol/clip hit contract failed: ${JSON.stringify(hitSemantics)}`);
  }
  const retainedDocument = await page.evaluate(() => {
    const root = document.querySelector("[data-fsgg-document-id='portable-document']");
    const stableElements = Object.fromEntries([...root.querySelectorAll("[data-fsgg-element-id]")].map((node) => [node.getAttribute("data-fsgg-element-id"), node]));
    const stableDefinitions = Object.fromEntries([...root.querySelectorAll("defs [id]")].map((node) => [node.id, node]));
    const reorderedResult = window.svgFoundation.replaceReorderedDocument();
    const reorderedIds = [...root.querySelectorAll(":scope > g[data-fsgg-element-id]")].map((node) => node.getAttribute("data-fsgg-element-id"));
    const reorderedRetained = Object.entries(stableElements).every(([id, node]) => root.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) === node)
      && Object.entries(stableDefinitions).every(([id, node]) => root.querySelector(`[id='${CSS.escape(id)}']`) === node);
    const changedResult = window.svgFoundation.replaceChangedDocument();
    const changedRetained = Object.entries(stableElements).every(([id, node]) => root.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) === node);
    const restoredResult = window.svgFoundation.replaceOriginalDocument();
    return { reorderedResult, changedResult, restoredResult, reorderedIds, reorderedRetained, changedRetained };
  });
  if (retainedDocument.reorderedResult !== null || retainedDocument.changedResult !== null || retainedDocument.restoredResult !== null || !retainedDocument.reorderedRetained || !retainedDocument.changedRetained || retainedDocument.reorderedIds.join(",") !== "luminance-element,symbol-instance,gallery") {
    throw new Error(`identified document reconciliation replaced stable nodes: ${JSON.stringify(retainedDocument)}`);
  }
  documentEvidence = {
    definitions: documentContract.definitionKinds,
    stableLocalReferences: documentContract.referencesLocalAndResolved,
    duplicateIdsRejectedBeforeReplacement: documentContract.invalid.includes("duplicate-id:/children"),
    duplicateMountNamespaceRejected: documentContract.duplicate === "duplicate-mount-namespace:gallery-browser",
    explicitFontFailure: documentContract.fonts[0].diagnostic,
    completeArcSegments: documentContract.arcSegments,
    selectedDefinitionDetails: details,
    visualReference: { browserFamily, tolerancePerChannel: reference.tolerance, samples },
    isolatedExportReload: true,
    hitSemantics,
    maskedTransparencyPolicy: "browser-dom-painted-geometry-remains-targetable;clip-removes-target",
    retainedReplacement: retainedDocument,
    exportedSvg: documentContract.exported,
  };
  // Full-byte digests frozen by actual pre-change Fable execution, before prefix factoring.
  const prefixExpected = [
  {
    "name": "gallery",
    "serializedSha256": "91948c47a8245b063c8a32c5861ece90c71446d8a41bbf63f98bd748b21dcdca",
    "svgSha256": "6a4591362d98ea820035c6e004e258482f90af5fe8061b595a8e3876490fae38"
  },
  {
    "name": "ascii-id",
    "serializedSha256": "eda3bff9f260d3396de1a5baa4493ed372393ee922937cd92c612386748422a7",
    "svgSha256": "d930505e5ba566bafffdab4a64446d005cb0541f55fd059505613d8d9e145a23"
  },
  {
    "name": "escaped-punctuation",
    "serializedSha256": "9cb301ec1730ce8ef92308fdf74c32969b659f98ef04875bcf633695c6116951",
    "svgSha256": "5ad13ffcbea0e7a08786d3279a7486a57bf0ed252bdb7aafff415134c31a50c4"
  },
  {
    "name": "unicode-id",
    "serializedSha256": "8a54a31ad81dcfbcfbe272d99eec6c9a5231bcf4f0574e51276d8b13283aeb1c",
    "svgSha256": "0d76f8155764f52f503084966a826680763e83bf46d5c520d799b71441dad316"
  },
  {
    "name": "namespace-a",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
  },
  {
    "name": "namespace-b",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "2aaa6b2549aca1df34d6803c84d085d370b2cb25f4b6510892c188628f54c007"
  },
  {
    "name": "document-a",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "f4596bbbea6dea2f8119bdb27ded54061815bd02c0a0f4e2e69673930c077316"
  },
  {
    "name": "document-b",
    "serializedSha256": "fd000a0b56e81317b70a7cf0ee69314286dfc139ff227357d84621bae9d3d755",
    "svgSha256": "1a536cc4b6a296a935903fe24f0bbeeba171712eea717357043f5f3112dcba70"
  },
  {
    "name": "definition-replaced",
    "serializedSha256": "e9dd57d245ae1dbef4ce2985396347caf1534f22785c01c5838ff1d9c4fabce8",
    "svgSha256": "c1015202ee14ec42ab139574dedc998d6843abb65d1e93be0aacea94aaf096e8"
  },
  {
    "name": "reference-changed",
    "serializedSha256": "21352ccadc2e1aeff6b6c42f232240813f6964f423f22d6b8ccb44f34a898a15",
    "svgSha256": "9f26771da92ac20910ceaa997c0fadfe070bff1e12fe3dce51d638518e82de32"
  },
  {
    "name": "reference-removed",
    "serializedSha256": "aed6e9dc17e20b85b66bdf9d57bba66963bdb25cb1269c36515d2bba30d1dd96",
    "svgSha256": "c90d6a62ffe352ef6de3f99b521bb93134bc4493a641a95c9316282af2c28983"
  },
  {
    "name": "interleave-a-first",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
  },
  {
    "name": "interleave-b",
    "serializedSha256": "fd000a0b56e81317b70a7cf0ee69314286dfc139ff227357d84621bae9d3d755",
    "svgSha256": "c7ba03084d0481d6c4b5deaeddb6757cb51a59c47c33145c1b8ebdacbf842697"
  },
  {
    "name": "interleave-a-again",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
  },
  {
    "name": "gallery-second-document",
    "serializedSha256": "6e64ad706729cc9267521117178cf0d537408eee4f1b555a5792a895ca32790c",
    "svgSha256": "e77d863b711994e34b27de2a117aa677be7ea1fa24557dff91b9dfbe0cf68619"
  },
  {
    "name": "invalid-blank-namespace",
    "serializedSha256": "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4",
    "svgSha256": "c0259da567fde755eb0feea735f352a2be968958ccf459046edc0a05407196a5"
  },
  {
    "name": "invalid-blank-document",
    "serializedSha256": "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b",
    "svgSha256": "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b"
  },
  {
    "name": "invalid-both-blank",
    "serializedSha256": "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b",
    "svgSha256": "2e310bd2f1278c399fedcbc7564add9289fe61ee85cc0023bb2346d475aed04d"
  },
  {
    "name": "invalid-duplicate",
    "serializedSha256": "2a81c7c1c473839bc711ba3210901380e276badd021262e6012cd3d0e04772c3",
    "svgSha256": "2a81c7c1c473839bc711ba3210901380e276badd021262e6012cd3d0e04772c3"
  },
  {
    "name": "invalid-missing-reference",
    "serializedSha256": "99a43d5380e50d7fef98c891f4128977db792cb6552ec4b2947812f068fb6e69",
    "svgSha256": "99a43d5380e50d7fef98c891f4128977db792cb6552ec4b2947812f068fb6e69"
  },
  {
    "name": "invalid-wrong-reference-kind",
    "serializedSha256": "65b240eefeabd3b0e103c755697d18f54e921f5f4eee4aa875597b8404f812a6",
    "svgSha256": "65b240eefeabd3b0e103c755697d18f54e921f5f4eee4aa875597b8404f812a6"
  },
  {
    "name": "invalid-cycle",
    "serializedSha256": "a997cb82763cea0f440259295f9a357ec24ed70d80c65b8407cd08a7ce27cf31",
    "svgSha256": "a997cb82763cea0f440259295f9a357ec24ed70d80c65b8407cd08a7ce27cf31"
  }
];
  const prefixObservations = await page.evaluate(() => window.svgFoundation.prefixCompatibility());
  const prefixDigest = (value) => createHash("sha256").update(value).digest("hex");
  function checkPrefixBytes(name, value) {
    const expected = prefixExpected.find((row) => row.name === name);
    if (!expected || prefixDigest(value) !== expected.svgSha256) throw new Error(`prefix-svg-byte-drift:${name}`);
  }
  if (prefixObservations.length !== 22 || new Set(prefixObservations.map((row) => row.name)).size !== 22) throw new Error("prefix-corpus-population");
  for (const row of prefixObservations) {
    const expected = prefixExpected.find((value) => value.name === row.name);
    if (!expected || prefixDigest(row.serialized) !== expected.serializedSha256) throw new Error(`prefix-serialized-byte-drift:${row.name}`);
    checkPrefixBytes(row.name, row.svg);
  }
  let stalePrefixRejected = false;
  try { checkPrefixBytes("namespace-b", prefixObservations.find((row) => row.name === "namespace-a").svg); }
  catch (error) { if (error.message !== "prefix-svg-byte-drift:namespace-b") throw error; stalePrefixRejected = true; }
  if (!stalePrefixRejected) throw new Error("prefix-stale-namespace-control");
  const prefixLive = await page.evaluate(() => {
    const api = window.svgFoundation;
    const container = document.querySelector("#document-fixture");
    function root() { return container.querySelector("svg"); }
    function references() {
      const value = root();
      const ids = [...value.querySelectorAll("[id]")].map((node) => node.id);
      if (new Set(ids).size !== ids.length) throw new Error("prefix-duplicate-dom-id");
      for (const use of value.querySelectorAll("use")) {
        const href = use.getAttribute("href") || use.getAttributeNS("http://www.w3.org/1999/xlink", "href");
        if (!href || !href.startsWith("#") || !value.querySelector(`[id='${CSS.escape(href.slice(1))}']`)) throw new Error("prefix-local-reference");
      }
    }
    const exports = [];
    api.prefixMount("document-a");
    const retained = root();
    const first = api.prefixState();
    const html = retained.outerHTML;
    for (const name of ["invalid-blank-document", "invalid-duplicate", "invalid-missing-reference", "invalid-wrong-reference-kind", "invalid-cycle"]) {
      const refusal = api.prefixReplace(name);
      const after = api.prefixState();
      if (!refusal || root() !== retained || after.document !== first.document || after.exportedSvg !== first.exportedSvg || retained.outerHTML !== html) throw new Error(`prefix-invalid-not-atomic:${name}`);
    }
    const originalSet = Element.prototype.setAttribute;
    const originalSetNS = Element.prototype.setAttributeNS;
    const originalAppend = Node.prototype.appendChild;
    const originalInsert = Node.prototype.insertBefore;
    let unchangedWrites = 0;
    let ownedMoves = 0;
    Element.prototype.setAttribute = function (name, value) {
      if (retained.contains(this) && this.getAttribute(name) === String(value)) unchangedWrites++;
      return originalSet.call(this, name, value);
    };
    Element.prototype.setAttributeNS = function (namespace, name, value) {
      if (retained.contains(this) && this.getAttributeNS(namespace, name.split(":").at(-1)) === String(value)) unchangedWrites++;
      return originalSetNS.call(this, namespace, name, value);
    };
    Node.prototype.appendChild = function (child) {
      if (retained.contains(this) && child.parentNode === this) ownedMoves++;
      return originalAppend.call(this, child);
    };
    Node.prototype.insertBefore = function (child, before) {
      if (retained.contains(this) && child.parentNode === this) ownedMoves++;
      return originalInsert.call(this, child, before);
    };
    try {
      if (api.prefixReplace("document-a") !== null) throw new Error("prefix-unchanged-replace");
    } finally {
      Element.prototype.setAttribute = originalSet;
      Element.prototype.setAttributeNS = originalSetNS;
      Node.prototype.appendChild = originalAppend;
      Node.prototype.insertBefore = originalInsert;
    }
    if (unchangedWrites !== 0 || ownedMoves !== 0) throw new Error(`prefix-unchanged-work:${unchangedWrites}/${ownedMoves}`);
    for (const name of ["document-a", "definition-replaced", "reference-changed", "document-b", "reference-removed", "document-a"]) {
      if (api.prefixReplace(name) !== null) throw new Error(`prefix-replace:${name}`);
      if (root() !== retained) throw new Error(`prefix-root-replaced:${name}`);
      references();
      exports.push({ name, svg: "ok:" + api.prefixState().exportedSvg });
    }
    const use = root().querySelector("use");
    const old = use.getAttribute("href");
    use.setAttribute("href", "#missing-prefix-control");
    let wrongReferenceRejected = false;
    try { references(); } catch (error) { if (error.message !== "prefix-local-reference") throw error; wrongReferenceRejected = true; }
    finally { use.setAttribute("href", old); }
    references();
    for (const name of ["namespace-a", "namespace-b", "unicode-id", "escaped-punctuation", "namespace-a"]) {
      api.prefixMount(name);
      if (container.querySelectorAll("svg").length !== 1) throw new Error("prefix-owned-root-count");
      references();
      exports.push({ name, svg: "ok:" + api.prefixState().exportedSvg });
    }
    if (api.documentDispose() !== 0 || container.querySelector("svg")) throw new Error("prefix-disposal");
    api.documentMount();
    return { exports, wrongReferenceRejected, invalidAtomicity: true, ownedMountCycles: 5, unchangedWrites, ownedMoves };
  });
  if (!prefixLive.wrongReferenceRejected) throw new Error("prefix-reference-negative-control");
  for (const row of prefixLive.exports) checkPrefixBytes(row.name, row.svg);
  documentEvidence.prefixCompatibility = { corpus: 22, prechangeRuntime: "Fable 5.17.0", fullByteDigests: true, stalePrefixRejected, ...prefixLive };
  const root = page.locator("[data-scene-root-id='svg-foundation-root']");
  const box = await root.boundingBox();
  if (!box) throw new Error("SVG root has no rendered bounds");

  await page.evaluate(() => {
    window.__stableBeforeSelection = {
      root: document.querySelector("[data-scene-root-id]"),
      viewport: document.querySelector("[data-scene-viewport]"),
      units: document.querySelector("[data-scene-layer-id='units']"),
      alpha: document.querySelector("[data-scene-object-id='alpha']"),
      beta: document.querySelector("[data-scene-object-id='beta']"),
      alphaChild: document.querySelector("[data-scene-object-id='alpha'] > *"),
      betaChild: document.querySelector("[data-scene-object-id='beta'] > *"),
    };
  });
  await page.mouse.click(box.x + 60, box.y + 50);
  let state = await page.evaluate(() => window.svgFoundation.state());
  if (state.selected !== "alpha" || state.focused !== "alpha") throw new Error(`inverse pointer pick failed: ${JSON.stringify(state)}`);
  const selectionRetained = await page.evaluate(() => Object.entries(window.__stableBeforeSelection).every(([key, node]) => node === ({
    root: document.querySelector("[data-scene-root-id]"),
    viewport: document.querySelector("[data-scene-viewport]"),
    units: document.querySelector("[data-scene-layer-id='units']"),
    alpha: document.querySelector("[data-scene-object-id='alpha']"),
    beta: document.querySelector("[data-scene-object-id='beta']"),
    alphaChild: document.querySelector("[data-scene-object-id='alpha'] > *"),
    betaChild: document.querySelector("[data-scene-object-id='beta'] > *"),
  })[key]));
  if (!selectionRetained) throw new Error("selection rebuilt retained scene children");

  await page.locator("[data-scene-control-id='beta']").click();
  state = await page.evaluate(() => window.svgFoundation.state());
  if (state.selected !== "beta" || state.focused !== "beta") throw new Error(`HTML control selection disagreed with pointer route: ${JSON.stringify(state)}`);

  await page.evaluate(() => window.svgFoundation.mount());
  await root.focus();
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("Enter");
  state = await page.evaluate(() => window.svgFoundation.state());
  if (state.selected !== "alpha" || state.focused !== "alpha") throw new Error(`keyboard selection disagreed with pointer selection: ${JSON.stringify(state)}`);
  const accessibility = await page.evaluate(() => {
    const svg = document.querySelector("[data-scene-root-id]");
    const alpha = document.querySelector("[data-scene-object-id='alpha']");
    return {
      rootRole: svg?.getAttribute("role"), rootLabel: svg?.getAttribute("aria-label"),
      objectRole: alpha?.getAttribute("role"), objectLabel: alpha?.getAttribute("aria-label"),
      svgRole: alpha?.getAttribute("role"),
      pressed: document.querySelector("[data-scene-control-id='alpha']")?.getAttribute("aria-pressed"),
      status: document.querySelector("[data-scene-selection-status]")?.textContent,
    };
  });
  if (accessibility.rootRole !== "application" || accessibility.rootLabel !== "SVG foundation scene" || accessibility.svgRole !== null || accessibility.objectLabel !== "Alpha unit" || accessibility.pressed !== "true" || accessibility.status !== "Alpha unit") {
    throw new Error(`accessible route failed: ${JSON.stringify(accessibility)}`);
  }

  const preservedNativeInput = await page.evaluate(() => {
    const root = document.querySelector("[data-scene-root-id]");
    const before = window.svgFoundation.transitionCount();
    const composing = new KeyboardEvent("keydown", { key: "ArrowRight", bubbles: true, cancelable: true, isComposing: true });
    root.dispatchEvent(composing);
    const input = document.createElement("input");
    document.querySelector("[data-scene-controls]").appendChild(input);
    const editing = new KeyboardEvent("keydown", { key: "ArrowRight", bubbles: true, cancelable: true });
    input.dispatchEvent(editing);
    return { before, after: window.svgFoundation.transitionCount(), composingPrevented: composing.defaultPrevented, editingPrevented: editing.defaultPrevented };
  });
  if (preservedNativeInput.before !== preservedNativeInput.after || preservedNativeInput.composingPrevented || preservedNativeInput.editingPrevented) {
    throw new Error(`native editing/composition was consumed: ${JSON.stringify(preservedNativeInput)}`);
  }

  const camera = await page.evaluate(() => {
    const before = window.svgFoundation.scenePoint(60, 50);
    const zoom = window.svgFoundation.zoomAt(60, 50, 4);
    const after = window.svgFoundation.scenePoint(60, 50);
    const anchoredHit = window.svgFoundation.hit(60, 50);
    const pan = window.svgFoundation.panBy(10, -5);
    const movedHit = window.svgFoundation.hit(70, 45);
    return { before, after, anchoredHit, movedHit, zoom, pan };
  });
  if (camera.before.x !== 20 || camera.before.y !== 20 || camera.after.x !== 20 || camera.after.y !== 20 || camera.anchoredHit !== "alpha" || camera.movedHit !== "alpha" || camera.zoom.error || camera.pan.error) {
    throw new Error(`camera anchor/inverse contract failed: ${JSON.stringify(camera)}`);
  }

  await page.evaluate(() => {
    window.svgFoundation.mount();
    window.__identity = {
      root: document.querySelector("[data-scene-root-id]"),
      units: document.querySelector("[data-scene-layer-id='units']"),
      overlay: document.querySelector("[data-scene-layer-id='overlay']"),
    };
  });
  await page.mouse.click(box.x + 60, box.y + 50);
  const revision = await page.evaluate(() => {
    const alpha = document.querySelector("[data-scene-object-id='alpha']");
    const beta = document.querySelector("[data-scene-object-id='beta']");
    const alphaChild = alpha.firstElementChild;
    const betaChild = beta.firstElementChild;
    const accepted = window.svgFoundation.replaceCurrent();
    const retained = window.__identity.root === document.querySelector("[data-scene-root-id]")
      && window.__identity.units === document.querySelector("[data-scene-layer-id='units']")
      && window.__identity.overlay === document.querySelector("[data-scene-layer-id='overlay']")
      && alpha === document.querySelector("[data-scene-object-id='alpha']")
      && beta === document.querySelector("[data-scene-object-id='beta']")
      && alphaChild !== alpha.firstElementChild
      && betaChild === beta.firstElementChild;
    const reordered = window.svgFoundation.replaceReordered();
    const reorderedRetained = alpha === document.querySelector("[data-scene-object-id='alpha']")
      && beta === document.querySelector("[data-scene-object-id='beta']")
      && [...document.querySelectorAll("[data-scene-layer-id='units'] > [data-scene-object-id]")].map((node) => node.dataset.sceneObjectId).join(",") === "beta,alpha";
    const stale = window.svgFoundation.replaceStale();
    return { accepted, reordered, stale, retained, reorderedRetained, domRevision: document.querySelector("[data-scene-root-id]")?.dataset.sceneRevision };
  });
  if (revision.accepted.error || revision.accepted.state.revision !== 2 || !revision.reorderedRetained || revision.reordered.error || revision.reordered.state.revision !== 3 || !revision.retained || revision.stale.error !== "non-increasing-revision" || revision.domRevision !== "3") {
    throw new Error(`retained revision/stale refusal failed: ${JSON.stringify(revision)}`);
  }

  const captureRecovery = await page.evaluate(() => {
    const root = document.querySelector("[data-scene-root-id]");
    window.svgFoundation.capture(71);
    const captured = window.svgFoundation.state().captured;
    root.dispatchEvent(new PointerEvent("lostpointercapture", { pointerId: 71, bubbles: true }));
    const lost = window.svgFoundation.state().captured;
    window.svgFoundation.capture(72);
    root.dispatchEvent(new Event("blur"));
    const blurred = window.svgFoundation.state().captured;
    window.svgFoundation.capture(73);
    root.dispatchEvent(new PointerEvent("pointercancel", { pointerId: 73, bubbles: true }));
    return { captured, lost, blurred, cancelled: window.svgFoundation.state().captured };
  });
  if (captureRecovery.captured !== 71 || captureRecovery.lost !== null || captureRecovery.blurred !== null || captureRecovery.cancelled !== null) {
    throw new Error(`capture recovery failed: ${JSON.stringify(captureRecovery)}`);
  }

  const lifecycle = await page.evaluate(() => {
    window.svgFoundation.capture(90);
    const disposed = window.svgFoundation.dispose();
    const afterFirstDispose = { balance: window.__svgListenerBalance, roots: document.querySelectorAll("[data-scene-root-id]").length, controls: document.querySelectorAll("[data-scene-controls]").length, captured: disposed.captured };
    const cycles = [];
    for (let index = 0; index < 12; index += 1) {
      window.svgFoundation.mount();
      const mounted = { balance: window.__svgListenerBalance, observe: window.svgFoundation.observe() };
      window.svgFoundation.dispose();
      cycles.push({ mounted, balanceAfter: window.__svgListenerBalance, rootsAfter: document.querySelectorAll("[data-scene-root-id]").length, controlsAfter: document.querySelectorAll("[data-scene-controls]").length });
    }
    window.svgFoundation.mount();
    return { afterFirstDispose, cycles, final: window.svgFoundation.observe(), finalBalance: window.__svgListenerBalance };
  });
  if (lifecycle.afterFirstDispose.balance !== 0 || lifecycle.afterFirstDispose.roots !== 0 || lifecycle.afterFirstDispose.controls !== 0 || lifecycle.afterFirstDispose.captured !== null || lifecycle.finalBalance !== 8 || lifecycle.final.listeners !== 9 || lifecycle.final.frames !== 0 || lifecycle.cycles.some((cycle) => cycle.mounted.balance !== 8 || cycle.mounted.observe.listeners !== 9 || cycle.mounted.observe.frames !== 0 || cycle.balanceAfter !== 0 || cycle.rootsAfter !== 0 || cycle.controlsAfter !== 0)) {
    throw new Error(`mount/dispose leaked owned resources: ${JSON.stringify(lifecycle)}`);
  }

  for (let index = 0; index < 20; index += 1) {
    const mounted = await page.evaluate(() => {
      window.svgFoundation.dispose();
      const start = performance.now();
      window.svgFoundation.mount();
      return performance.now() - start;
    });
    mountSamples.push(mounted);
    const currentBox = await root.boundingBox();
    const started = await page.evaluate(() => performance.now());
    await page.mouse.click(currentBox.x + 60, currentBox.y + 50);
    await page.evaluate(() => new Promise((done) => requestAnimationFrame(() => done())));
    inputPaintSamples.push((await page.evaluate(() => performance.now())) - started);
  }
  observations = await page.evaluate(() => window.svgFoundation.observe());

  const cases = [
    { name: "desktop-1280x720-dpr1", viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1, hasTouch: false },
    { name: "hidpi-800x600-dpr2", viewport: { width: 800, height: 600 }, deviceScaleFactor: 2, hasTouch: false },
    { name: "touch-390x844", viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, hasTouch: true },
    { name: "reflow-320-css-pixels", viewport: { width: 320, height: 720 }, deviceScaleFactor: 1, hasTouch: false, reflow: true },
    { name: "zoom-400-percent-layout-equivalent", viewport: { width: 320, height: 180 }, screen: { width: 1280, height: 720 }, deviceScaleFactor: 1, hasTouch: false, reflow: true, zoomPercent: 400 },
  ];
  const caseResults = [];
  for (const scenario of cases) {
    const scenarioContext = await browser.newContext({
      viewport: scenario.viewport,
      screen: scenario.screen,
      deviceScaleFactor: scenario.deviceScaleFactor,
      hasTouch: scenario.hasTouch,
    });
    const scenarioPage = await scenarioContext.newPage();
    try {
      await scenarioPage.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
      await scenarioPage.waitForFunction(() => window.svgFoundation !== undefined, undefined, { timeout: 5000 });
      const betaControl = scenarioPage.locator("[data-scene-control-id='beta']");
      if (scenario.hasTouch) {
        const controlBox = await betaControl.boundingBox();
        if (!controlBox) throw new Error(`${scenario.name}: touch control has no bounds`);
        await scenarioPage.touchscreen.tap(controlBox.x + controlBox.width / 2, controlBox.y + controlBox.height / 2);
      } else {
        await betaControl.click();
      }
      const beforeResize = await scenarioPage.evaluate(() => {
        const root = document.querySelector("[data-scene-root-id]");
        const status = document.querySelector("[data-scene-selection-status]");
        return {
          innerWidth: window.innerWidth,
          innerHeight: window.innerHeight,
          dpr: window.devicePixelRatio,
          horizontalOverflow: document.documentElement.scrollWidth > window.innerWidth,
          rootVisible: root?.getBoundingClientRect().width > 0 && root?.getBoundingClientRect().height > 0,
          selected: window.svgFoundation.state().selected,
          status: status?.textContent,
        };
      });
      const rootShot = PNG.sync.read(await scenarioPage.locator("[data-scene-root-id]").screenshot());
      const expectedShotWidth = Math.round(200 * scenario.deviceScaleFactor);
      if (!beforeResize.rootVisible || beforeResize.selected !== "beta" || beforeResize.status !== "Beta unit" || beforeResize.dpr !== scenario.deviceScaleFactor || rootShot.width !== expectedShotWidth || (scenario.reflow && beforeResize.horizontalOverflow)) {
        throw new Error(`${browserFamily}/${scenario.name}: viewport contract failed: ${JSON.stringify({ beforeResize, screenshot: { width: rootShot.width, height: rootShot.height, expectedWidth: expectedShotWidth } })}`);
      }
      let resized = null;
      if (scenario.name === "desktop-1280x720-dpr1") {
        await scenarioPage.setViewportSize({ width: 1024, height: 640 });
        resized = await scenarioPage.evaluate(() => ({
          innerWidth: window.innerWidth,
          innerHeight: window.innerHeight,
          selected: window.svgFoundation.state().selected,
          rootVisible: document.querySelector("[data-scene-root-id]")?.getBoundingClientRect().width > 0,
        }));
        if (resized.innerWidth !== 1024 || resized.innerHeight !== 640 || resized.selected !== "beta" || !resized.rootVisible) {
          throw new Error(`${browserFamily}/${scenario.name}: resize contract failed: ${JSON.stringify(resized)}`);
        }
      }
      caseResults.push({
        name: scenario.name,
        result: "pass",
        viewport: scenario.viewport,
        physicalScreen: scenario.screen ?? null,
        deviceScaleFactor: scenario.deviceScaleFactor,
        touchEmulated: scenario.hasTouch,
        zoomPercent: scenario.zoomPercent ?? null,
        zoomMethod: scenario.zoomPercent ? "layout-viewport equivalent (1280 physical pixels / 320 CSS pixels)" : null,
        horizontalOverflow: beforeResize.horizontalOverflow,
        screenshotPixels: { width: rootShot.width, height: rootShot.height },
        resized,
      });
    } finally {
      await scenarioContext.close();
    }
  }
  browserMatrixEvidence = caseResults;
} finally {
  await browser.close();
  await new Promise((done) => server.close(done));
}
if (consoleErrors.length) throw new Error(`browser console errors:\n${consoleErrors.join("\n")}`);
const productionFiles = files(dist);
const rawBytes = productionFiles.reduce((sum, path) => sum + statSync(path).size, 0);
const gzipBytes = productionFiles.reduce((sum, path) => sum + gzipSync(readFileSync(path)).length, 0);
const round = (value) => Number(value.toFixed(3));
const evidence = {
  schema: "fsgg.svg-scene.browser-observation/v2",
  result: "pass",
  capturedAtUtc: new Date().toISOString(),
  candidate: { sourceSha256: sourceDigest, packageSetSha256: packageDigest },
  environment: {
    browserFamily,
    browser: await browserType.launch({ headless: true }).then(async (probe) => { const version = probe.version(); await probe.close(); return version; }),
    node: process.version, os: `${platform()} ${release()}`, logicalCpuCount: cpus().length,
    viewport: { width: 800, height: 600, deviceScaleFactor: 1 }, headless: true,
  },
  sceneCost: observations,
  startup: { productionFiles: productionFiles.length, rawBytes, gzipBytes, coldNetworkCache: false },
  timing: {
    sampleCount: mountSamples.length,
    mountMilliseconds: { median: round(median(mountSamples)), maximum: round(Math.max(...mountSamples)) },
    inputToNextAnimationFrameMilliseconds: { median: round(median(inputPaintSamples)), maximum: round(Math.max(...inputPaintSamples)) },
  },
  functional: {
    pointerKeyboardSameObject: true, accessibleDomRoute: true, anchoredZoomAndInversePicking: true,
    retainedRootAndLayersAcrossRevision: true, staleRevisionRefused: true, mountDisposeCycles: 12,
    finalOwnedListenerCount: observations.listeners, scheduledFrameCount: observations.frames,
    sessionHost: sessionEvidence,
    externalSessionHost: externalSessionEvidence,
    animationHost: animationEvidence,
  },
  browserMatrix: browserMatrixEvidence,
  document: { ...documentEvidence, exportedSvg: undefined },
  unavailable: [
    "Input timing ends at the next animation-frame callback; no compositor presentation timestamp was captured.",
    "Heap and detached-node measurements were not captured in this focused foundation fixture.",
    "Touch input was emulated; physical touch hardware and mobile GPUs were not observed.",
    "No actual assistive-technology session or announcement stream was available to this automated harness."
  ],
  claims: { thresholdEstablished: false, completeM9Qualification: false, packagePublished: false },
};
const exportedPath = output.replace(/\.json$/, ".exported.svg");
writeFileSync(exportedPath, `${documentEvidence.exportedSvg}\n`);
writeFileSync(output, `${JSON.stringify(evidence, null, 2)}\n`);
console.log(JSON.stringify({ result: evidence.result, browser: evidence.environment.browser, sceneCost: observations, document: evidence.document, rawBytes, gzipBytes, mountMedianMs: evidence.timing.mountMilliseconds.median, inputMedianMs: evidence.timing.inputToNextAnimationFrameMilliseconds.median, output, exportedPath }));
