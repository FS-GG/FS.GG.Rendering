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
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined, undefined, { timeout: 5000 }).catch((error) => {
    throw new Error(`fixture API unavailable: ${consoleErrors.join("\n") || error.message}`);
  });
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
