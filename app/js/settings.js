import { api, showToast } from './api.js';

export function initSettings() {
  const form = document.getElementById('settings-form');
  const exitButton = document.getElementById('exit-app');

  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const closeWindowAction = form.elements.closeWindowAction.value;

    try {
      await api.updateSettings({ closeWindowAction });
      window.pinmoConfig?.setCloseWindowAction?.(closeWindowAction);
      showToast('Settings saved');
    } catch (error) {
      showToast(error.message, 'error');
    }
  });

  exitButton.addEventListener('click', () => {
    window.pinmoConfig?.quitApp?.();
  });
}

export async function loadSettings() {
  const form = document.getElementById('settings-form');

  try {
    const settings = await api.getSettings();
    form.elements.closeWindowAction.value = settings.closeWindowAction;
  } catch (error) {
    showToast(error.message, 'error');
  }
}
