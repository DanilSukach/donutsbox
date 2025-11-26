namespace Donutsbox.Api.Services.MinioService;

public interface IMinioService
{
    Task EnsureBucketAsync();
    Task UploadFileAsync(string objectKey, Stream stream, string contentType);
    Task<byte[]> GetProcessedObjectBytesAsync(string objectKey, CancellationToken ct = default);
    Task UploadImageAsync(string objectKey, Stream stream, string contentType);
    Task<string> GetPresignedGetUrlAsync(string objectKey, string bucket, int expiresInSeconds = 300);
    Task DeleteObjectAsync(string objectKey, string bucket);
    Task DeleteDirectoryAsync(string prefix, string bucket);
    Task CopyObjectAsync(string sourceObjectKey, string sourceBucket, string destObjectKey, string destBucket);
    string GetTempBucket();
    string GetProcessedBucket();
    string GetImagesBucket();
}
