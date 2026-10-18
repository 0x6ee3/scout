import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const source=await readFile(new URL('../../../src/Scout.Web/wwwroot/js/ui-dialog.js',import.meta.url),'utf8');
const {sync,dispose,requestClose,isDirty}=await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
function fixture(){
 const calls=[];let focused=0;
 const opener={isConnected:true,focus(){focused++;}};
 globalThis.document={activeElement:opener};
 const handlers=new Map();
 const dialog={open:false,dataset:{busy:'false',completed:'false'},showModal(){this.open=true;},close(){this.open=false;},addEventListener(k,v){handlers.set(k,v);},removeEventListener(k){handlers.delete(k);}};
 const receiver={invokeMethodAsync(method,dirty){calls.push({method,dirty});return Promise.resolve();}};
 return {dialog,calls,opener,receiver,handlers,focused:()=>focused};
}
test('Escape requests discard review for edited inputs without closing the native dialog',()=>{
 const f=fixture();sync(f.dialog,true,f.receiver);f.handlers.get('input')();let prevented=false;
 f.handlers.get('cancel')({preventDefault(){prevented=true;}});
 assert.equal(prevented,true);assert.equal(f.dialog.open,true);assert.deepEqual(f.calls,[{method:'RequestClose',dirty:true}]);
});
test('busy operations cannot be dismissed with Escape or the close button',()=>{
 const f=fixture();sync(f.dialog,true,f.receiver);f.dialog.dataset.busy='true';
 f.handlers.get('cancel')({preventDefault(){}});requestClose(f.dialog);
 assert.equal(f.calls.length,0);assert.equal(f.dialog.open,true);
});
test('closing returns focus, reopening clears dirty state and confirmed success is clean',()=>{
 const f=fixture();sync(f.dialog,true,f.receiver);f.handlers.get('input')();assert.equal(isDirty(f.dialog),true);
 sync(f.dialog,false,f.receiver);assert.equal(f.focused(),1);
 sync(f.dialog,true,f.receiver);assert.equal(isDirty(f.dialog),false);
 f.handlers.get('input')();f.dialog.dataset.completed='true';sync(f.dialog,true,f.receiver);assert.equal(isDirty(f.dialog),false);
});
test('disposal removes handlers without attempting to focus a removed invoker',()=>{
 const f=fixture();sync(f.dialog,true,f.receiver);f.opener.isConnected=false;dispose(f.dialog);
 assert.equal(f.dialog.open,false);assert.equal(f.handlers.size,0);assert.equal(f.focused(),0);
});
test('Tab and Shift+Tab wrap at the visible enabled control boundaries',()=>{
 const f=fixture();sync(f.dialog,true,f.receiver);
 const control=()=>({disabled:false,tabIndex:0,matches(){return this.disabled;},getClientRects:()=>[{}],focus(){document.activeElement=this;}});
 const first=control(),last=control(),hidden=control(),disabled=control();hidden.getClientRects=()=>[];disabled.disabled=true;
 f.dialog.querySelectorAll=()=>[first,hidden,disabled,last];let prevented=0;
 document.activeElement=last;f.handlers.get('keydown')({key:'Tab',shiftKey:false,preventDefault(){prevented++;}});
 assert.equal(document.activeElement,first);
 f.handlers.get('keydown')({key:'Tab',shiftKey:true,preventDefault(){prevented++;}});
 assert.equal(document.activeElement,last);assert.equal(prevented,2);
});
