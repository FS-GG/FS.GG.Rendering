import {settlePendingCreation} from './focus-comparison-lifecycle.mjs';
const names=[];
const requireValue=v=>{if(!v)throw new Error('control predicate failed');};
async function wait(promise,deadline,cause,allowCancel){requireValue(allowCancel===false);let timer;try{return await Promise.race([promise,new Promise((_,reject)=>{timer=setTimeout(()=>reject(new Error(cause)),Math.max(0,deadline-performance.now()));})]);}finally{clearTimeout(timer);}}
async function check(name,action){await action();names.push(name);}
await check('late-launch-settles-in-original-close-reserve',async()=>{let status='launch-pending',closed=false;const promise=new Promise(resolve=>setTimeout(()=>{status='open';resolve({close:async()=>{closed=true;}});},1));const result=await settlePendingCreation(promise,()=>status,performance.now()+100,wait);requireValue(result===null && status==='open');await (await promise).close();requireValue(closed);});
await check('unresolved-launch-is-unknown',async()=>{const result=await settlePendingCreation(new Promise(()=>{}),()=> 'launch-pending',performance.now()+2,wait);requireValue(result.unknown && result.cause.includes('original reserve'));});
await check('rejected-launch-is-refused-not-pending',async()=>{const result=await settlePendingCreation(Promise.reject(new Error('original launch refused')),()=> 'launch-rejected',performance.now()+100,wait);requireValue(!result.unknown && result.cause==='original launch refused');});
await check('late-context-settles-before-owner-close',async()=>{let status='creation-pending';const promise=Promise.resolve().then(()=>{status='open';return {};});const result=await settlePendingCreation(promise,()=>status,performance.now()+100,wait);requireValue(result===null && status==='open');});
await check('unresolved-context-is-unknown',async()=>{const result=await settlePendingCreation(new Promise(()=>{}),()=> 'creation-pending',performance.now()+2,wait);requireValue(result.unknown);});
await check('expired-original-reserve-refuses',async()=>{const result=await settlePendingCreation(new Promise(()=>{}),()=> 'launch-pending',performance.now()-1,wait);requireValue(result.unknown);});
console.log(JSON.stringify({complete:true,names,count:names.length,nativeJourneyAcceptance:false}));
