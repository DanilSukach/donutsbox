namespace File.Service.Api.Models;

public record AudioUploadedEvent(
    Guid AudioId,
    string ObjectKey
);

