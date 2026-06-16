import { api, escapeHtml, showToast } from './api.js';

let refreshTimer = null;

export function initDashboard() {
  document.getElementById('refresh-dashboard').addEventListener('click', loadDashboard);
  document.getElementById('reset-dashboard').addEventListener('click', resetDashboard);
  loadDashboard();
  refreshTimer = setInterval(loadDashboard, 1000);
}

async function resetDashboard() {
  const resetButton = document.getElementById('reset-dashboard');
  resetButton.disabled = true;

  try {
    await api.resetDashboard();
    showToast('Dashboard stats reset');
    await loadDashboard();
  } catch (error) {
    showToast(`Failed to reset dashboard: ${error.message}`, 'error');
  } finally {
    resetButton.disabled = false;
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

async function loadDashboard() {
  const tableEl = document.getElementById('dashboard-endpoints');

  try {
    const summary = await api.getDashboard();

    if (!summary.endpoints.length) {
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
          ${summary.endpoints.map((endpoint) => `
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
  } catch (error) {
    tableEl.innerHTML = `<div class="empty-state">Failed to load dashboard: ${escapeHtml(error.message)}</div>`;
  }
}

export { loadDashboard };
