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
  await page.evaluate(()=>Object.defineProperty(navigator,"getGamepads",{configurable:true,value:()=>[{index:2,buttons:[{pressed:true},{pressed:false}]}]}));
  const nativeGamepads=await page.evaluate(()=>window.svgStudioFixture.nativeGamepads());
  const labels=await page.evaluate(()=>[...document.querySelector("[aria-label='SVG art tools']").querySelectorAll("button")].map(x=>x.getAttribute("aria-label")));
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
  const workspace=await page.evaluate(()=>window.svgStudioFixture.workspaceJourney());
  const workspaceUi={};
  for(const [buttonName,dialogName,key] of [["Open command palette","Command palette","palette"],["Open possible input help","Possible input help","help"],["Rebind selected command","Rebind command","rebind"]]){
    await page.getByRole("button",{name:buttonName,exact:true}).click();
    const dialog=page.getByRole("dialog",{name:dialogName,exact:true});
    workspaceUi[key]={visible:await dialog.isVisible(),focused:await dialog.evaluate(node=>node===document.activeElement),text:await dialog.textContent()};
    await page.keyboard.press("Escape");
    workspaceUi[key].restored=await page.getByRole("button",{name:buttonName,exact:true}).evaluate(node=>node===document.activeElement);
  }
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.evaluate(()=>{window.svgInputKeyEvents=[];document.getElementById('studio').addEventListener('keydown',event=>window.svgInputKeyEvents.push({key:event.key,code:event.code,ctrl:event.ctrlKey,meta:event.metaKey,alt:event.altKey,shift:event.shiftKey,target:event.target.tagName}));});
  await page.evaluate(()=>document.getElementById("studio").focus());
  await page.keyboard.press("Control+k");
  const logical=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.keyboard.press("Meta+k");
  const meta=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.keyboard.press("q");
  const physical=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.keyboard.down("Control");
  await page.keyboard.press("x");
  await page.keyboard.press("s");
  await page.keyboard.up("Control");
  const sequence=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.locator("input[aria-label='Translate X']").focus();
  await page.keyboard.press("q");
  const nativeControl=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>{
    const root=document.querySelector('svg');
    root.focus();
    root.dispatchEvent(new CompositionEvent('compositionstart',{bubbles:true}));
    root.dispatchEvent(new KeyboardEvent('keydown',{key:'k',code:'KeyK',ctrlKey:true,bubbles:true,cancelable:true,isComposing:true}));
    root.dispatchEvent(new CompositionEvent('compositionend',{bubbles:true}));
  });
  const composition=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>{
    window.svgStudioFixture.inputReset();
    const event=new KeyboardEvent('keydown',{key:'z',code:'KeyZ',ctrlKey:true,altKey:true,bubbles:true,cancelable:true});
    Object.defineProperty(event,'getModifierState',{value:name=>name==='AltGraph'});
    document.getElementById('studio').dispatchEvent(event);
  });
  const altGraph=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>{window.svgStudioFixture.inputReset();window.svgStudioFixture.inputBeginCapture();document.getElementById('studio').focus();});
  await page.keyboard.press("z");
  const capture=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.locator("svg").dispatchEvent("pointerdown",{pointerId:81,pointerType:"mouse",button:0,bubbles:true,cancelable:true});
  await page.locator("svg").dispatchEvent("pointerup",{pointerId:81,pointerType:"mouse",button:0,bubbles:true,cancelable:true});
  const pointer=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>window.svgStudioFixture.inputReset());
  await page.locator("svg").dispatchEvent("pointerdown",{pointerId:82,pointerType:"touch",button:0,bubbles:true,cancelable:true});
  await page.locator("svg").dispatchEvent("pointerup",{pointerId:82,pointerType:"touch",button:0,bubbles:true,cancelable:true});
  const touch=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  await page.evaluate(()=>{window.svgStudioFixture.inputReset();window.svgStudioFixture.inputPads([true,true]);window.svgStudioFixture.inputPads([false,true]);window.dispatchEvent(new Event('blur'));});
  const gamepad=await page.evaluate(()=>window.svgStudioFixture.inputObserved());
  const inputLifecycle=await page.evaluate(()=>window.svgStudioFixture.inputLifecycle());
  const inputKeyEvents=await page.evaluate(()=>window.svgInputKeyEvents);
  const geometry={};
  for(const operation of ["union","intersection","difference","xor","curve"])
    geometry[operation]=await page.evaluate(operation=>window.svgStudioFixture.geometry(operation),operation);
  const geometryCancelled=await page.evaluate(()=>window.svgStudioFixture.cancelGeometry());
  const resource=await page.evaluate(()=>window.svgStudioFixture.resourceRoundtrip());
  const disposedInput=await page.evaluate(()=>window.svgStudioFixture.disposeInput());
  const disposed=await page.evaluate(()=>window.svgStudioFixture.dispose());
  const remounted=await page.evaluate(()=>window.svgStudioFixture.mount());
  const disposedAgain=await page.evaluate(()=>window.svgStudioFixture.dispose());
  const roots=await page.evaluate(()=>document.querySelectorAll("[data-fsgg-document-id]").length);
  const evidence={labels,created,preview,cancelled,scene,camera,placement,workspace,workspaceUi,logical,meta,physical,sequence,nativeControl,composition,altGraph,capture,pointer,touch,gamepad,nativeGamepads,inputLifecycle,inputKeyEvents,resource,geometryCancelled,disposedInput,disposed,remounted,disposedAgain,roots};
  await page.waitForFunction(()=>window.svgStudioFixture.fontStatus().ready||window.svgStudioFixture.fontStatus().diagnostic,{timeout:5000});
  const font=await page.evaluate(()=>window.svgStudioFixture.fontStatus());
  await page.evaluate(()=>window.svgStudioFixture.disposeFont());
  // Keep the real FontFace load, but hold delivery to the production callback.
  // This makes load -> Dispose -> late settlement deterministic in every family.
  const fontSettlement = {};
  for (const mode of ["ordinary", "ordinary-failure", "disposed-success", "disposed-failure"]) {
    fontSettlement[mode] = await page.evaluate(async mode => {
      const originalLoad = FontFace.prototype.load;
      const originalAdd = FontFaceSet.prototype.add;
      const originalRevoke = URL.revokeObjectURL;
      let face, actualLoad, delayedLoad, release;
      let adds = 0, revokes = 0;
      FontFace.prototype.load = function () {
        if (face) throw new Error("one font load per settlement case");
        face = this;
        actualLoad = originalLoad.call(this);
        delayedLoad = actualLoad.then(loaded => new Promise((resolve, reject) => {
          release = () => mode.endsWith("failure")
            ? reject(new Error("controlled late font-load rejection")) : resolve(loaded);
        }));
        return delayedLoad;
      };
      FontFaceSet.prototype.add = function (value) {
        if (value === face) adds++;
        return originalAdd.call(this, value);
      };
      URL.revokeObjectURL = function (url) {
        revokes++;
        return originalRevoke.call(this, url);
      };
      try {
        window.svgStudioFixture.resourceRoundtrip();
        await actualLoad;
        // The native font has loaded; its production completion is still pending.
        await Promise.resolve();
        if (!release || face.status !== "loaded") throw new Error("native font did not load");
        const pending = window.svgStudioFixture.fontStatus();
        if (pending.ready || pending.diagnostic || document.fonts.has(face))
          throw new Error("font callback settled before its gate");
        if (mode.startsWith("disposed-")) window.svgStudioFixture.disposeFont();
        release();
        await delayedLoad.catch(() => {});
        // Drain the production success/failure continuation before inspecting custody.
        await Promise.resolve();
        let readyBeforeDispose = false, diagnosticBeforeDispose = null;
        if (mode === "ordinary") {
          readyBeforeDispose = window.svgStudioFixture.fontStatus().ready;
          if (!readyBeforeDispose || !document.fonts.has(face))
            throw new Error("ordinary loaded font was not activated");
          window.svgStudioFixture.disposeFont();
        }
        if (mode === "ordinary-failure") {
          const failed = window.svgStudioFixture.fontStatus();
          diagnosticBeforeDispose = failed.diagnostic;
          if (failed.ready || diagnosticBeforeDispose !== "controlled late font-load rejection")
            throw new Error("ordinary font failure did not retain its diagnostic");
        }
        window.svgStudioFixture.disposeFont();
        return { nativeStatus: face.status, adds, revokes, readyBeforeDispose, diagnosticBeforeDispose,
          presentAfterDispose: document.fonts.has(face) };
      } finally {
        // Retire the fixture while its instrumentation is still active, then restore it.
        window.svgStudioFixture.disposeFont();
        FontFace.prototype.load = originalLoad;
        FontFaceSet.prototype.add = originalAdd;
        URL.revokeObjectURL = originalRevoke;
      }
    }, mode);
    const proof = fontSettlement[mode];
    if (proof.nativeStatus !== "loaded" || proof.presentAfterDispose || proof.revokes !== 1 ||
        proof.adds !== (mode === "ordinary" ? 1 : 0) ||
        proof.readyBeforeDispose !== (mode === "ordinary") ||
        proof.diagnosticBeforeDispose !== (mode === "ordinary-failure" ? "controlled late font-load rejection" : null))
      throw new Error(JSON.stringify({ fontSettlement }));
  }
  if(evidence.nativeGamepads.length!==1||evidence.nativeGamepads[0].source!=="gamepad:2"||evidence.nativeGamepads[0].buttons.join(",")!=="true,false")throw new Error(JSON.stringify({nativeGamepads:evidence.nativeGamepads}));
  if(evidence.labels.join(",")!=="Region,Boundary,Object,Rectangle,Ellipse,Polygon,Path,Group,Ungroup,Align left,Bring forward,Apply style,Grid snapping,Freeform,Undo,Redo"||evidence.created.Revision!==1||evidence.created.SelectionCount!==1||!evidence.preview.acceptedUnchanged||evidence.preview.during===evidence.preview.after||evidence.cancelled[0]!==evidence.cancelled[1]||evidence.scene.schema!=="fsgg.svg-scene/1"||evidence.scene.entities!==1||evidence.scene.x!==11||evidence.scene.y!==13||!evidence.camera.style.includes("matrix(2")||evidence.camera.picked!=="rectangle-1"||evidence.placement.revision!==5||evidence.placement.children!==3||!evidence.placement.freeform||evidence.workspace.mode!=="review"||!evidence.workspace.sideCollapsed||evidence.workspace.modalFocus!=="workspace.possible-input-help"||evidence.workspace.restoredFocus!=="studio-fixture--scene"||!evidence.workspace.layoutRoundtrip||!evidence.workspace.authoringPreserved||!evidence.workspace.cameraPreserved||Object.values(evidence.workspaceUi).some(value=>!value.visible||!value.focused||!value.restored)||!evidence.workspaceUi.rebind.text.includes("Conflict feedback")||!evidence.logical.includes("invoke:workspace.palette")||!evidence.meta.includes("invoke:workspace.palette")||!evidence.physical.includes("invoke:workspace.physical")||!evidence.sequence.includes("invoke:workspace.sequence")||evidence.nativeControl.length!==0||evidence.composition.some(x=>x.startsWith("invoke:"))||evidence.altGraph.some(x=>x.startsWith("invoke:"))||!evidence.capture.some(x=>x.startsWith("captured:"))||!evidence.pointer.includes("invoke:workspace.pointer")||!evidence.touch.includes("invoke:workspace.touch")||!evidence.gamepad.includes("held:workspace.gamepad:true")||!evidence.gamepad.includes("held:workspace.gamepad:false")||evidence.inputLifecycle.OwnedListenerCount<10||evidence.inputLifecycle.OwnedDeadlineCount!==0||evidence.inputLifecycle.GamepadPollScheduled||evidence.disposedInput.OwnedListenerCount!==0||evidence.disposedInput.OwnedDeadlineCount!==0||evidence.disposedInput.GamepadPollScheduled||!evidence.disposedInput.IsDisposed||!evidence.resource.embedded||evidence.resource.fonts!==1||evidence.resource.children!==1||!evidence.geometryCancelled||Object.values(geometry).some(value=>value.operations!==1||value.contours<1||!value.duplicateRefused)||geometry.difference.contours<2||!font.ready||evidence.disposed!==0||evidence.disposedAgain!==0||evidence.roots!==0)throw new Error(JSON.stringify({evidence,font,geometry}));
  console.log(`svg-studio-browser: ${family}=passed input-devices=passed workspace-accessibility=passed repeated-mount-dispose=passed native-controls=passed cancellation=passed offline-font=passed font-late-disposal=passed`);
} finally { await browser.close(); await new Promise(done=>server.close(done)); }
