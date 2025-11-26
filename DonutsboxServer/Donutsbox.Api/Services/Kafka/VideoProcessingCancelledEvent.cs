namespace Donutsbox.Api.Services.Kafka;

public record VideoProcessingCancelledEvent(
    Guid VideoId,
    string Reason
);

