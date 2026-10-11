// Ordinary hosted product DOM contract. No private mmap/native-AT custody claim.
import {createHash} from 'node:crypto';
import {createServer} from 'node:http';
import {createReadStream, readFileSync, writeFileSync, readdirSync, statSync} from 'node:fs';
import {createRequire} from 'node:module';
import {join, resolve, dirname} from 'node:path';
import {pathToFileURL} from 'node:url';
import os from 'node:os';
import {settlePendingCreation} from './focus-comparison-lifecycle.mjs';
import {PRODUCT_FIXTURE_ENTRY,fixtureEntryProof} from './focus-comparison-entrypoint.mjs';
const [inputPath, outputPath] = process.argv.slice(2);
if (!inputPath || !outputPath) throw new Error('exact inputs and absent result path required');
const inputBytes=readFileSync(inputPath), c=JSON.parse(inputBytes);
const hash=b=>createHash('sha256').update(b).digest('hex');
const requireValue=(v,cause)=>{if(!v)throw new Error(cause);};
requireValue(c.workSeconds===60 && c.cleanupSeconds===10 && c.variants.length===2 && c.variants[0].name==='original' && c.variants[1].name==='candidate','comparison contract differs');
requireValue(c.variants[0].expectedRetainedFocus===false && c.variants[1].expectedRetainedFocus===true && c.nativeJourneyAcceptance===false,'literal product outcomes differ');
const start=performance.now(), workDeadline=start+60000, finalDeadline=start+70000;
let launchPromise=null, pendingContext=null;
let first=null, results=null, browser=null, context=null, server=null, selected=null, browserVersion=null, browserDisconnected=false, executable=null, runtime=null;
const fixtureEntries=[], pageDiagnostics=[];
let mutationEvidence=null;
const served=[], cleanup={context:'not-created',browser:'not-created',server:'not-created'}, cancellations=[];
let cancelReject;
const canceled=new Promise((_,reject)=>{cancelReject=reject;});
for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>{cancellations.push(signal);cancelReject(new Error('original hosted comparison canceled: '+signal));});
async function bounded(promise,deadline,cause,allowCancel=true){let timer;try{return await Promise.race([promise,...(allowCancel?[canceled]:[]),new Promise((_,reject)=>{timer=setTimeout(()=>reject(new Error(cause)),Math.max(0,deadline-performance.now()));})]);}finally{clearTimeout(timer);}}
async function hashFile(path){const value=createHash('sha256');for await(const chunk of createReadStream(path))value.update(chunk);return value.digest('hex');}
function bindEvidence(evidence,variant){
 requireValue(evidence.scope==='actual-SvgBrowserHost-ReplaceScene-focus-control' && evidence.expectedRetainedFocus===variant.expectedRetainedFocus && evidence.fixtureOnlyProgrammaticFocus===true && evidence.productActivation===0 && evidence.nativeJourneyAcceptance===false,'product control authority differs');
 const receipt=evidence.receipt;
 requireValue(receipt.before.focused===true && receipt.after.sameButton===true && receipt.after.connected===true && receipt.after.revision===2 && receipt.after.focused===variant.expectedRetainedFocus && Number.isInteger(receipt.redundantControlMutations) && (variant.expectedRetainedFocus?receipt.redundantControlMutations===0:receipt.redundantControlMutations>0) && !receipt.sourceResult.error,'actual product focus receipt incomplete');
 const expected=variant.expectedRetainedFocus?['100-accepted-continuous-scene-updates','reorder-active-alpha','reorder-active-beta','remove-alpha','remove-beta']:[];
 requireValue(JSON.stringify(evidence.additional.map(x=>x.case))===JSON.stringify(expected),'actual product focus case set incomplete');
 return {variant:variant.name,head:variant.head,productionSourceSha256:variant.productionSource.sha256,packedSourceSha256:variant.packedSourceSha256,fixtureManifestSha256:variant.distManifest.sha256,packageSha256:variant.package.sha256,retainedPackageSha256:variant.retainedPackage.sha256,sourceManifestSha256:variant.sourceManifest.sha256,artifactId:variant.artifactId,evidence};
}
try {
 const candidate=c.variants[1], loader=createRequire(join(resolve(candidate.dist,'..'),'package.json'));
 const {chromium}=loader('playwright-core');
 const packagePath=loader.resolve('playwright-core/package.json');
 const browsersPath=join(packagePath,'../browsers.json');
 const descriptor=JSON.parse(readFileSync(browsersPath)).browsers.find(x=>x.name==='chromium');
 executable={path:chromium.executablePath(),sha256:await bounded(hashFile(chromium.executablePath()),workDeadline,'browser executable identity deadline exhausted')};
 runtime={node:process.version,nodeExecutableSha256:await bounded(hashFile(process.execPath),workDeadline,'Node identity deadline exhausted'),playwrightVersion:JSON.parse(readFileSync(packagePath)).version,playwrightPackageSha256:hash(readFileSync(packagePath)),browserDescriptor:descriptor,platform:process.platform,architecture:process.arch,kernel:os.release(),imageOS:process.env.ImageOS??null,imageVersion:process.env.ImageVersion??null,runnerName:process.env.RUNNER_NAME??null};
 const controlBytes=readFileSync(c.control.path);requireValue(hash(controlBytes)===c.control.sha256,'actual comparison control differs');
 const {producerFocusCompositionControl}=await import(pathToFileURL(c.control.path));
 requireValue(hash(readFileSync(c.mutationControl.path))===c.mutationControl.sha256,'actual mutation control differs');
 const {producerMutationCompositionControl,MUTATION_CASES}=await import(pathToFileURL(c.mutationControl.path));
 const manifests=new Map();
 for(const variant of c.variants){
  const bytes=readFileSync(variant.distManifest.path);requireValue(hash(bytes)===variant.distManifest.sha256,'selected fixture manifest changed');const manifest=JSON.parse(bytes);
  requireValue(manifest.artifactId==='sha256:'+hash(JSON.stringify({files:manifest.files,packages:manifest.packages,sourceSha256:manifest.sourceSha256,workloadSha256:manifest.workloadSha256})),'fixture artifactId binding differs');
  fixtureEntries.push({variant:variant.name,...fixtureEntryProof(variant.dist,manifest,variant.expectedRetainedFocus)});
  manifests.set(variant.name,new Map(manifest.files.map(x=>[x.path,x])));
 }
 server=createServer((request,response)=>{
  try{
   const path=decodeURIComponent(new URL(request.url,'http://127.0.0.1').pathname).slice(1)||PRODUCT_FIXTURE_ENTRY;const member=selected && manifests.get(selected.name).get(path);
   if(request.method!=='GET'||!member){response.writeHead(404);response.end();return;}
   const body=readFileSync(join(selected.dist,path));requireValue(body.length===member.bytes && 'sha256:'+hash(body)===member.sha256,'served fixture bytes differ');
   requireValue(served.length<1024,'fixture request count exceeded');served.push({variant:selected.name,path,bytes:body.length,sha256:hash(body)});
   response.setHeader('Content-Type',path.endsWith('.html')?'text/html':path.endsWith('.js')?'text/javascript':path.endsWith('.css')?'text/css':'application/octet-stream');response.end(body);
  }catch(error){if(first===null)first='fixture server: '+error.message;response.writeHead(500);response.end();}
 });
 await bounded(new Promise((done,reject)=>{server.once('error',reject);server.listen(0,'127.0.0.1',done);}),workDeadline,'fixture listener deadline exhausted');
 const origin='http://127.0.0.1:'+server.address().port;
 cleanup.browser='launch-pending';
 launchPromise=chromium.launch({headless:true,executablePath:executable.path,timeout:Math.min(15000,Math.max(1,workDeadline-performance.now()))});
 launchPromise.then(value=>{browser=value;browserVersion=value.version();cleanup.browser='open';value.once('disconnected',()=>{browserDisconnected=true;});},()=>{cleanup.browser='launch-rejected';});
 await bounded(launchPromise,workDeadline,'browser launch deadline exhausted');
 const completed=[];
 for(const variant of c.variants){
  selected=variant;cleanup.context='creation-pending';pendingContext=browser.newContext();
  pendingContext.then(value=>{context=value;cleanup.context='open';},()=>{cleanup.context='creation-rejected';});
  await bounded(pendingContext,workDeadline,'context deadline exhausted');pendingContext=null;
  await context.route('**/*',route=>{const url=new URL(route.request().url());if(url.origin===origin)return route.continue();if(first===null)first='unexpected external fixture request: '+url.origin;return route.abort();});
  const page=await bounded(context.newPage(),workDeadline,'page deadline exhausted');
  const diagnostic=(kind,text)=>{if(pageDiagnostics.length<32)pageDiagnostics.push({variant:variant.name,kind,text:String(text).slice(0,1024)});};
  page.on('pageerror',error=>diagnostic('pageerror',error.message));page.on('console',message=>{if(message.type()==='error')diagnostic('console-error',message.text());});
  page.setDefaultTimeout(Math.min(5000,Math.max(1,workDeadline-performance.now())));
  const response=await bounded(page.goto(origin+'/'+PRODUCT_FIXTURE_ENTRY),workDeadline,'fixture navigation deadline exhausted');
  requireValue(response?.ok()===true,'actual BrowserFixture entrypoint response refused');
  await bounded(page.waitForFunction(()=>window.svgFoundation!==undefined),workDeadline,'actual fixture initialization deadline exhausted');
  const evidence=await bounded(producerFocusCompositionControl(page,variant.expectedRetainedFocus),workDeadline,'actual product comparison deadline exhausted');
  requireValue(first===null,'earlier fixture failure: '+first);completed.push(bindEvidence(evidence,variant));
  if(variant.name==='candidate'){
   mutationEvidence=await bounded(producerMutationCompositionControl(page),workDeadline,'actual mutation comparison deadline exhausted');
   requireValue(mutationEvidence.scope==='actual-packed-SvgBrowserHost-MutationObserver'&&mutationEvidence.nativeJourneyAcceptance===false&&mutationEvidence.fixtureOnlySetupFocus===true&&mutationEvidence.fixtureSelectionClicks===1&&JSON.stringify(mutationEvidence.cases.map(x=>x.case))===JSON.stringify(MUTATION_CASES)&&MUTATION_CASES.length===7,'actual mutation case receipts incomplete');
  }
  await bounded(context.close(),workDeadline,'variant context close deadline exhausted');cleanup.context='closed';context=null;
 }
 requireValue(mutationEvidence!==null && completed.length===2 && completed.reduce((n,x)=>n+1+x.evidence.additional.length,0)===7,'seven actual case receipts incomplete');results=completed;
}catch(error){if(first===null)first=error.name+': '+String(error.message).slice(0,4096);}
finally{
 const closeDeadline=Math.min(finalDeadline,performance.now()+10000);
 async function close(name,action){try{await bounded(action(),closeDeadline,'original '+name+' close reserve exhausted',false);cleanup[name]='closed';}catch(error){cleanup[name]='unknown';if(first===null)first='cleanup '+name+': '+String(error.message).slice(0,4096);}}
 // Cancellation has already refused workload; close still gets its original finite reserve.
 if(launchPromise && !browser && cleanup.browser==='launch-pending'){
  const outcome=await settlePendingCreation(launchPromise,()=>cleanup.browser,closeDeadline,bounded);if(outcome){if(outcome.unknown)cleanup.browser='unknown-pending-launch';if(first===null)first='cleanup launch: '+outcome.cause;}
 }
 if(pendingContext && !context){
  const outcome=await settlePendingCreation(pendingContext,()=>cleanup.context,closeDeadline,bounded);if(outcome){if(outcome.unknown)cleanup.context='unknown-pending-context';if(first===null)first='cleanup context creation: '+outcome.cause;}
 }
 if(context)await close('context',()=>context.close());
 if(browser)await close('browser',()=>browser.close());
 if(server)await close('server',()=>new Promise((done,reject)=>server.close(error=>error?reject(error):done())));
 if(browser && !browserDisconnected && first===null)first='ordinary browser disconnect not observed after close';
 const value={schema:'fsgg.svg-browser.product-focus-comparison/v1',firstCause:first,inputsSha256:hash(inputBytes),runId:c.runId,runAttempt:c.runAttempt,candidateHead:c.candidateHead,baselineHead:c.baselineHead,runtime,browserExecutable:executable,browserVersion,results:first===null?results:null,completedCases:first===null?7:null,completedMutationCases:first===null?7:null,mutationEvidence:first===null?mutationEvidence:null,partialCoverage:first===null?'complete':'unknown; failed/incomplete product result is never pass',served,fixtureEntries,pageDiagnostics,cleanup,browserDisconnected,cancellations,elapsedMs:performance.now()-start,bounds:{workMs:60000,closeReserveMs:10000},custody:'ordinary hosted Playwright/browser/context/server close contract only; no exact private group/kernel-backing or whole-tree proof',nativeJourneyAcceptance:false};
 const body=JSON.stringify(value,null,2)+'\n';requireValue(Buffer.byteLength(body)<=1048576,'comparison result exceeds retained evidence budget');const population=readdirSync(dirname(outputPath));requireValue(population.length<128 && population.reduce((sum,name)=>sum+statSync(join(dirname(outputPath),name)).size,0)+Buffer.byteLength(body)<=4194304-65536,'retained result budget exhausted; no oversized proof promoted');writeFileSync(outputPath,body,{flag:'wx'});
}
if(first!==null)process.exitCode=1;
