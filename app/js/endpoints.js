import { api, escapeHtml, formatDate, renderEndpointRow, showToast } from './api.js';

let editingEndpointId = null;

export function initEndpoints() {
  const form = document.getElementById('endpoint-form');
  const cancelButton = document.getElementById('endpoint-cancel');

  form.addEventListener('submit', handleSubmit);
  cancelButton.addEventListener('click', resetForm);

  loadManagedEndpoints();
}

async function handleSubmit(event) {
  event.preventDefault();

  const payload = {
    name: document.getElementById('endpoint-name').value.trim(),
    url: document.getElementById('endpoint-url').value.trim(),
    httpMethod: document.getElementById('endpoint-method').value,
    intervalSeconds: Number(document.getElementById('endpoint-interval').value),
    packetsPerPing: Number(document.getElementById('endpoint-packets').value),
    isEnabled: document.getElementById('endpoint-enabled').checked
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
  document.getElementById('endpoint-enabled').checked = true;
  document.getElementById('endpoint-interval').value = '60';
  document.getElementById('endpoint-packets').value = '2';
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

    container.innerHTML = endpoints.map((endpoint) =>
      renderEndpointRow(endpoint, `
        <button class="btn btn-secondary btn-sm" data-edit-id="${endpoint.id}">Edit</button>
        <button class="btn btn-secondary btn-sm" data-ping-id="${endpoint.id}">Ping</button>
        <button class="btn btn-danger btn-sm" data-delete-id="${endpoint.id}">Delete</button>
      `)
    ).join('');

    container.querySelectorAll('[data-edit-id]').forEach((button) => {
      button.addEventListener('click', () => startEdit(endpoints.find((e) => e.id === button.dataset.editId)));
    });

    container.querySelectorAll('[data-ping-id]').forEach((button) => {
      button.addEventListener('click', async () => {
        try {
          await api.pingEndpoint(button.dataset.pingId);
          showToast('Ping completed');
          await loadManagedEndpoints();
        } catch (error) {
          showToast(error.message, 'error');
        }
      });
    });

    container.querySelectorAll('[data-delete-id]').forEach((button) => {
      button.addEventListener('click', async () => {
        if (!confirm('Delete this endpoint and its history?')) return;
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
  document.getElementById('endpoint-name').value = endpoint.name;
  document.getElementById('endpoint-url').value = endpoint.url;
  document.getElementById('endpoint-method').value = endpoint.httpMethod;
  document.getElementById('endpoint-interval').value = String(endpoint.intervalSeconds);
  document.getElementById('endpoint-packets').value = String(endpoint.packetsPerPing ?? 2);
  document.getElementById('endpoint-enabled').checked = endpoint.isEnabled;
  document.getElementById('endpoint-submit').textContent = 'Save changes';
  document.getElementById('endpoint-cancel').hidden = false;
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

export { loadManagedEndpoints };
