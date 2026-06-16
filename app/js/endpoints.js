import { api, escapeHtml, showToast } from './api.js';

let editingEndpointId = null;

function normalizeEndpointAddress(input) {
  let value = input.trim();

  if (!value) {
    return { ok: false, message: 'URL or IP address is required.' };
  }

  if (value.toLowerCase().startsWith('http://')) {
    value = value.slice('http://'.length);
  } else if (value.toLowerCase().startsWith('https://')) {
    value = value.slice('https://'.length);
  }

  value = value.replace(/\/+$/, '').trim();

  if (!value) {
    return { ok: false, message: 'URL or IP address is required.' };
  }

  const slashIndex = value.indexOf('/');
  const authority = slashIndex >= 0 ? value.slice(0, slashIndex) : value;
  const portSeparator = authority.lastIndexOf(':');
  let host = authority;

  if (portSeparator > 0 && authority.indexOf(':') === portSeparator) {
    host = authority.slice(0, portSeparator);
    const port = Number(authority.slice(portSeparator + 1));

    if (!Number.isInteger(port) || port < 1 || port > 65535) {
      return {
        ok: false,
        message: 'Enter a hostname, path, or IP address without http:// or https:// (e.g. example.com or 192.168.1.1).'
      };
    }
  }

  const isIpv4 = /^(?:\d{1,3}\.){3}\d{1,3}$/.test(host) && host.split('.').every((part) => {
    const octet = Number(part);
    return Number.isInteger(octet) && octet >= 0 && octet <= 255;
  });
  const isHostname = /^(?=.{1,253}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$/.test(host);

  if (!isIpv4 && !isHostname) {
    return {
      ok: false,
      message: 'Enter a hostname, path, or IP address without http:// or https:// (e.g. example.com or 192.168.1.1).'
    };
  }

  return { ok: true, value };
}

export function initEndpoints() {
  const form = document.getElementById('endpoint-form');
  const cancelButton = document.getElementById('endpoint-cancel');

  form.addEventListener('submit', handleSubmit);
  cancelButton.addEventListener('click', resetForm);

  loadManagedEndpoints();
}

async function handleSubmit(event) {
  event.preventDefault();

  const normalized = normalizeEndpointAddress(document.getElementById('endpoint-url').value);

  if (!normalized.ok) {
    showToast(normalized.message, 'error');
    return;
  }

  const payload = {
    url: normalized.value
  };

  try {
    if (editingEndpointId) {
      await api.updateEndpoint(editingEndpointId, payload);
      showToast('Endpoint updated');
    } else {
      await api.createEndpoint(payload);
      showToast('Endpoint added');
    }

    resetForm();
    await loadManagedEndpoints();
  } catch (error) {
    showToast(error.message, 'error');
  }
}

function resetForm() {
  editingEndpointId = null;
  document.getElementById('endpoint-form').reset();
  document.getElementById('endpoint-submit').textContent = 'Add endpoint';
  document.getElementById('endpoint-cancel').hidden = true;
}

async function loadManagedEndpoints() {
  const container = document.getElementById('managed-endpoints');

  try {
    const endpoints = await api.getEndpoints();

    if (!endpoints.length) {
      container.innerHTML = '<div class="empty-state">No endpoints registered yet.</div>';
      return;
    }

    container.innerHTML = `
      <table class="data-table">
        <thead>
          <tr>
            <th>Endpoints</th>
            <th>Edit</th>
            <th>Delete</th>
          </tr>
        </thead>
        <tbody>
          ${endpoints.map((endpoint) => `
            <tr>
              <td>${escapeHtml(endpoint.url)}</td>
              <td>
                <button class="btn btn-secondary btn-sm" data-edit-id="${endpoint.id}">Edit</button>
              </td>
              <td>
                <button class="btn btn-danger btn-sm" data-delete-id="${endpoint.id}">Delete</button>
              </td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    container.querySelectorAll('[data-edit-id]').forEach((button) => {
      button.addEventListener('click', () => startEdit(endpoints.find((e) => e.id === button.dataset.editId)));
    });

    container.querySelectorAll('[data-delete-id]').forEach((button) => {
      button.addEventListener('click', async () => {
        if (!confirm('Delete this endpoint?')) return;
        try {
          await api.deleteEndpoint(button.dataset.deleteId);
          showToast('Endpoint deleted');
          await loadManagedEndpoints();
        } catch (error) {
          showToast(error.message, 'error');
        }
      });
    });
  } catch (error) {
    container.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
  }
}

function startEdit(endpoint) {
  editingEndpointId = endpoint.id;

  let displayUrl = endpoint.url;
  if (displayUrl.toLowerCase().startsWith('http://')) {
    displayUrl = displayUrl.slice('http://'.length);
  } else if (displayUrl.toLowerCase().startsWith('https://')) {
    displayUrl = displayUrl.slice('https://'.length);
  }

  document.getElementById('endpoint-url').value = displayUrl;
  document.getElementById('endpoint-submit').textContent = 'Save changes';
  document.getElementById('endpoint-cancel').hidden = false;
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

export { loadManagedEndpoints };
