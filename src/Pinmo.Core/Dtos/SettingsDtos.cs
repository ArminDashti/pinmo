namespace Pinmo.Core.Dtos;

public record SettingsResponse(
    int RequestTimeoutSeconds,
    bool LaunchAtStartup);

public record SettingsUpdateRequest(bool LaunchAtStartup);
