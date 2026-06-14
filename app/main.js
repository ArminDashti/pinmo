const { app, BrowserWindow, shell } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
const http = require('http');

const API_PORT = 5199;
const API_BASE = `http://127.0.0.1:${API_PORT}`;
let apiProcess = null;
let mainWindow = null;

function resolveApiProjectPath() {
  return path.join(__dirname, '..', 'src', 'Pinmo.Api', 'Pinmo.Api.csproj');
}

function startApiServer() {
  return new Promise((resolve, reject) => {
    const projectPath = resolveApiProjectPath();
    apiProcess = spawn('dotnet', ['run', '--project', projectPath], {
      cwd: path.join(__dirname, '..'),
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' },
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true
    });

    apiProcess.stdout.on('data', (data) => {
      if (process.argv.includes('--dev')) {
        console.log(`[API] ${data.toString().trim()}`);
      }
    });

    apiProcess.stderr.on('data', (data) => {
      if (process.argv.includes('--dev')) {
        console.error(`[API] ${data.toString().trim()}`);
      }
    });

    apiProcess.on('error', reject);

    waitForApiReady()
      .then(resolve)
      .catch(reject);
  });
}

function waitForApiReady(maxAttempts = 60) {
  return new Promise((resolve, reject) => {
    let attempts = 0;

    const check = () => {
      attempts += 1;
      const req = http.get(`${API_BASE}/api/health`, (res) => {
        res.resume();
        if (res.statusCode === 200) {
          resolve();
        } else if (attempts >= maxAttempts) {
          reject(new Error('API health check failed'));
        } else {
          setTimeout(check, 500);
        }
      });

      req.on('error', () => {
        if (attempts >= maxAttempts) {
          reject(new Error('API did not start in time'));
        } else {
          setTimeout(check, 500);
        }
      });

      req.setTimeout(1000, () => {
        req.destroy();
      });
    };

    check();
  });
}

function stopApiServer() {
  if (apiProcess) {
    apiProcess.kill();
    apiProcess = null;
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    title: 'Pinmo',
    backgroundColor: '#0f1419',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'index.html'));

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: 'deny' };
  });

  if (process.argv.includes('--dev')) {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }
}

app.whenReady().then(async () => {
  try {
    await startApiServer();
    createWindow();
  } catch (error) {
    console.error('Failed to start Pinmo:', error.message);
    app.quit();
  }

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  stopApiServer();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  stopApiServer();
});

process.on('exit', stopApiServer);
