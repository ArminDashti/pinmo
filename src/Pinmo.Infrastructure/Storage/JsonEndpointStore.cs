using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Storage;

public sealed class JsonEndpointStore(string filePath) : IEndpointStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<MonitoredEndpoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            return document.Endpoints
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
            var document = await ReadDocumentAsync(cancellationToken);
            return document.Endpoints.FirstOrDefault(e => e.Id == id);
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
            var document = await ReadDocumentAsync(cancellationToken);
            document.Endpoints.Add(endpoint);
            await WriteDocumentAsync(document, cancellationToken);
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
            var document = await ReadDocumentAsync(cancellationToken);
            var index = document.Endpoints.FindIndex(e => e.Id == endpoint.Id);
            if (index < 0)
            {
                return null;
            }

            document.Endpoints[index] = endpoint;
            await WriteDocumentAsync(document, cancellationToken);
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
            var document = await ReadDocumentAsync(cancellationToken);
            var removed = document.Endpoints.RemoveAll(e => e.Id == id);
            if (removed == 0)
            {
                return false;
            }

            await WriteDocumentAsync(document, cancellationToken);
            return true;
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
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            var endpoint = document.Endpoints.FirstOrDefault(e => e.Id == id);
            if (endpoint is null)
            {
                return;
            }

            endpoint.LastCheckedAt = checkedAt;
            endpoint.LastIsSuccess = isSuccess;
            endpoint.LastStatusCode = statusCode;
            endpoint.LastResponseTimeMs = responseTimeMs;
            endpoint.LastErrorMessage = errorMessage;

            await WriteDocumentAsync(document, cancellationToken);
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
            var document = await ReadDocumentAsync(cancellationToken);
            foreach (var endpoint in document.Endpoints)
            {
                endpoint.LastCheckedAt = null;
                endpoint.LastIsSuccess = null;
                endpoint.LastStatusCode = null;
                endpoint.LastResponseTimeMs = null;
                endpoint.LastErrorMessage = null;
            }

            await WriteDocumentAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<MonitoredEndpoint> endpoints, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await WriteDocumentAsync(new EndpointDocument(endpoints.ToList()), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool FileExists() => File.Exists(filePath);

    private Task<EndpointDocument> ReadDocumentAsync(CancellationToken cancellationToken) =>
        JsonFileHelper.ReadAsync(filePath, new EndpointDocument(), cancellationToken);

    private Task WriteDocumentAsync(EndpointDocument document, CancellationToken cancellationToken) =>
        JsonFileHelper.WriteAsync(filePath, document, cancellationToken);

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
