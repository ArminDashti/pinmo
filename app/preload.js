const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('pinmoConfig', {
  apiBaseUrl: 'http://127.0.0.1:5199/api',
  quitApp: () => ipcRenderer.invoke('app:quit'),
  setCloseWindowAction: (action) => ipcRenderer.invoke('app:setCloseWindowAction', action)
});
