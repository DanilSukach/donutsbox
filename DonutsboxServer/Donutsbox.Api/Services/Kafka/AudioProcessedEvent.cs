namespace Donutsbox.Api.Services.Kafka;

public record AudioProcessedEvent(Guid AudioId, string OutputPath);

