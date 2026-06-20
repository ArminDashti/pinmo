using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Storage;

public sealed class InMemoryEndpointStore : IEndpointStore
{
    private readonly List<MonitoredEndpoint> _endpoints = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _filePath;

    public async Task LoadSeedFromFileAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;

        if (_filePath is null || !File.Exists(_filePath))
        {
            return;
        }

        var document = await JsonFileHelper.ReadAsync(
            _filePath,
            new EndpointDocument(),
            cancellationToken);

        var seeded = document.Endpoints
            .Select(ClearPingState)
            .ToList();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _endpoints.Clear();
            _endpoints.AddRange(seeded);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MonitoredEndpoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _endpoints
                .OrderBy(e => e.Url, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MonitoredEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _endpoints.FirstOrDefault(e => e.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MonitoredEndpoint> AddAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _endpoints.Add(endpoint);
            await PersistLockedAsync(cancellationToken);
            return endpoint;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MonitoredEndpoint?> UpdateAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var index = _endpoints.FindIndex(e => e.Id == endpoint.Id);
            if (index < 0)
            {
                return null;
            }

            _endpoints[index] = endpoint;
            await PersistLockedAsync(cancellationToken);
            return endpoint;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var removed = _endpoints.RemoveAll(e => e.Id == id) > 0;
            if (removed)
            {
                await PersistLockedAsync(cancellationToken);
            }

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdatePingStateAsync(
        Guid id,
        DateTime checkedAt,
        bool isSuccess,
        int? statusCode,
        int? responseTimeMs,
        string? errorMessage,
        int packetsSent,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var endpoint = _endpoints.FirstOrDefault(e => e.Id == id);
            if (endpoint is null)
            {
                return;
            }

            endpoint.LastCheckedAt = checkedAt;
            endpoint.LastIsSuccess = isSuccess;
            endpoint.LastStatusCode = statusCode;
            endpoint.LastResponseTimeMs = responseTimeMs;
            endpoint.LastErrorMessage = errorMessage;
            endpoint.LastPacketsSent = packetsSent;
            await PersistLockedAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetAllPingStateAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            foreach (var endpoint in _endpoints)
            {
                ClearPingState(endpoint);
            }

            await PersistLockedAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task PersistLockedAsync(CancellationToken cancellationToken)
    {
        if (_filePath is null)
        {
            return;
        }

        await JsonFileHelper.WriteAsync(
            _filePath,
            new EndpointDocument(_endpoints.ToList()),
            cancellationToken);
    }

    private static MonitoredEndpoint ClearPingState(MonitoredEndpoint endpoint)
    {
        endpoint.LastCheckedAt = null;
        endpoint.LastIsSuccess = null;
        endpoint.LastStatusCode = null;
        endpoint.LastResponseTimeMs = null;
        endpoint.LastErrorMessage = null;
        endpoint.LastPacketsSent = null;
        return endpoint;
    }

    private sealed class EndpointDocument
    {
        public List<MonitoredEndpoint> Endpoints { get; set; } = [];

        public EndpointDocument()
        {
        }

        public EndpointDocument(List<MonitoredEndpoint> endpoints)
        {
            Endpoints = endpoints;
        }
    }
}
