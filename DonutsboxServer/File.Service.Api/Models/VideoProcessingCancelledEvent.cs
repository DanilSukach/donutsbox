namespace File.Service.Api.Models;

public record VideoProcessingCancelledEvent(Guid VideoId, string Reason);

