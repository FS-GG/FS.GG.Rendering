// Actual compiled product observations; fixture setup focus is never a native AT proof.
export const MUTATION_CASES=['100-identical-projections','changed-content-label','real-object-layer-reorder','added-object','removed-object','selection-status','dispose'];
export async function producerMutationCompositionControl(page){
 const cases=[];
 async function observe(mode){return page.evaluate(selected=>{
  const root=document.querySelector('#fixture'),beforeButton=root.querySelector('[data-scene-control-id="alpha"]');
  const observer=new MutationObserver(()=>{});observer.observe(root,{subtree:true,childList:true,attributes:true,attributeOldValue:true,characterData:true});
  const results=[];
  if(selected==='stable')for(let i=0;i<100;i++)results.push(window.svgFoundation.advanceFocusControl('stable'));
  else if(selected==='remove-beta')results.push(window.svgFoundation.advanceFocusControl(selected));
  else if(selected==='selection')beforeButton.click();
  else if(selected==='dispose')window.svgFoundation.dispose();
  else results.push(window.svgFoundation.advanceMutationControl(selected));
  const records=observer.takeRecords();observer.disconnect();
  const mutations=records.map(m=>({type:m.type,attribute:m.attributeName,targetId:m.target.id??'',sceneRoot:m.target.matches?.('svg[data-scene-root-id]')===true,added:m.addedNodes.length,removed:m.removedNodes.length}));
  return {errors:results.map(x=>x.error),mutations,sameButton:root.querySelector('[data-scene-control-id="alpha"]')===beforeButton,focused:document.activeElement===beforeButton,
   buttons:[...root.querySelectorAll('[data-scene-control-id]')].map(x=>({id:x.dataset.sceneControlId,label:x.textContent,pressed:x.getAttribute('aria-pressed')})),
   objects:[...root.querySelectorAll('[data-scene-layer-id="units"] > [data-scene-object-id]')].map(x=>x.getAttribute('data-scene-object-id')),
   layers:[...root.querySelectorAll('[data-scene-layer-id]')].map(x=>x.getAttribute('data-scene-layer-id')),
   status:root.querySelector('[role="status"]')?.textContent,rectWidth:root.querySelector('[data-scene-object-id="alpha"] rect')?.getAttribute('width'),svgCount:root.querySelectorAll('svg').length};
 },mode);}
 async function prime(){await page.evaluate(()=>{window.svgFoundation.mount();window.svgFoundation.replaceCurrent();});await page.locator('#fixture [data-scene-control-id="alpha"]').focus();}
 function require(v,cause,receipt){if(!v)throw Error(cause+': '+JSON.stringify(receipt));}
 function clean(v){require(v.errors.every(x=>!x),'actual mutation projection refused',v);}
 await prime();let value=await observe('stable');clean(value);
 require(value.mutations.length===100&&value.mutations.every(x=>x.type==='attributes'&&x.attribute==='data-scene-revision'&&x.sceneRoot)&&value.sameButton&&value.focused,'identical projections made redundant DOM changes or lost focus',value);
 cases.push({case:MUTATION_CASES[0],updates:100,mutationCount:100,allowedAttribute:'data-scene-revision',focused:true});
 await prime();value=await observe('changed');clean(value);
 require(value.buttons.find(x=>x.id==='alpha')?.label==='Alpha changed'&&value.rectWidth==='35'&&value.mutations.some(x=>x.type==='childList')&&value.sameButton&&value.focused,'actual changed content/label not presented',value);cases.push({case:MUTATION_CASES[1],receipt:value});
 await prime();value=await observe('reordered');clean(value);
 require(JSON.stringify(value.objects)==='["beta","alpha"]'&&JSON.stringify(value.layers)==='["overlay","units"]'&&value.sameButton&&value.focused&&value.mutations.some(x=>x.type==='childList'),'actual object/layer reorder/focus incorrect',value);cases.push({case:MUTATION_CASES[2],receipt:value});
 await prime();value=await observe('add');clean(value);
 require(JSON.stringify(value.objects)==='["alpha","beta","gamma"]'&&value.buttons.some(x=>x.id==='gamma'&&x.label==='Gamma unit')&&value.sameButton&&value.focused,'actual added object missing',value);cases.push({case:MUTATION_CASES[3],receipt:value});
 await prime();value=await observe('remove-beta');clean(value);
 require(JSON.stringify(value.objects)==='["alpha"]'&&!value.buttons.some(x=>x.id==='beta')&&value.sameButton&&value.focused,'actual removal semantics changed',value);cases.push({case:MUTATION_CASES[4],receipt:value});
 await prime();value=await observe('selection');clean(value);
 require(value.buttons.find(x=>x.id==='alpha')?.pressed==='true'&&value.status==='Alpha unit'&&value.sameButton&&value.focused,'actual selection/status update missing',value);cases.push({case:MUTATION_CASES[5],receipt:value});
 await prime();value=await observe('dispose');clean(value);
 require(value.svgCount===0&&value.buttons.length===0&&!value.sameButton&&value.mutations.some(x=>x.type==='childList'),'actual disposal retained owned nodes',value);cases.push({case:MUTATION_CASES[6],receipt:value});
 return {scope:'actual-packed-SvgBrowserHost-MutationObserver',cases,fixtureOnlySetupFocus:true,fixtureSelectionClicks:1,nativeJourneyAcceptance:false};
}
