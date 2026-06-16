import { api, showToast } from './api.js';
import { initDashboard, loadDashboard, stopDashboardRefresh } from './dashboard.js';
import { initEndpoints, loadManagedEndpoints } from './endpoints.js';
import { initSettings, loadSettings } from './settings.js';

const pages = {
  dashboard: {
    init: initDashboard,
    onShow: loadDashboard,
    onHide: stopDashboardRefresh
  },
  endpoints: {
    init: initEndpoints,
    onShow: loadManagedEndpoints
  },
  settings: {
    init: initSettings,
    onShow: loadSettings
  }
};

let currentPage = 'dashboard';
const initialized = new Set();

function navigateTo(pageName) {
  if (!pages[pageName]) return;

  pages[currentPage]?.onHide?.();

  document.querySelectorAll('.page').forEach((page) => page.classList.remove('active'));
  document.querySelectorAll('.nav-link').forEach((link) => link.classList.remove('active'));

  document.getElementById(`page-${pageName}`).classList.add('active');
  document.querySelector(`[data-page="${pageName}"]`).classList.add('active');

  if (!initialized.has(pageName)) {
    pages[pageName].init();
    initialized.add(pageName);
  }

  currentPage = pageName;
  pages[pageName].onShow?.();
}

async function updateConnectionStatus() {
  const statusEl = document.getElementById('connection-status');

  try {
    await api.health();
    statusEl.textContent = 'API connected';
    statusEl.className = 'status-pill status-up';
  } catch {
    statusEl.textContent = 'API offline';
    statusEl.className = 'status-pill status-down';
  }
}

document.querySelectorAll('.nav-link').forEach((link) => {
  link.addEventListener('click', () => navigateTo(link.dataset.page));
});

navigateTo('dashboard');
updateConnectionStatus();
setInterval(updateConnectionStatus, 15000);
