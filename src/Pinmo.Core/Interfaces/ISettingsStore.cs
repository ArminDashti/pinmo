using Pinmo.Core.Entities;

namespace Pinmo.Core.Interfaces;

public interface ISettingsStore
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
