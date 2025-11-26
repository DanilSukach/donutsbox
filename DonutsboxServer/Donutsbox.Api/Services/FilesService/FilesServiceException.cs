namespace Donutsbox.Api.Services.FilesService;

public class FilesServiceException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

