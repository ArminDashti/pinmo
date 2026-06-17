import { api, escapeHtml, showToast } from './api.js';

let refreshTimer = null;
let loadRequestId = 0;

export function initDashboard() {
  document.getElementById('refresh-dashboard').addEventListener('click', loadDashboard);
  document.getElementById('reset-dashboard').addEventListener('click', resetDashboard);
  startDashboardRefresh();
  loadDashboard();
}

export function startDashboardRefresh() {
  stopDashboardRefresh();
  refreshTimer = setInterval(loadDashboard, 1000);
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
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
}

function formatPing(value) {
  return value == null ? '—' : `${value} ms`;
}

function formatPacketLoss(value) {
  return value == null ? '—' : `${value}%`;
}

function renderDashboard(summary) {
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
  const requestId = ++loadRequestId;
  const tableEl = document.getElementById('dashboard-endpoints');

  try {
    const summary = await api.getDashboard();
    if (requestId !== loadRequestId) {
      return;
    }

    renderDashboard(summary);
  } catch (error) {
    if (requestId !== loadRequestId) {
      return;
    }

    tableEl.innerHTML = `<div class="empty-state">Failed to load dashboard: ${escapeHtml(error.message)}</div>`;
  }
}

export { loadDashboard };
