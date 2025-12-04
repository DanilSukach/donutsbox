namespace Donutsbox.Api.Services.Kafka;

public record AudioUploadedEvent(
    Guid AudioId,
    string ObjectKey
);

