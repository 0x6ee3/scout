// Native input owns editor values. No input must wait for the Blazor connection.
const editors = new Map();
const tabId = crypto.randomUUID();
let database;
let reconnectReload = false;
function openDatabase() {
    if (!database) database = new Promise((resolve, reject) => {
        const request = indexedDB.open('scout-workspace-drafts', 1);
        request.onupgradeneeded = () => {
            const store = request.result.createObjectStore('drafts', { keyPath: 'key' });
            store.createIndex('target', ['workspace', 'round', 'configuration', 'target']);
            store.createIndex('workspace', ['workspace', 'round']);
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new Error('Draft database unavailable'));
    });
    return database;
}
async function transaction(mode, action) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction('drafts', mode);
        let result;
        tx.oncomplete = () => resolve(result?.result);
        tx.onerror = tx.onabort = () => reject(tx.error || new Error('Draft transaction failed'));
        try { result = action(tx.objectStore('drafts')); } catch (error) { tx.abort(); reject(error); }
    });
}
function saveRecord(record) { return transaction('readwrite', store => store.put(record)); }
async function discard(record) {
    await transaction('readwrite', store => {
        const read = store.get(record.key);
        read.onsuccess = () => { if (read.result?.version === record.version) store.delete(record.key); };
        return read;
    });
}
function queryTarget(config) { return transaction('readonly', store => store.index('target').getAll(IDBKeyRange.only([config.workspace, config.round, config.configuration, config.target]))); }
export function samePayload(a, b) { return a.text === b.text && a.asked === b.asked && a.follow === b.follow && a.creationToken === b.creationToken; }
function payload(state) {
    return { text: state.config.textEnabled ? state.element.querySelector('[data-field=text]')?.value ?? '' : null,
        asked: state.element.querySelector('[data-field=asked]')?.checked ?? false,
        follow: state.element.querySelector('[data-field=follow]')?.checked ?? false,
        creationToken: state.config.creation ? state.creationToken : null };
}
function display(state, value) {
    const text = state.element.querySelector('[data-field=text]'); if (text) text.value = value.text ?? '';
    const asked = state.element.querySelector('[data-field=asked]'); if (asked) asked.checked = value.asked;
    const follow = state.element.querySelector('[data-field=follow]'); if (follow) follow.checked = value.follow;
}
function feedback(state, value) { state.status.textContent = value; }
function record(state) {
    return { schema: 1, key: [state.config.workspace, state.config.round, state.config.configuration, state.config.target, tabId].join('/'),
        workspace: state.config.workspace, round: state.config.round, configuration: state.config.configuration, target: state.config.target, tabId,
        version: state.version, baseRevision: state.revision, basePayload: structuredClone(state.base), payload: payload(state), updatedAt: new Date().toISOString() };
}
function persist(state) {
    const captured = record(state);
    state.pendingWrite = state.pendingWrite.catch(() => {}).then(() => saveRecord(captured));
    return state.pendingWrite.then(() => {
        state.storageAvailable = true;
        state.localStoredVersion = captured.version;
        if (!state.disposed && captured.version === state.version && !state.busy && !state.paused) feedback(state, 'Draft stored in this browser · Unsaved');
    }).catch(() => {
        state.storageAvailable = false;
        if (!state.disposed) feedback(state, 'Draft recovery unavailable — save or copy now');
    });
}
function changed(state) {
    if (state.disposed || state.blocked) return;
    reconnectReload = false; state.version++; state.dirty = true;
    persist(state);
    if (state.config.creation || state.paused) return;
    clearTimeout(state.idleTimer); state.idleTimer = setTimeout(() => flush(state), 750);
    if (!state.maximumTimer) state.maximumTimer = setTimeout(() => flush(state), 3000);
}
function button(label, action) { const b = document.createElement('button'); b.type = 'button'; b.textContent = label; b.addEventListener('click', action); return b; }
function copyText(text, container) {
    const field = document.createElement('textarea'); field.value = text; field.readOnly = true; field.setAttribute('aria-label', 'Copyable draft');
    container.append(field); field.focus(); field.select();
    const link = document.createElement('a'); link.textContent = 'Download draft text'; link.download = 'scout-unsaved-draft.txt';
    const url = URL.createObjectURL(new Blob([text], { type: 'text/plain' })); link.href = url; container.append(link);
    link.addEventListener('click', () => setTimeout(() => URL.revokeObjectURL(url), 60000), { once: true });
}
function copyDraft(state, value = payload(state)) {
    copyText(value.text ?? JSON.stringify(value), state.recovery);
}
function resetTimers(state) { clearTimeout(state.idleTimer); clearTimeout(state.maximumTimer); state.idleTimer = state.maximumTimer = null; }
async function showRecovery(state, candidates, conflict = false) {
    state.paused = true; resetTimers(state); state.recovery.replaceChildren();
    feedback(state, conflict ? 'Conflict — review the saved version and your draft' : 'A browser draft is available — review before restoring');
    const draft = candidates[0];
    const saved = document.createElement('pre'); saved.className = 'plain-text'; saved.textContent = 'Saved version:\n' + (state.base.text ?? JSON.stringify(state.base));
    const preview = document.createElement('pre'); preview.className = 'plain-text'; preview.textContent = 'Browser draft (' + draft.updatedAt + '):\n' + (draft.payload.text ?? JSON.stringify(draft.payload));
    state.recovery.append(saved, preview);
    const next = async () => {
        try { await discard(draft); } catch { feedback(state, 'Draft cleanup unavailable. The retained draft may be offered again.'); }
        if (candidates.length > 1) { await showRecovery(state, candidates.slice(1)); return; }
        state.paused = false; state.recovery.replaceChildren();
    };
    state.recovery.append(button('Restore draft', async () => {
        // An explicit choice adopts the reviewed server revision. Subsequent server changes still conflict.
        const desired = conflict ? payload(state) : draft.payload;
        if (state.config.creation) state.creationToken = desired.creationToken || crypto.randomUUID();
        display(state, desired); state.version++; state.dirty = true; await persist(state);
        // Restore is a decision for this candidate only; do not overwrite drafts from other tabs.
        await next();
        if (!state.paused && !state.config.creation) await flush(state);
        else if (!state.paused) feedback(state, state.storageAvailable ? "Draft stored in this browser · Unsaved" : "Draft recovery unavailable — save or copy now");
    }), button('Keep saved version', async () => {
        const own = record(state);
        display(state, state.base); state.dirty = false; state.version++;
        await state.pendingWrite.catch(() => {});
        try { await discard(own); } catch { }
        await next(); if (!state.paused) feedback(state, 'Saved to Scout');
    }), button('Copy draft', () => copyDraft(state, conflict ? payload(state) : draft.payload)));
}
export async function flush(state) {
    resetTimers(state);
    if (state.disposed || state.blocked || state.busy || state.paused || !state.dirty) return;
    if (state.config.creation && !payload(state).text?.trim()) { feedback(state, "Enter question wording before adding it."); return; }
    state.busy = true;
    if (state.config.creation) state.element.querySelector('fieldset').disabled = true;
    const captured = record(state); feedback(state, 'Saving');
    try {
        const result = await state.dotnet.invokeMethodAsync('SaveDraft', state.revision, captured.payload);
        if (state.disposed) return;
        if (result.reviewRequired) {
            state.blocked = true;
            // Stop editing revoked fields while keeping local copy/export controls usable.
            state.element.querySelector('fieldset').disabled = false;
            state.element.querySelectorAll('[data-field]').forEach(field => { if (field.tagName === 'TEXTAREA') field.readOnly = true; else field.disabled = true; });
            state.element.querySelector('[data-action=save]').disabled = true;
            feedback(state, 'Unsaved — private Workspace review required. Copy your draft before closing.'); return;
        }
        if (result.conflict) {
            state.revision = result.revision;
            state.base = { text: state.config.textEnabled ? result.text : null, asked: result.isAsked, follow: result.followUpRequired, creationToken: null };
            await showRecovery(state, [record(state)], true); return;
        }
        if (!result.saved) { feedback(state, 'Unsaved — save failed. Your draft is retained; retry or copy it.'); return; }
        state.revision = result.revision; state.base = captured.payload;
        // Wait for local writes before deleting precisely the version acknowledged by Scout.
        await state.pendingWrite.catch(() => {});
        try { await discard(captured); } catch { /* A retained draft is safer than losing it. */ }
        if (state.version === captured.version) {
            state.dirty = false;
            feedback(state, 'Saved to Scout');
            if (state.config.creation) {
                state.creationToken = crypto.randomUUID(); state.base = { text: '', asked: false, follow: false, creationToken: state.creationToken };
                display(state, state.base); await state.dotnet.invokeMethodAsync('Created');
            }
        } else {
            // New typing survives the acknowledgment and is rebased for the next conditional write.
            await persist(state); feedback(state, 'Draft stored in this browser · Unsaved');
        }
    } catch { if (!state.disposed) feedback(state, 'Unsaved/offline — draft retained. Save or copy when available.'); }
    finally {
        state.busy = false;
        if (state.config.creation && !state.disposed && !state.blocked) state.element.querySelector('fieldset').disabled = false;
        if (state.dirty && !state.paused && !state.blocked && !state.config.creation && state.status.textContent.includes('Draft stored')) state.idleTimer = setTimeout(() => flush(state), 750);
    }
}
export async function registerEditor(element, dotnet, config) {
    if (editors.has(element)) return;
    const state = { element, dotnet, config, status: element.querySelector('[data-save-status]'), recovery: element.querySelector('[data-recovery]'), revision: config.revision,
        version: 0, dirty: false, busy: false, paused: false, blocked: false, disposed: false, pendingWrite: Promise.resolve(), storageAvailable: true, creationToken: crypto.randomUUID() };
    state.base = payload(state); editors.set(element, state);
    if (config.readOnly) { feedback(state, 'Read only'); return; }
    state.input = () => changed(state); state.blur = () => { if (!config.creation) flush(state); };
    state.save = () => flush(state); state.copy = () => copyDraft(state);
    element.addEventListener('input', state.input); element.addEventListener('change', state.input); element.addEventListener('focusout', state.blur);
    element.querySelector('[data-action=save]').addEventListener('click', state.save);
    element.querySelector('[data-action=copy]').addEventListener('click', state.copy);
    try {
        const candidates = (await queryTarget(config)).filter(x => x.schema === 1).sort((a,b) => b.updatedAt.localeCompare(a.updatedAt));
        const pending = [];
        for (const draft of candidates) {
            if (!config.creation && samePayload(draft.payload, state.base)) await discard(draft); else pending.push(draft);
        }
        if (state.disposed) return;
        if (pending.length) await showRecovery(state, pending); else feedback(state, config.creation ? 'Question remains local until Add question.' : 'Saved to Scout');
    } catch { state.storageAvailable = false; feedback(state, 'Draft recovery unavailable — save or copy now'); }
    if (!state.disposed) element.querySelector('fieldset').disabled = false;
}
export function disposeEditor(element) {
    const state = editors.get(element); if (!state) return;
    state.disposed = true; resetTimers(state);
    element.removeEventListener('input', state.input); element.removeEventListener('change', state.input); element.removeEventListener('focusout', state.blur);
    element.querySelector('[data-action=save]')?.removeEventListener('click', state.save); element.querySelector('[data-action=copy]')?.removeEventListener('click', state.copy); editors.delete(element);
}
let workspace;
export function registerWorkspace(dotnet, workspaceId) {
    disposeWorkspace();
    const cleanups = [];
    const search = document.querySelector('[data-workspace-search]');
    const filter = document.querySelector('[data-workspace-state]');
    const preset = document.querySelector('[data-workspace-preset]');
    const grid = document.querySelector('.workspace-grid');
    const applyFilter = () => {
        const query = (search?.value ?? '').trim().toLocaleLowerCase();
        document.querySelectorAll('[data-filterable]').forEach(article => {
            const text = article.textContent + ' ' + [...article.querySelectorAll('textarea')].map(x => x.value).join(' ');
            const kind = article.dataset.filterable;
            const asked = article.querySelector('[data-field=asked]')?.checked;
            const follow = article.querySelector('[data-field=follow]')?.checked;
            const answer = article.querySelector('[data-field=text]')?.value;
            const mode = filter?.value ?? 'all';
            const stateMatch = kind !== 'question' || mode === 'all' || (mode === 'asked' && asked) || (mode === 'unasked' && !asked) || (mode === 'follow' && follow) || (mode === 'unanswered' && answer !== undefined && !answer.trim());
            article.hidden = !stateMatch || !text.toLocaleLowerCase().includes(query);
        });
    };
    const listen = (target,event,handler) => { target?.addEventListener(event,handler);cleanups.push(() => target?.removeEventListener(event,handler)); };
    listen(search,'input',applyFilter);listen(filter,'change',applyFilter);listen(grid,'change',applyFilter);
    const preferencesKey = 'scout-workspace-layout/' + workspaceId;
    let preferences = {};
    try { preferences = JSON.parse(localStorage.getItem(preferencesKey) || '{}'); } catch { }
    const storePreferences = () => { try { localStorage.setItem(preferencesKey,JSON.stringify(preferences)); } catch { } };
    if(preset) preset.value = preferences.preset === 'notes-wide' ? 'notes-wide' : 'balanced';
    const applyPreset = () => { grid?.classList.toggle('notes-wide',preset?.value === 'notes-wide');preferences.preset=preset?.value;storePreferences(); };
    listen(preset,'change',applyPreset);applyPreset();
    document.querySelectorAll('[data-workspace-panel]').forEach((panel,index) => {
        if(preferences.panels?.[index] !== undefined) panel.open=Boolean(preferences.panels[index]);
        listen(panel,'toggle',() => { preferences.panels ??= {};preferences.panels[index]=panel.open;storePreferences(); });
    });
    const pending = () => [...editors.values()].some(s => s.dirty);
    const unload = event => { if (pending() && !reconnectReload) { event.preventDefault(); event.returnValue = ''; } };
    const key = event => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') { event.preventDefault(); editors.forEach(state => { if(!state.config.creation) flush(state); }); } };
    const check = async () => {
        try { if (!await dotnet.invokeMethodAsync('CheckCurrent')) { editors.forEach(state => { state.blocked = true; state.element.hidden = true; }); } }
        catch { editors.forEach(state => { if (state.dirty) feedback(state, 'Unsaved/offline — browser draft retained'); }); }
    };
    const navigate = event => { if (event.target.closest('a[href]') && pending() && !confirm('Some changes are not saved to Scout. Leave and retain browser drafts?')) event.preventDefault(); };
    window.addEventListener('beforeunload', unload); window.addEventListener('keydown', key); window.addEventListener('focus', check); document.addEventListener('click', navigate, true);
    workspace = { unload, key, check, navigate, cleanups, timer: setInterval(check, 15000) };
}
export function disposeWorkspace() {
    if (!workspace) return;
    workspace.cleanups.forEach(cleanup => cleanup());
    clearInterval(workspace.timer); window.removeEventListener('beforeunload', workspace.unload); window.removeEventListener('keydown', workspace.key); window.removeEventListener('focus', workspace.check); document.removeEventListener('click', workspace.navigate, true); workspace = null;
}
export async function privateRecovery(element, workspaceId, round) {
    element.replaceChildren();
    try {
        const records = await transaction('readonly', store => store.index('workspace').getAll(IDBKeyRange.only([workspaceId, round])));
        if (!records.length) { element.textContent = 'No browser drafts for this Workspace.'; return; }
        for (const draft of records) {
            const section = document.createElement('section'); section.className = 'card';
            const heading = document.createElement('h3'); heading.textContent = draft.target + ' · review ' + draft.configuration + ' · ' + draft.updatedAt;
            const content = document.createElement('pre'); content.className = 'plain-text'; content.textContent = draft.payload.text ?? JSON.stringify(draft.payload);
            section.append(heading, content, button('Copy draft', () => copyText(content.textContent, section)), button('Discard this draft', async () => { await discard(draft); section.remove(); }));element.append(section);
        }
    } catch { element.textContent = 'Browser draft recovery unavailable. Use the original tab to copy any unsaved text.'; }
}

export async function prepareReconnectReload() {
    const states = [...editors.values()].filter(state => state.dirty);
    await Promise.all(states.map(state => state.pendingWrite.catch(() => {})));
    reconnectReload = states.every(state => state.storageAvailable && state.localStoredVersion === state.version);
    return reconnectReload;
}
