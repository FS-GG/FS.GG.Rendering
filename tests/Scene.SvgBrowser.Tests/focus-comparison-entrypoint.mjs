// Source/manifest coupling only; real browser API initialization and seven cases still required.
import {createHash} from 'node:crypto';
import {readFileSync} from 'node:fs';
import {join} from 'node:path';
export const PRODUCT_FIXTURE_ENTRY='index.html';
const requireValue=(value,cause)=>{if(!value)throw new Error(cause);};
const hash=body=>createHash('sha256').update(body).digest('hex');
export function fixtureEntryProof(dist,manifest,advanced=false,entry=PRODUCT_FIXTURE_ENTRY) {
 requireValue(entry===PRODUCT_FIXTURE_ENTRY,'selected route is not the product BrowserFixture entrypoint');
 function member(path){const selected=manifest.files.filter(x=>x.path===path);requireValue(selected.length===1,'entrypoint/module manifest member absent or ambiguous');const value=selected[0];const body=readFileSync(join(dist,path));requireValue(body.length===value.bytes && 'sha256:'+hash(body)===value.sha256,'entrypoint/module bytes differ from actual fixture manifest');return {value,body};}
 const html=member(entry);const text=html.body.toString('utf8');
 requireValue(/<div\b[^>]*\bid=["']fixture["']/.test(text),'product fixture container absent');
 const scripts=[...text.matchAll(/<script\b[^>]*\btype=["']module["'][^>]*\bsrc=["']([^"']+)["'][^>]*>/g)];
 requireValue(scripts.length===1,'actual product module entry absent/ambiguous');
 const path=scripts[0][1];requireValue(path.startsWith('/assets/') && path.endsWith('.js') && !path.includes('..') && !path.includes('?') && !path.includes('#'),'product module path is not a declared dist asset');
 const program=member(path.slice(1));const source=program.body.toString('utf8');
 const requiredApi=advanced?['mount','state','replaceCurrent','advanceFocusControl','advanceMutationControl']:['mount','state','replaceCurrent'];
 requireValue(/window\.svgFoundation\s*=/.test(source) && requiredApi.every(name=>new RegExp('\\b'+name+'\\s*:').test(source)),'compiled Program API bootstrap absent');
 return {entrypoint:entry,html:html.value,programModule:program.value,requiredApi,sourceCouplingOnly:true};
}
