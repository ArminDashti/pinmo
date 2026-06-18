import { api, escapeHtml, showToast } from './api.js';

let refreshTimer = null;
let loadRequestId = 0;
let refreshInFlight = false;
let dashboardAbortController = null;

function isDashboardPageActive() {
  return document.getElementById('page-dashboard')?.classList.contains('active') ?? false;
}

function cancelDashboardLoad() {
  loadRequestId += 1;
  if (dashboardAbortController) {
    dashboardAbortController.abort();
    dashboardAbortController = null;
  }
}

export function initDashboard() {
  document.getElementById('refresh-dashboard').addEventListener('click', loadDashboard);
  document.getElementById('reset-dashboard').addEventListener('click', resetDashboard);
  startDashboardRefresh();
  loadDashboard();
}

export function startDashboardRefresh() {
  stopDashboardRefresh();
  scheduleDashboardRefresh();
}

function scheduleDashboardRefresh() {
  refreshTimer = setTimeout(() => {
    if (!isDashboardPageActive()) {
      return;
    }

    if (!refreshInFlight) {
      refreshInFlight = true;
      void loadDashboard().finally(() => {
        refreshInFlight = false;
      });
    }

    if (isDashboardPageActive()) {
      scheduleDashboardRefresh();
    }
  }, 1000);
}

async function resetDashboard() {
  const resetButton = document.getElementById('reset-dashboard');
  resetButton.disabled = true;
  stopDashboardRefresh();

  try {
    const summary = await api.resetDashboard();
    if (summary) {
      renderDashboard(summary);
    } else {
      await loadDashboard();
    }
    showToast('Dashboard stats reset');
  } catch (error) {
    showToast(`Failed to reset dashboard: ${error.message}`, 'error');
    await loadDashboard();
  } finally {
    resetButton.disabled = false;
    startDashboardRefresh();
  }
}

export function stopDashboardRefresh() {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
    refreshTimer = null;
  }

  cancelDashboardLoad();
}

function formatPing(value) {
  return value == null ? '—' : `${value} ms`;
}

function formatPacketLoss(value) {
  return value == null ? '—' : `${value}%`;
}

function renderDashboard(summary) {
  if (!isDashboardPageActive()) {
    return;
  }

  const tableEl = document.getElementById('dashboard-endpoints');
  const endpoints = Array.isArray(summary?.endpoints) ? summary.endpoints : [];

  if (!endpoints.length) {
    tableEl.innerHTML = '<div class="empty-state">No endpoints yet. Add one from the Add Endpoints page.</div>';
    return;
  }

  tableEl.innerHTML = `
    <table class="data-table">
      <thead>
        <tr>
          <th>Endpoints</th>
          <th>Latest ping</th>
          <th>Avg ping</th>
          <th>Avg packet loss</th>
        </tr>
      </thead>
      <tbody>
        ${endpoints.map((endpoint) => `
          <tr>
            <td>${escapeHtml(endpoint.url)}</td>
            <td>${formatPing(endpoint.latestPingMs)}</td>
            <td>${formatPing(endpoint.avgPingMs)}</td>
            <td>${formatPacketLoss(endpoint.avgPacketLossPercent)}</td>
          </tr>
        `).join('')}
      </tbody>
    </table>
  `;
}

async function loadDashboard() {
  if (!isDashboardPageActive()) {
    return;
  }

  const requestId = ++loadRequestId;
  if (dashboardAbortController) {
    dashboardAbortController.abort();
  }

  const abortController = new AbortController();
  dashboardAbortController = abortController;
  const tableEl = document.getElementById('dashboard-endpoints');

  try {
    const summary = await api.getDashboard({ signal: abortController.signal });
    if (requestId !== loadRequestId || !isDashboardPageActive()) {
      return;
    }

    renderDashboard(summary);
  } catch (error) {
    if (error.name === 'AbortError') {
      return;
    }

    if (requestId !== loadRequestId || !isDashboardPageActive()) {
      return;
    }

    tableEl.innerHTML = `<div class="empty-state">Failed to load dashboard: ${escapeHtml(error.message)}</div>`;
  } finally {
    if (dashboardAbortController === abortController) {
      dashboardAbortController = null;
    }
  }
}

export { loadDashboard };
