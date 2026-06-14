import { api, showToast } from './api.js';

export function initSettings() {
  document.getElementById('settings-form').addEventListener('submit', handleSubmit);
  loadSettings();
}

async function loadSettings() {
  try {
    const settings = await api.getSettings();
    document.getElementById('settings-default-interval').value = settings.defaultIntervalSeconds;
    document.getElementById('settings-timeout').value = settings.requestTimeoutSeconds;
    document.getElementById('settings-retention').value = settings.historyRetentionDays;
    document.getElementById('settings-auto-start').checked = settings.startMonitoringOnLaunch;
    document.getElementById('settings-notify-failure').checked = settings.notifyOnFailure;
  } catch (error) {
    showToast(`Failed to load settings: ${error.message}`, 'error');
  }
}

async function handleSubmit(event) {
  event.preventDefault();

  const payload = {
    defaultIntervalSeconds: Number(document.getElementById('settings-default-interval').value),
    requestTimeoutSeconds: Number(document.getElementById('settings-timeout').value),
    historyRetentionDays: Number(document.getElementById('settings-retention').value),
    startMonitoringOnLaunch: document.getElementById('settings-auto-start').checked,
    notifyOnFailure: document.getElementById('settings-notify-failure').checked
  };

  try {
    await api.updateSettings(payload);
    showToast('Settings saved');
  } catch (error) {
    showToast(error.message, 'error');
  }
}

export { loadSettings };
