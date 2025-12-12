using Minio;
using Minio.DataModel.Args;

namespace Donutsbox.Api.Services.MinioService;

public class MinioCleanupService : BackgroundService
{
    private readonly IMinioService _minioService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MinioCleanupService> _logger;
    private readonly MinioClient _minioClient;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _fileMaxAge = TimeSpan.FromHours(1);

    public MinioCleanupService(
        IMinioService minioService,
        IConfiguration configuration,
        ILogger<MinioCleanupService> logger)
    {
        _minioService = minioService;
        _configuration = configuration;
        _logger = logger;

        var endpoint = configuration["Minio:Endpoint"] ?? throw new InvalidOperationException("Minio:Endpoint is not configured");
        var accessKey = configuration["Minio:AccessKey"] ?? throw new InvalidOperationException("Minio:AccessKey is not configured");
        var secretKey = configuration["Minio:SecretKey"] ?? throw new InvalidOperationException("Minio:SecretKey is not configured");

        _minioClient = (MinioClient)new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MinIO Cleanup Service started. Cleanup interval: {Interval}, Max file age: {MaxAge}",
            _cleanupInterval, _fileMaxAge);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTempFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during MinIO cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("MinIO Cleanup Service stopped");
    }

    private async Task CleanupTempFilesAsync(CancellationToken cancellationToken)
    {
        var tempBucket = _minioService.GetTempBucket();
        var audioBucket = _minioService.GetAudioBucket();

        _logger.LogInformation("Starting cleanup of temporary files in buckets: {TempBucket}, {AudioBucket}",
            tempBucket, audioBucket);

        var deletedCount = 0;

        deletedCount += await CleanupBucketAsync(tempBucket, cancellationToken);

        deletedCount += await CleanupBucketAsync(audioBucket, cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleanup completed. Deleted {Count} old temporary files", deletedCount);
        }
        else
        {
            _logger.LogDebug("Cleanup completed. No old files to delete");
        }
    }

    private async Task<int> CleanupBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - _fileMaxAge;
            var deletedCount = 0;

            var listArgs = new ListObjectsArgs()
                .WithBucket(bucket)
                .WithRecursive(true);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs).WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var statArgs = new StatObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(item.Key);

                    var objectStat = await _minioClient.StatObjectAsync(statArgs);
                    var lastModified = objectStat.LastModified;

                    if (lastModified < cutoffTime)
                    {
                        try
                        {
                            await _minioService.DeleteObjectAsync(item.Key, bucket);
                            deletedCount++;
                            _logger.LogDebug("Deleted old temporary file: {ObjectKey} from bucket {Bucket} (age: {Age})",
                                item.Key, bucket, DateTime.UtcNow - lastModified);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete object {ObjectKey} from bucket {Bucket}",
                                item.Key, bucket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get metadata for object {ObjectKey} from bucket {Bucket}, skipping",
                        item.Key, bucket);
                }
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up bucket {Bucket}", bucket);
            return 0;
        }
    }
}
