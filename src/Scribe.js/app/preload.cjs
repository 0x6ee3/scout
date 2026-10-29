const { contextBridge, ipcRenderer } = require('electron');
contextBridge.exposeInMainWorld('scribe', {
  state: () => ipcRenderer.invoke('scribe:state'),
  action: action => ipcRenderer.invoke('scribe:action', action),
  hide: () => ipcRenderer.invoke('scribe:hide'),
  preferences: () => ipcRenderer.invoke('scribe:preferences'),
  browse: () => ipcRenderer.invoke('scribe:browse'),
  save: value => ipcRenderer.invoke('scribe:save', value),
  onState: callback => { const handler = (_event, value) => callback(value); ipcRenderer.on('scribe:state', handler); return () => ipcRenderer.removeListener('scribe:state', handler); },
  onSettingsOpen: callback => { const handler = () => callback(); ipcRenderer.on('scribe:settings-open', handler); return () => ipcRenderer.removeListener('scribe:settings-open', handler); }
});
