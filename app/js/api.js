const API_BASE = window.pinmoConfig?.apiBaseUrl ?? 'http://127.0.0.1:5199/api';

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers ?? {})
    },
    ...options
  });

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      message = body.message ?? message;
    } catch {
      // ignore parse errors
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export const api = {
  getDashboard: () => request('/dashboard'),
  getEndpoints: () => request('/endpoints'),
  createEndpoint: (payload) => request('/endpoints', { method: 'POST', body: JSON.stringify(payload) }),
  updateEndpoint: (id, payload) => request(`/endpoints/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteEndpoint: (id) => request(`/endpoints/${id}`, { method: 'DELETE' }),
  pingEndpoint: (id) => request(`/endpoints/${id}/ping`, { method: 'POST' }),
  getHistory: (params) => {
    const query = new URLSearchParams(params).toString();
    return request(`/history?${query}`);
  },
  purgeHistory: () => request('/history', { method: 'DELETE' }),
  getSettings: () => request('/settings'),
  updateSettings: (payload) => request('/settings', { method: 'PUT', body: JSON.stringify(payload) }),
  health: () => request('/health')
};

export function showToast(message, type = 'success') {
  const container = document.getElementById('toast-container');
  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.textContent = message;
  container.appendChild(toast);
  setTimeout(() => toast.remove(), 3500);
}

export function formatDate(value) {
  if (!value) return '—';
  return new Date(value).toLocaleString();
}

export function statusClass(isSuccess) {
  if (isSuccess === true) return 'status-up';
  if (isSuccess === false) return 'status-down';
  return 'status-unknown';
}

export function statusLabel(isSuccess) {
  if (isSuccess === true) return 'Up';
  if (isSuccess === false) return 'Down';
  return 'Unknown';
}

export function renderEndpointRow(endpoint, actionsHtml = '') {
  return `
    <article class="endpoint-row">
      <div class="endpoint-info">
        <h4>${escapeHtml(endpoint.name)}</h4>
        <p>${escapeHtml(endpoint.url)} · ${escapeHtml(endpoint.httpMethod)} · every ${endpoint.intervalSeconds}s · ${endpoint.packetsPerPing ?? 2} packets</p>
      </div>
      <div class="endpoint-meta">
        <span class="status-pill ${statusClass(endpoint.lastIsSuccess)}">${statusLabel(endpoint.lastIsSuccess)}</span>
        <span class="meta-chip">${endpoint.lastStatusCode ?? '—'} · ${endpoint.lastResponseTimeMs ?? '—'} ms</span>
        <span class="meta-chip">${formatDate(endpoint.lastCheckedAt)}</span>
        ${actionsHtml}
      </div>
    </article>
  `;
}

export function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}
