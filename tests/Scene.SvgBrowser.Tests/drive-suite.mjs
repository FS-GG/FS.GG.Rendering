import { chromium } from "playwright-core";
import { readFileSync, writeFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { fileURLToPath } from "node:url";

const argument = (name) => process.argv[process.argv.indexOf(name) + 1];
if (!process.argv.includes("--base-url") || !process.argv.includes("--out")) throw new Error("Usage: drive-suite.mjs --base-url URL --out RESULT [--exercise-resume] [--mutant NAME]");
const url = new URL("suite.html", argument("--base-url"));
const mutant = process.argv.includes("--mutant") ? argument("--mutant") : null;
if (mutant && !["unnecessary-rebuild", "listener-leak", "excessive-document-acceptance"].includes(mutant)) throw new Error("Unknown mutant");
// This driver is explicitly the retained container software profile. A supplied URL
// does not turn its local machine into a host-GPU measurement route.
const deadlineMilliseconds = process.argv.includes("--deadline-ms") ? Number(argument("--deadline-ms")) : 180000;
if (!Number.isFinite(deadlineMilliseconds) || deadlineMilliseconds < 1000 || deadlineMilliseconds > 750000) throw new Error("Invalid driver deadline (1000–750000 milliseconds)");
const executablePath = process.argv.includes("--executable") ? argument("--executable") : undefined;
const sha256 = (value) => `sha256:${createHash("sha256").update(value).digest("hex")}`;
const driverSha256 = sha256(readFileSync(fileURLToPath(import.meta.url)));
const executableSha256 = executablePath ? sha256(readFileSync(executablePath)) : null;
const browserPromise = chromium.launch({ headless: true, args: ["--disable-gpu"], executablePath, timeout: Math.min(30000, deadlineMilliseconds) });
let shutdownPromise;
const closeBrowser = () => shutdownPromise ??= browserPromise.then((browser) => browser.close(), () => {});
const deadline = setTimeout(() => {
  console.error(`Driver deadline exceeded: ${deadlineMilliseconds}ms`);
  closeBrowser().then(() => process.exit(124), (error) => { console.error(error); process.exit(125); });
}, deadlineMilliseconds);
for (const signal of ["SIGTERM", "SIGINT"]) process.once(signal, () => {
  closeBrowser().then(() => process.exit(signal === "SIGTERM" ? 143 : 130), (error) => { console.error(error); process.exit(125); });
});
try {
  const browser = await browserPromise;
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1, acceptDownloads: true });
  if (mutant) await context.addInitScript((name) => {
    if (window === window.top) return;
    const poll = setInterval(() => {
      const f = window.svgFoundation;
      if (!f) return;
      clearInterval(poll);
      if (name === "unnecessary-rebuild") { const original = f.performanceSelect; f.performanceSelect = (i) => { f.performanceMount("ordinary"); return original(i); }; }
      if (name === "listener-leak") { const original = f.animationDispose; f.animationDispose = () => ({ ...original(), listeners: 1 }); }
      if (name === "excessive-document-acceptance") f.performanceExcessiveDocument = () => ({ rootRetained: false, error: null });
    }, 0);
  }, mutant);
  const page = await context.newPage();
  await page.goto(url.href);
  await page.waitForFunction(() => !document.querySelector("#run").disabled || document.querySelector("#progress").textContent.includes("failed:"), undefined, { timeout: 30000 });
  if (await page.locator("#run").isDisabled()) throw new Error(await page.locator("#progress").textContent());
  await page.locator("#run").click();
  let resumeObserved = false;
  if (process.argv.includes("--exercise-resume")) {
    await page.waitForFunction(() => window.svgSuite.result?.cases.length >= 2, undefined, { timeout: 120000 });
    await page.reload();
    await page.locator("#resume").waitFor();
    await page.waitForFunction(() => !document.querySelector("#resume").disabled, undefined, { timeout: 30000 });
    const saved = await page.evaluate(() => window.svgSuite.result.cases.length);
    await page.locator("#resume").click(); resumeObserved = saved >= 2;
  }
  await page.waitForFunction(() => window.svgSuite.result?.completedAtUtc && !window.svgSuite.running, undefined, { timeout: Math.min(750000, deadlineMilliseconds) });
  const downloadEvent = page.waitForEvent("download");
  await page.locator("#download").click();
  const download = await downloadEvent;
  await download.saveAs(argument("--out"));
  const result = JSON.parse(readFileSync(argument("--out"), "utf8"));
  result.driver = { profile: executablePath ? "container-software-diagnostic-explicit-browser" : "container-software-historical", historicalBaselineEquivalence: false, browser: browser.version(), executablePath: executablePath ?? "Playwright default", executableSha256, driverSha256, args: ["--disable-gpu"], baseUrl: url.href, resumeObserved, downloadObserved: true };
  writeFileSync(argument("--out"), `${JSON.stringify(result, null, 2)}\n`);
  if (mutant) {
    const gate = { "unnecessary-rebuild": "unnecessary-rebuild gate", "listener-leak": "listener/resource-leak gate", "excessive-document-acceptance": "excessive-document gate" }[mutant];
    if (!result.cases.some((c) => c.error?.includes(gate))) throw new Error(`Mutant survived: ${mutant}`);
    console.log(JSON.stringify({ mutant, result: "killed", gate, output: argument("--out") }));
  } else {
    if (result.outcome !== "container-controls-completed" || result.integrity !== "verified-before-and-after") throw new Error(`Suite failed: ${JSON.stringify(result.cases.filter((c) => c.outcome === "failed"))} ${result.error ?? ""}`);
    console.log(JSON.stringify({ outcome: result.outcome, cases: result.cases.length, resumeObserved, output: argument("--out") }));
  }
} finally { clearTimeout(deadline); await closeBrowser(); }
