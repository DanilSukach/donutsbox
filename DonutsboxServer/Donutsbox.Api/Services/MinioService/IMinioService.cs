namespace Donutsbox.Api.Services.MinioService;

public interface IMinioService
{
    Task EnsureBucketAsync();
    Task UploadFileAsync(string objectKey, Stream stream, string contentType);
}
