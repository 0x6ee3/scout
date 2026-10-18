const dialogs = new WeakMap();
export function sync(element, open, receiver) {
    let state = dialogs.get(element);
    if (!state) {
        state = { receiver, opener: null, dirty: false };
        state.input = () => { state.dirty = true; };
        state.keydown = event => {
            if (event.key !== "Tab") return;
            const controls = [...element.querySelectorAll('a[href],button,input,select,textarea,[tabindex]')]
                .filter(control => !control.matches(':disabled') && control.tabIndex >= 0 && control.getClientRects().length);
            const first = controls[0], last = controls.at(-1);
            if (!first) { event.preventDefault(); return; }
            if (event.shiftKey && (document.activeElement === first || document.activeElement === element)) {
                event.preventDefault(); last.focus();
            } else if (!event.shiftKey && (document.activeElement === last || document.activeElement === element)) {
                event.preventDefault(); first.focus();
            }
        };
        state.cancel = event => {
            event.preventDefault();
            if (element.dataset.busy !== "true") state.receiver.invokeMethodAsync("RequestClose", state.dirty).catch(() => {});
        };
        element.addEventListener("input", state.input);
        element.addEventListener("keydown", state.keydown);
        element.addEventListener("cancel", state.cancel);
        dialogs.set(element, state);
    }
    if (element.dataset.completed === "true") state.dirty = false;
    if (open && !element.open) {
        state.opener = document.activeElement;
        state.dirty = false;
        element.showModal();
    } else if (!open && element.open) {
        element.close();
        if (state.opener?.isConnected) state.opener.focus({ preventScroll: true });
    }
}
export function dispose(element) {
    const state = dialogs.get(element);
    if (!state) return;
    if (element.open) element.close();
    element.removeEventListener("input", state.input);
    element.removeEventListener("keydown", state.keydown);
    element.removeEventListener("cancel", state.cancel);
    if (state.opener?.isConnected) state.opener.focus({ preventScroll: true });
    dialogs.delete(element);
}
export function requestClose(element) {
    const state = dialogs.get(element);
    if (state && element.dataset.busy !== "true") state.receiver.invokeMethodAsync("RequestClose", state.dirty).catch(() => {});
}
export function isDirty(element) { return dialogs.get(element)?.dirty ?? false; }
