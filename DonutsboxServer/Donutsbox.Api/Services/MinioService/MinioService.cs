using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace Donutsbox.Api.Services.MinioService;

public class MinioService(IConfiguration configuration, ILogger<MinioService> logger) :IMinioService
{
    private readonly MinioClient _client = (MinioClient)new MinioClient()
        .WithEndpoint(configuration["Minio:Endpoint"])
        .WithCredentials(configuration["Minio:AccessKey"], configuration["Minio:SecretKey"])
        .Build();

    private readonly string _tempBucket = configuration["Minio:BucketTemp"] ?? "video-temp";
    private readonly string _processedBucket = configuration["Minio:BucketProcessed"] ?? "video-processed";

    public async Task EnsureBucketAsync()
    {
        try
        {
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_tempBucket)
            );

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_tempBucket)
                );
                logger.LogInformation("Bucket {Bucket} created", _tempBucket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring MinIO bucket");
            throw;
        }
    }

    public async Task UploadFileAsync(string objectKey, Stream stream, string contentType)
    {
        await EnsureBucketAsync();

        var args = new PutObjectArgs()
            .WithBucket(_tempBucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args);

        logger.LogInformation("File {ObjectKey} uploaded to MinIO bucket {Bucket}", objectKey, _tempBucket);
    }
    public async Task<byte[]> GetProcessedObjectBytesAsync(string objectKey, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_processedBucket)
            .WithObject(objectKey)
            .WithCallbackStream(s => s.CopyTo(ms)), ct);
        return ms.ToArray();
    }
}
