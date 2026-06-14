const { contextBridge } = require('electron');

contextBridge.exposeInMainWorld('pinmoConfig', {
  apiBaseUrl: 'http://127.0.0.1:5199/api'
});
