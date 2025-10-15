namespace File.Service.Api.Models;

public record VideoUploadedEvent(Guid VideoId, string ObjectKey);
