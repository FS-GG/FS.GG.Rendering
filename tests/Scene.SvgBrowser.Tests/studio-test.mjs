import { createServer } from "node:http";
import { readFileSync, statSync, existsSync } from "node:fs";
import { resolve, dirname, extname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium, firefox, webkit } from "playwright-core";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "dist");
const family = process.argv[process.argv.indexOf("--browser") + 1];
const kind = {chromium,firefox,webkit}[family];
const server=createServer((request,response)=>{const path=resolve(root,(request.url==="/"?"studio.html":request.url.slice(1)));if(!path.startsWith(root)||!existsSync(path)||statSync(path).isDirectory()){response.writeHead(404).end();return;}const type=extname(path)===".js"?"text/javascript":extname(path)===".html"?"text/html":"application/octet-stream";response.writeHead(200,{"content-type":type});response.end(readFileSync(path));});
await new Promise(done=>server.listen(0,"127.0.0.1",done));
const browser=await kind.launch({headless:true});
try {
  const page=await browser.newPage();
  await page.goto(`http://127.0.0.1:${server.address().port}/studio.html`);
  await page.waitForFunction(()=>window.svgStudioFixture);
  const labels=await page.evaluate(()=>[...document.querySelectorAll("button")].map(x=>x.getAttribute("aria-label")));
  await page.getByRole("button",{name:"Rectangle",exact:true}).click();
  const created=await page.evaluate(()=>window.svgStudioFixture.observation());
  const preview=await page.evaluate(()=>window.svgStudioFixture.previewGesture());
  const cancelled=await page.evaluate(()=>window.svgStudioFixture.cancelGesture());
  const scene=await page.evaluate(()=>window.svgStudioFixture.sceneRoundtrip());
  const camera=await page.evaluate(()=>window.svgStudioFixture.camera());
  await page.getByRole("button",{name:"Grid snapping",exact:true}).click();
  await page.getByRole("button",{name:"Object",exact:true}).click();
  await page.getByRole("button",{name:"Freeform",exact:true}).click();
  await page.getByRole("button",{name:"Boundary",exact:true}).click();
  const placement=await page.evaluate(()=>window.svgStudioFixture.placement());
  const geometry={};
  for(const operation of ["union","intersection","difference","xor","curve"])
    geometry[operation]=await page.evaluate(operation=>window.svgStudioFixture.geometry(operation),operation);
  const geometryCancelled=await page.evaluate(()=>window.svgStudioFixture.cancelGeometry());
  const resource=await page.evaluate(()=>window.svgStudioFixture.resourceRoundtrip());
  const disposed=await page.evaluate(()=>window.svgStudioFixture.dispose());
  const remounted=await page.evaluate(()=>window.svgStudioFixture.mount());
  const disposedAgain=await page.evaluate(()=>window.svgStudioFixture.dispose());
  const roots=await page.evaluate(()=>document.querySelectorAll("[data-fsgg-document-id]").length);
  const evidence={labels,created,preview,cancelled,scene,camera,placement,resource,geometryCancelled,disposed,remounted,disposedAgain,roots};
  await page.waitForFunction(()=>window.svgStudioFixture.fontStatus().ready||window.svgStudioFixture.fontStatus().diagnostic,{timeout:5000});
  const font=await page.evaluate(()=>window.svgStudioFixture.fontStatus());
  await page.evaluate(()=>window.svgStudioFixture.disposeFont());
  if(evidence.labels.join(",")!=="Region,Boundary,Object,Rectangle,Ellipse,Polygon,Path,Group,Ungroup,Align left,Bring forward,Apply style,Grid snapping,Freeform,Undo,Redo,Apply translation"||evidence.created.Revision!==1||evidence.created.SelectionCount!==1||!evidence.preview.acceptedUnchanged||evidence.preview.during===evidence.preview.after||evidence.cancelled[0]!==evidence.cancelled[1]||evidence.scene.schema!=="fsgg.svg-scene/1"||evidence.scene.entities!==1||evidence.scene.x!==11||evidence.scene.y!==13||!evidence.camera.style.includes("matrix(2")||evidence.camera.picked!=="rectangle-1"||evidence.placement.revision!==5||evidence.placement.children!==3||!evidence.placement.freeform||!evidence.resource.embedded||evidence.resource.fonts!==1||evidence.resource.children!==1||!evidence.geometryCancelled||Object.values(geometry).some(value=>value.operations!==1||value.contours<1||!value.duplicateRefused)||geometry.difference.contours<2||!font.ready||evidence.disposed!==0||evidence.disposedAgain!==0||evidence.roots!==0)throw new Error(JSON.stringify({evidence,font,geometry}));
  console.log(`svg-studio-browser: ${family}=passed repeated-mount-dispose=passed native-controls=passed cancellation=passed offline-font=passed`);
} finally { await browser.close(); await new Promise(done=>server.close(done)); }
