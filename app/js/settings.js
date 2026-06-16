import { api, showToast } from './api.js';

export function initSettings() {
  document.getElementById('settings-form').addEventListener('submit', handleSubmit);
  loadSettings();
}

async function loadSettings() {
  try {
    const settings = await api.getSettings();
    document.getElementById('settings-default-interval').value = settings.defaultIntervalSeconds;
    document.getElementById('settings-default-packets').value = settings.defaultPacketsPerPing;
  } catch (error) {
    showToast(`Failed to load settings: ${error.message}`, 'error');
  }
}

async function handleSubmit(event) {
  event.preventDefault();

  const payload = {
    defaultIntervalSeconds: Number(document.getElementById('settings-default-interval').value),
    defaultPacketsPerPing: Number(document.getElementById('settings-default-packets').value)
  };

  try {
    await api.updateSettings(payload);
    showToast('Settings saved');
  } catch (error) {
    showToast(error.message, 'error');
  }
}

export { loadSettings };
