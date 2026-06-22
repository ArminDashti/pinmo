const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('pinmoConfig', {
  apiBaseUrl: 'http://127.0.0.1:5199/api',
  setLaunchAtStartup: (enabled) => ipcRenderer.invoke('app:setLaunchAtStartup', enabled)
});
