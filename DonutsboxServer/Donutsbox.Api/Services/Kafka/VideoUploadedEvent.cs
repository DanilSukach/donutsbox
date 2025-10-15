namespace Donutsbox.Api.Services.Kafka;

public record VideoUploadedEvent(
    Guid VideoId,
    string ObjectKey
);