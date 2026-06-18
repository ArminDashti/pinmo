import { api, escapeHtml, showToast } from './api.js';

let editingEndpointId = null;
let managedLoadRequestId = 0;
let isSubmitting = false;

function isEndpointsPageActive() {
  return document.getElementById('page-endpoints')?.classList.contains('active') ?? false;
}

function isUrlInputFocused() {
  const urlInput = document.getElementById('endpoint-url');
  return urlInput !== null && document.activeElement === urlInput;
}

function focusEndpointUrlInput() {
  const focus = () => {
    const urlInput = document.getElementById('endpoint-url');
    if (!urlInput || !isEndpointsPageActive()) {
      return;
    }

    window.focus();
    urlInput.focus({ preventScroll: true });
  };

  focus();
  requestAnimationFrame(focus);
  setTimeout(focus, 0);
}

function showDeleteConfirm(message) {
  return new Promise((resolve) => {
    const backdrop = document.createElement('div');
    backdrop.className = 'confirm-backdrop';
    backdrop.innerHTML = `
      <div class="confirm-dialog" role="alertdialog" aria-modal="true">
        <p>${escapeHtml(message)}</p>
        <div class="confirm-actions">
          <button type="button" class="btn btn-secondary" data-action="cancel">Cancel</button>
          <button type="button" class="btn btn-danger" data-action="confirm">Delete</button>
        </div>
      </div>
    `;

    const finish = (confirmed) => {
      backdrop.remove();
      resolve(confirmed);
      focusEndpointUrlInput();
    };

    backdrop.addEventListener('click', (event) => {
      if (event.target === backdrop) {
        finish(false);
      }
    });

    backdrop.querySelector('[data-action="cancel"]').addEventListener('click', () => finish(false));
    backdrop.querySelector('[data-action="confirm"]').addEventListener('click', () => finish(true));

    document.body.appendChild(backdrop);
    backdrop.querySelector('[data-action="confirm"]').focus();
  });
}

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
}

async function handleSubmit(event) {
  event.preventDefault();

  if (isSubmitting) {
    return;
  }

  const normalized = normalizeEndpointAddress(document.getElementById('endpoint-url').value);

  if (!normalized.ok) {
    showToast(normalized.message, 'error');
    return;
  }

  const payload = {
    url: normalized.value
  };

  const submitButton = document.getElementById('endpoint-submit');
  isSubmitting = true;
  submitButton.disabled = true;

  try {
    if (editingEndpointId) {
      await api.updateEndpoint(editingEndpointId, payload);
      showToast('Endpoint updated');
    } else {
      await api.createEndpoint(payload);
      showToast('Endpoint added');
    }

    resetForm();
    await loadManagedEndpoints({ force: true });
  } catch (error) {
    showToast(error.message, 'error');
  } finally {
    isSubmitting = false;
    submitButton.disabled = false;
  }
}

function resetForm() {
  editingEndpointId = null;
  document.getElementById('endpoint-form').reset();
  document.getElementById('endpoint-submit').textContent = 'Add endpoint';
  document.getElementById('endpoint-cancel').hidden = true;
}

async function loadManagedEndpoints({ force = false } = {}) {
  if (!isEndpointsPageActive()) {
    return;
  }

  if (!force && isUrlInputFocused()) {
    return;
  }

  const requestId = ++managedLoadRequestId;
  const container = document.getElementById('managed-endpoints');

  try {
    const endpoints = await api.getEndpoints();

    if (requestId !== managedLoadRequestId || !isEndpointsPageActive()) {
      return;
    }

    if (!force && isUrlInputFocused()) {
      return;
    }

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
        const deletedId = button.dataset.deleteId;
        const confirmed = await showDeleteConfirm('Delete this endpoint?');

        if (!confirmed) {
          return;
        }

        try {
          focusEndpointUrlInput();
          await api.deleteEndpoint(deletedId);

          if (editingEndpointId === deletedId) {
            resetForm();
          }

          showToast('Endpoint deleted');
          await loadManagedEndpoints({ force: true });
          focusEndpointUrlInput();
        } catch (error) {
          showToast(error.message, 'error');
          focusEndpointUrlInput();
        }
      });
    });
  } catch (error) {
    if (requestId !== managedLoadRequestId || !isEndpointsPageActive()) {
      return;
    }

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
