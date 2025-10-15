namespace Donutsbox.Api.Services.Kafka;

public record VideoProcessedEvent(Guid VideoId, string OutputPath);
