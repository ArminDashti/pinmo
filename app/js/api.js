const API_BASE = window.pinmoConfig?.apiBaseUrl ?? 'http://127.0.0.1:5199/api';

async function request(path, options = {}) {
  const { signal, headers, ...rest } = options;

  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(headers ?? {})
    },
    signal,
    ...rest
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
  getDashboard: (options = {}) => request('/dashboard', options),
  resetDashboard: () => request('/dashboard/reset', { method: 'POST' }),
  getEndpoints: () => request('/endpoints'),
  createEndpoint: (payload) => request('/endpoints', { method: 'POST', body: JSON.stringify(payload) }),
  updateEndpoint: (id, payload) => request(`/endpoints/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteEndpoint: (id) => request(`/endpoints/${id}`, { method: 'DELETE' }),
  pingEndpoint: (id) => request(`/endpoints/${id}/ping`, { method: 'POST' }),
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

export function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}
