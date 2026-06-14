import { api, formatDate, renderEndpointRow, showToast } from './api.js';

let refreshTimer = null;

export function initDashboard() {
  document.getElementById('refresh-dashboard').addEventListener('click', loadDashboard);
  loadDashboard();
  refreshTimer = setInterval(loadDashboard, 10000);
}

export function stopDashboardRefresh() {
  if (refreshTimer) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
}

async function loadDashboard() {
  const statsEl = document.getElementById('dashboard-stats');
  const listEl = document.getElementById('dashboard-endpoints');

  try {
    const summary = await api.getDashboard();

    statsEl.innerHTML = `
      <div class="stat-card"><div class="label">Total</div><div class="value">${summary.totalEndpoints}</div></div>
      <div class="stat-card"><div class="label">Enabled</div><div class="value">${summary.enabledEndpoints}</div></div>
      <div class="stat-card up"><div class="label">Up</div><div class="value">${summary.upCount}</div></div>
      <div class="stat-card down"><div class="label">Down</div><div class="value">${summary.downCount}</div></div>
      <div class="stat-card unknown"><div class="label">Unknown</div><div class="value">${summary.unknownCount}</div></div>
      <div class="stat-card"><div class="label">Avg response</div><div class="value">${summary.averageResponseTimeMs}<span style="font-size:0.9rem"> ms</span></div></div>
    `;

    if (!summary.endpoints.length) {
      listEl.innerHTML = '<div class="empty-state">No endpoints yet. Add one from the Add Endpoints page.</div>';
      return;
    }

    listEl.innerHTML = summary.endpoints.map((endpoint) =>
      renderEndpointRow(endpoint, `
        <button class="btn btn-secondary btn-sm" data-ping-id="${endpoint.id}">Ping now</button>
      `)
    ).join('');

    listEl.querySelectorAll('[data-ping-id]').forEach((button) => {
      button.addEventListener('click', async () => {
        try {
          await api.pingEndpoint(button.dataset.pingId);
          showToast('Ping completed');
          await loadDashboard();
        } catch (error) {
          showToast(error.message, 'error');
        }
      });
    });
  } catch (error) {
    statsEl.innerHTML = '';
    listEl.innerHTML = `<div class="empty-state">Failed to load dashboard: ${error.message}</div>`;
  }
}

export { loadDashboard };
