import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const source = await readFile(new URL('../../../src/Scout.Web/wwwroot/js/workspace-drafts.js', import.meta.url), 'utf8');
const records = new Map();
globalThis.IDBKeyRange = { only: value => value };
globalThis.indexedDB = { open() {
    const request = {};
    queueMicrotask(() => { request.result = { transaction() {
        const tx = { objectStore() { return {
            get(key) { const r = { result: records.get(key) }; queueMicrotask(() => r.onsuccess?.()); return r; },
            put(record) { records.set(record.key, structuredClone(record)); return {}; },
            delete(key) { records.delete(key); return {}; }
        }; } };
        setTimeout(() => tx.oncomplete?.(), 0); return tx;
    } }; request.onsuccess(); }); return request;
} };
const { flush } = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
function state(invoke) {
    const input = { value: 'first' }, status = { textContent: '' };
    return { input, status, element: { querySelector: selector => selector === '[data-field=text]' ? input : null },
        dotnet: { invokeMethodAsync: invoke }, config: { workspace: 'one', round: 1, configuration: 1, target: 'notes', textEnabled: true },
        version: 1, revision: 0, base: { text: '', asked: false, follow: false, creationToken: null }, pendingWrite: Promise.resolve(), dirty: true };
}
test('typing during an in-flight save survives the old acknowledgment and is conditionally saved next', async () => {
    let acknowledge, calls = [];
    const s = state((method, revision, payload) => { calls.push({ revision, payload }); return new Promise(resolve => acknowledge = resolve); });
    const first = flush(s); s.input.value = 'second'; s.version = 2;
    acknowledge({ saved: true, revision: 1 }); await first; clearTimeout(s.idleTimer);
    assert.equal(s.input.value, 'second'); assert.equal(s.dirty, true); assert.equal(s.revision, 1);
    assert.equal([...records.values()].find(r => r.workspace === 'one').payload.text, 'second');
    const second = flush(s); acknowledge({ saved: true, revision: 2 }); await second;
    assert.equal(calls[1].revision, 1); assert.equal(calls[1].payload.text, 'second'); assert.equal(s.dirty, false);
    assert.equal(records.size, 0); assert.equal(s.status.textContent, 'Saved to Scout');
});
test('a failed server save keeps the current edit and does not claim a SQLite save', async () => {
    const s = state(async () => ({ saved: false })); await flush(s);
    assert.equal(s.dirty, true); assert.equal(s.input.value, 'first'); assert.match(s.status.textContent, /^Unsaved/);
});
test('a lost connection preserves pending changes for a deliberate retry', async () => {
    const s = state(async () => { throw new Error('Circuit offline'); }); await flush(s);
    assert.equal(s.dirty, true); assert.equal(s.busy, false); assert.match(s.status.textContent, /offline/);
});
test('a second flush never creates a competing request for the same editor', async () => {
    let complete, calls = 0;
    const s = state(() => { calls++; return new Promise(resolve => complete = resolve); });
    const first = flush(s); await flush(s); assert.equal(calls, 1);
    complete({ saved: true, revision: 1 }); await first; assert.equal(s.dirty, false);
});

test('denied browser storage keeps text editable and gives honest recovery feedback', async () => {
    globalThis.indexedDB = { open() { const request = {}; queueMicrotask(() => { request.error = new Error('QuotaExceededError'); request.onerror(); }); return request; } };
    const denied = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64') + '#denied');
    const input = { value: 'Retain this text' }, status = { textContent: '' }, fieldset = { disabled: true };
    const handlers = new Map(), save = { addEventListener: (type, fn) => handlers.set('save', fn), removeEventListener() {} }, copy = { addEventListener() {}, removeEventListener() {} };
    const element = { addEventListener: (type, fn) => handlers.set(type, fn), removeEventListener() {}, querySelector(selector) {
        return ({ '[data-save-status]': status, '[data-recovery]': {}, '[data-field=text]': input, 'fieldset': fieldset, '[data-action=save]': save, '[data-action=copy]': copy })[selector] ?? null;
    } };
    await denied.registerEditor(element, { invokeMethodAsync: async () => ({ saved: false }) }, { workspace: 'one', round: 1, configuration: 2, target: 'notes', revision: 0, textEnabled: true });
    assert.equal(fieldset.disabled, false); assert.match(status.textContent, /Draft recovery unavailable/);
    handlers.get('input')(); await new Promise(resolve => setTimeout(resolve, 0));
    assert.equal(input.value, 'Retain this text'); assert.match(status.textContent, /Draft recovery unavailable/);
    await handlers.get('save')(); assert.equal(input.value, 'Retain this text'); assert.match(status.textContent, /^Unsaved/);
    assert.equal(await denied.prepareReconnectReload(), false);
    denied.disposeEditor(element);
});

test('revoked editing permission retains the draft and leaves copy controls usable', async () => {
    const s = state(async () => ({ saved: false, reviewRequired: true }));
    const fieldset = { disabled: true }, save = { disabled: false };
    s.input.tagName = 'TEXTAREA';
    s.element.querySelectorAll = () => [s.input];
    s.element.querySelector = selector => ({ 'fieldset': fieldset, '[data-action=save]': save, '[data-field=text]': s.input })[selector] ?? null;
    await flush(s);
    assert.equal(s.blocked, true); assert.equal(s.dirty, true); assert.equal(s.input.value, 'first');
    assert.equal(s.input.readOnly, true); assert.equal(fieldset.disabled, false); assert.equal(save.disabled, true);
});
