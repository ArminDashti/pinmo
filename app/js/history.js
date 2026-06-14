import { api, escapeHtml, formatDate, showToast, statusClass, statusLabel } from './api.js';

let currentPage = 1;

export function initHistory() {
  document.getElementById('refresh-history').addEventListener('click', () => {
    currentPage = 1;
    loadHistory();
  });

  document.getElementById('purge-history').addEventListener('click', async () => {
    try {
      const result = await api.purgeHistory();
      showToast(`Removed ${result.deletedCount} old records`);
      await loadHistory();
    } catch (error) {
      showToast(error.message, 'error');
    }
  });

  document.getElementById('history-endpoint-filter').addEventListener('change', () => {
    currentPage = 1;
    loadHistory();
  });

  document.getElementById('history-status-filter').addEventListener('change', () => {
    currentPage = 1;
    loadHistory();
  });

  loadEndpointFilter();
  loadHistory();
}

async function loadEndpointFilter() {
  const select = document.getElementById('history-endpoint-filter');

  try {
    const endpoints = await api.getEndpoints();
    select.innerHTML = '<option value="">All endpoints</option>' +
      endpoints.map((endpoint) =>
        `<option value="${endpoint.id}">${escapeHtml(endpoint.name)}</option>`
      ).join('');
  } catch {
    // keep default option
  }
}

async function loadHistory() {
  const tableEl = document.getElementById('history-table');
  const paginationEl = document.getElementById('history-pagination');
  const endpointId = document.getElementById('history-endpoint-filter').value;
  const statusFilter = document.getElementById('history-status-filter').value;

  const params = { page: currentPage, pageSize: 25 };
  if (endpointId) params.endpointId = endpointId;
  if (statusFilter !== '') params.successOnly = statusFilter;

  try {
    const data = await api.getHistory(params);

    if (!data.records.length) {
      tableEl.innerHTML = '<div class="empty-state">No ping history yet.</div>';
      paginationEl.innerHTML = '';
      return;
    }

    tableEl.innerHTML = `
      <table>
        <thead>
          <tr>
            <th>Time</th>
            <th>Endpoint</th>
            <th>Status</th>
            <th>Code</th>
            <th>Response</th>
            <th>Error</th>
          </tr>
        </thead>
        <tbody>
          ${data.records.map((record) => `
            <tr>
              <td>${formatDate(record.checkedAt)}</td>
              <td>
                <strong>${escapeHtml(record.endpointName)}</strong><br>
                <span style="color:var(--text-muted);font-size:0.8rem">${escapeHtml(record.endpointUrl)}</span>
              </td>
              <td><span class="status-pill ${statusClass(record.isSuccess)}">${statusLabel(record.isSuccess)}</span></td>
              <td>${record.statusCode ?? '—'}</td>
              <td>${record.responseTimeMs} ms</td>
              <td>${escapeHtml(record.errorMessage ?? '—')}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    paginationEl.innerHTML = `
      <span>Page ${data.page} of ${Math.max(data.totalPages, 1)} · ${data.totalCount} records</span>
      <div>
        <button class="btn btn-secondary btn-sm" id="history-prev" ${data.page <= 1 ? 'disabled' : ''}>Previous</button>
        <button class="btn btn-secondary btn-sm" id="history-next" ${data.page >= data.totalPages ? 'disabled' : ''}>Next</button>
      </div>
    `;

    const prev = document.getElementById('history-prev');
    const next = document.getElementById('history-next');

    prev?.addEventListener('click', () => {
      currentPage -= 1;
      loadHistory();
    });

    next?.addEventListener('click', () => {
      currentPage += 1;
      loadHistory();
    });
  } catch (error) {
    tableEl.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
    paginationEl.innerHTML = '';
  }
}

export { loadHistory, loadEndpointFilter };
