// Inert actual compiled-byte coupling; no BrowserFixture/product JavaScript is executed.
import {fixtureEntryProof} from './focus-comparison-entrypoint.mjs';
import {createHash} from 'node:crypto';
import {readFileSync,writeFileSync,mkdirSync,mkdtempSync,rmSync} from 'node:fs';
import {join} from 'node:path';
import os from 'node:os';
const [originalDist,originalManifest,candidateDist,candidateManifest]=process.argv.slice(2);
if(!originalDist||!originalManifest||!candidateDist||!candidateManifest)throw new Error('literal actual compiled input pairs required');
const hash=body=>createHash('sha256').update(body).digest('hex');
const original=JSON.parse(readFileSync(originalManifest)),candidate=JSON.parse(readFileSync(candidateManifest));
const requireValue=value=>{if(!value)throw new Error('entrypoint control predicate failed');};
const names=[];
function check(name,action){action();names.push(name);}
function refused(action,cause){try{action();}catch(error){requireValue(error.message.includes(cause));return;}throw new Error('expected entrypoint refusal absent');}
for(const [dist,manifest] of [[originalDist,original],[candidateDist,candidate]])for(const item of manifest.files){const body=readFileSync(join(dist,item.path));requireValue(body.length===item.bytes && 'sha256:'+hash(body)===item.sha256);}
check('actual-original-compiled-index-Program-API',()=>{const value=fixtureEntryProof(originalDist,original);requireValue(value.entrypoint==='index.html' && value.requiredApi.length===3);});
check('actual-candidate-compiled-index-advanced-API',()=>{const value=fixtureEntryProof(candidateDist,candidate,true);requireValue(value.requiredApi.includes('advanceFocusControl'));});
check('original-failed-suite-route-refusal',()=>refused(()=>fixtureEntryProof(originalDist,original,false,'suite.html'),'not the product BrowserFixture'));
check('missing-index-manifest-refusal',()=>refused(()=>fixtureEntryProof(originalDist,{...original,files:original.files.filter(x=>x.path!=='index.html')}),'manifest member absent'));
check('duplicate-index-manifest-refusal',()=>refused(()=>fixtureEntryProof(originalDist,{...original,files:[...original.files,original.files.find(x=>x.path==='index.html')]}),'manifest member absent or ambiguous'));
check('wrong-index-bytes-refusal',()=>refused(()=>fixtureEntryProof(originalDist,{...original,files:original.files.map(x=>x.path==='index.html'?{...x,sha256:'sha256:'+('0'.repeat(64))}:x)}),'bytes differ'));
const valid=fixtureEntryProof(originalDist,original);
check('missing-Program-module-refusal',()=>refused(()=>fixtureEntryProof(originalDist,{...original,files:original.files.filter(x=>x.path!==valid.programModule.path)}),'manifest member absent'));
check('wrong-Program-module-bytes-refusal',()=>refused(()=>fixtureEntryProof(originalDist,{...original,files:original.files.map(x=>x.path===valid.programModule.path?{...x,bytes:x.bytes+1}:x)}),'bytes differ'));
check('original-without-candidate-API-refusal',()=>refused(()=>fixtureEntryProof(originalDist,original,true),'Program API bootstrap absent'));
const tmp=mkdtempSync(join(os.tmpdir(),'actual-entrypoint-controls-'));
try{
 function mutated(name,htmlTransform,programTransform){const root=join(tmp,name);mkdirSync(root);mkdirSync(join(root,'assets'));const html=Buffer.from(htmlTransform(readFileSync(join(originalDist,'index.html'),'utf8'))),program=Buffer.from(programTransform(readFileSync(join(originalDist,valid.programModule.path),'utf8')));writeFileSync(join(root,'index.html'),html);writeFileSync(join(root,valid.programModule.path),program);return [root,{...original,files:[{path:'index.html',bytes:html.length,sha256:'sha256:'+hash(html)},{path:valid.programModule.path,bytes:program.length,sha256:'sha256:'+hash(program)}]}];}
 const identity=value=>value;
 check('performance-HTML-is-not-fixture-refusal',()=>{const [root,manifest]=mutated('performance',()=>readFileSync(join(originalDist,'suite.html'),'utf8'),identity);refused(()=>fixtureEntryProof(root,manifest),'fixture container absent');});
 check('fixture-container-missing-refusal',()=>{const [root,manifest]=mutated('container',value=>value.replace('id="fixture"','id="other"'),identity);refused(()=>fixtureEntryProof(root,manifest),'fixture container absent');});
 check('compiled-global-API-bootstrap-missing-refusal',()=>{const [root,manifest]=mutated('global',identity,value=>value.replace('window.svgFoundation=','window.otherAPI='));refused(()=>fixtureEntryProof(root,manifest),'Program API bootstrap absent');});
 for(const api of ['mount','state','replaceCurrent'])check('compiled-'+api+'-API-missing-refusal',()=>{const [root,manifest]=mutated('missing-'+api,identity,value=>value.replaceAll(api+':','otherAPI:'));refused(()=>fixtureEntryProof(root,manifest),'Program API bootstrap absent');});
 check('ambiguous-product-module-refusal',()=>{const [root,manifest]=mutated('ambiguous',value=>value+'<script type="module" src="/'+valid.programModule.path+'"></script>',identity);refused(()=>fixtureEntryProof(root,manifest),'module entry absent/ambiguous');});
 check('external-product-module-refusal',()=>{const [root,manifest]=mutated('external',value=>value.replace('src="/assets/','src="https://external.invalid/'),identity);refused(()=>fixtureEntryProof(root,manifest),'not a declared dist asset');});
}finally{rmSync(tmp,{recursive:true});}
console.log(JSON.stringify({complete:true,names,count:names.length,nativeJourneyAcceptance:false}));
