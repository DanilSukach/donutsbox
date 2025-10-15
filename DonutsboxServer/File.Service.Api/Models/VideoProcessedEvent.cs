namespace File.Service.Api.Models;

public record VideoProcessedEvent(Guid VideoId, string OutputPath);
