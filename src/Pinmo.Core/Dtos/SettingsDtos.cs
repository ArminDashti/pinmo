namespace Pinmo.Core.Dtos;

public record SettingsResponse(
    int RequestTimeoutSeconds,
    string CloseWindowAction);

public record SettingsUpdateRequest(string CloseWindowAction);
