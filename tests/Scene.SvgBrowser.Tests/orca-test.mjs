import { createServer } from "node:http";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, readFileSync, rmSync, statSync, writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { chromium } from "playwright-core";

const fixture = dirname(fileURLToPath(import.meta.url));
const dist = resolve(fixture, "dist");
const outputIndex = process.argv.indexOf("--out");
if (outputIndex < 0) throw new Error("--out is required");
const output = resolve(process.argv[outputIndex + 1]);

function mime(path) {
  if (extname(path) === ".html") return "text/html; charset=utf-8";
  if (extname(path) === ".js") return "text/javascript; charset=utf-8";
  return "application/octet-stream";
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
const address = server.address();
const profile = resolve(fixture, ".orca-chromium-profile");
rmSync(profile, { recursive: true, force: true });
const browser = await chromium.launchPersistentContext(profile, {
  headless: false,
  args: [`--app=http://127.0.0.1:${address.port}/`, "--force-renderer-accessibility=complete", "--disable-gpu"],
  viewport: { width: 1024, height: 720 },
});
let page = browser.pages()[0] ?? await browser.newPage();
const waitForOrca = () => page.waitForTimeout(1200);
const focusWindow = (title) => {
  const ids = execFileSync("xdotool", ["search", "--onlyvisible", "--name", title], { encoding: "utf8" }).trim().split(/\s+/);
  const id = ids.at(-1);
  execFileSync("xdotool", ["windowactivate", "--sync", id]);
  return id;
};
const activateWebContent = async (title) => {
  const id = focusWindow(title);
  execFileSync("xdotool", ["mousemove", "--window", id, "20", "140", "click", "1"]);
  await waitForOrca();
};
const desktopKey = async (key) => {
  execFileSync("xdotool", ["key", "--clearmodifiers", key]);
  await page.waitForTimeout(250);
};
const focusForOrca = async (selector) => {
  await page.locator(selector).focus();
  await waitForOrca();
};
const enterDocumentForOrca = async (selector, expectedSpeech) => {
  const speechLog = process.env.SVG_SCENE_ORCA_SPEECH_LOG;
  if (!speechLog) throw new Error("SVG_SCENE_ORCA_SPEECH_LOG is required");
  for (let index = 0; index < 6; index += 1) {
    await desktopKey("F6");
    await waitForOrca();
    await focusForOrca(selector);
    if (existsSync(speechLog) && readFileSync(speechLog, "utf8").toLowerCase().includes(expectedSpeech.toLowerCase())) return;
  }
  throw new Error(`Orca did not enter the browser document and announce ${expectedSpeech}`);
};
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined);
  await page.waitForTimeout(4000);
  await activateWebContent("SVG foundation browser fixture");
  // Playwright targets the browser widget; Orca independently observes the
  // resulting native AT-SPI focus event and generates the asserted speech.
  await enterDocumentForOrca("[data-scene-root-id='svg-foundation-root']", "SVG foundation scene");
  await focusForOrca("[data-scene-control-id='alpha']");
  await page.keyboard.press("Space");
  await waitForOrca();
  const afterAlphaControl = await page.evaluate(() => window.svgFoundation.state());

  await focusForOrca("[data-scene-control-id='beta']");
  await page.keyboard.press("Space");
  await waitForOrca();
  const afterHtmlControl = await page.evaluate(() => window.svgFoundation.state());

  await focusForOrca("[data-scene-root-id='svg-foundation-root']");
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("Enter");
  await waitForOrca();
  const afterSvgKeyboard = await page.evaluate(() => window.svgFoundation.state());

  const nonInteractive = page.locator("[data-scene-object-id='label']");
  const decorationFocusable = await nonInteractive.evaluate((node) => node.tabIndex >= 0);

  await page.goto(`http://127.0.0.1:${address.port}/studio.html`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgStudioFixture !== undefined);
  await page.waitForTimeout(4000);
  await activateWebContent("SVG art studio fixture");
  await enterDocumentForOrca("button[aria-label='Rectangle']", "Rectangle");
  await page.keyboard.press("Space");
  await waitForOrca();
  await waitForOrca();
  const studioAfterCreate = await page.evaluate(() => window.svgStudioFixture.observation());
  await focusForOrca("input[aria-label='Translate X']");
  execFileSync("xdotool", ["type", "--clearmodifiers", "not-a-number"]);
  await focusForOrca("button[aria-label='Apply translation']");
  await page.keyboard.press("Enter");
  await waitForOrca();
  await waitForOrca();
  const validationFeedback = await page.locator("[role='status']").textContent();
  const selectionFeedback = await page.getByLabel("Current selection").textContent();
  writeFileSync(output, JSON.stringify({
    alphaHtmlControl: { selected: afterAlphaControl.selected, focused: afterAlphaControl.focused },
    betaHtmlControl: { selected: afterHtmlControl.selected, focused: afterHtmlControl.focused },
    svgKeyboard: { selected: afterSvgKeyboard.selected, focused: afterSvgKeyboard.focused },
    studio: {
      revision: studioAfterCreate.Revision,
      selectionCount: studioAfterCreate.SelectionCount,
      selectionFeedback,
      validationFeedback,
    },
    negativeControl: { nonInteractiveDecorationFocusable: decorationFocusable },
  }, null, 2) + "\n");
} finally {
  await browser.close();
  server.close();
}
