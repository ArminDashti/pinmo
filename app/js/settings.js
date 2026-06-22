import { api, showToast } from './api.js';

export function initSettings() {
  const form = document.getElementById('settings-form');

  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const launchAtStartup = form.elements.launchAtStartup.checked;

    try {
      await api.updateSettings({ launchAtStartup });
      await window.pinmoConfig?.setLaunchAtStartup?.(launchAtStartup);
      showToast('Settings saved');
    } catch (error) {
      showToast(error.message, 'error');
    }
  });
}

export async function loadSettings() {
  const form = document.getElementById('settings-form');

  try {
    const settings = await api.getSettings();
    form.elements.launchAtStartup.checked = Boolean(settings.launchAtStartup);
  } catch (error) {
    showToast(error.message, 'error');
  }
}
