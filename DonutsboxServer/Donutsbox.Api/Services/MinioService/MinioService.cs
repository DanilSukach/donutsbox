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
    private readonly string _imagesBucket = configuration["Minio:BucketImages"] ?? "images";

    public string GetTempBucket() => _tempBucket;
    public string GetProcessedBucket() => _processedBucket;
    public string GetImagesBucket() => _imagesBucket;

    public async Task EnsureBucketAsync()
    {
        async Task ensure(string bucket)
        {
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                logger.LogInformation("Bucket {Bucket} created", bucket);
            }
        }

        await ensure(_tempBucket);
        await ensure(_processedBucket);
        await ensure(_imagesBucket);
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

    public async Task UploadImageAsync(string objectKey, Stream stream, string contentType)
    {
        await EnsureBucketAsync();

        var args = new PutObjectArgs()
            .WithBucket(_imagesBucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args);
        logger.LogInformation("Image {ObjectKey} uploaded to MinIO bucket {Bucket}", objectKey, _imagesBucket);
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

    public async Task<string> GetPresignedGetUrlAsync(string objectKey, string bucket, int expiresInSeconds = 300)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry(expiresInSeconds);
        var url = await _client.PresignedGetObjectAsync(args);
        
        // Заменяем внутренний адрес MinIO на публичный через nginx
        var publicEndpoint = configuration["Minio:PublicEndpoint"];
        if (!string.IsNullOrEmpty(publicEndpoint))
        {
            // Парсим URL и заменяем хост на публичный endpoint
            var uri = new Uri(url);
            var queryString = uri.Query;
            // Путь от MinIO уже содержит bucket: /images/banners/...
            var path = uri.AbsolutePath;
            
            // Формируем новый URL через nginx
            // publicEndpoint = https://localhost/minio
            // path = /images/banners/... (уже содержит bucket)
            // queryString = ?X-Amz-...
            // Итого: https://localhost/minio/images/banners/...?X-Amz-...
            var newUrl = $"{publicEndpoint}{path}{queryString}";
            logger.LogDebug("Presigned URL преобразован: {OriginalUrl} -> {NewUrl}", url, newUrl);
            return newUrl;
        }
        
        return url;
    }

    public async Task DeleteObjectAsync(string objectKey, string bucket)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey);

            await _client.RemoveObjectAsync(args);
            logger.LogInformation("Object {ObjectKey} deleted from MinIO bucket {Bucket}", objectKey, bucket);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete object {ObjectKey} from bucket {Bucket}", objectKey, bucket);
        }
    }

    public async Task DeleteDirectoryAsync(string prefix, string bucket)
    {
        try
        {
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucket)
                .WithPrefix(prefix)
                .WithRecursive(true);

            var objects = new List<string>();
            await foreach (var item in _client.ListObjectsEnumAsync(listArgs))
            {
                objects.Add(item.Key);
            }

            if (objects.Count > 0)
            {
                foreach (var objectKey in objects)
                {
                    try
                    {
                        var removeArgs = new RemoveObjectArgs()
                            .WithBucket(bucket)
                            .WithObject(objectKey);

                        await _client.RemoveObjectAsync(removeArgs);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete object {ObjectKey} from bucket {Bucket}", objectKey, bucket);
                    }
                }

                logger.LogInformation("Deleted {Count} objects with prefix {Prefix} from bucket {Bucket}",
                    objects.Count, prefix, bucket);
            }
            else
            {
                logger.LogInformation("No objects found with prefix {Prefix} in bucket {Bucket}", prefix, bucket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete directory with prefix {Prefix} from bucket {Bucket}", prefix, bucket);
        }
    }
}
