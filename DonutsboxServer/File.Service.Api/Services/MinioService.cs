using Minio;
using System.Collections.Concurrent;
using System.Threading;

namespace File.Service.Api.Services;

public class MinioService(IConfiguration configuration, ILogger<MinioService> logger)
{
    private readonly MinioClient client = (MinioClient)new MinioClient()
        .WithEndpoint(configuration["Minio:Endpoint"])
        .WithCredentials(configuration["Minio:AccessKey"], configuration["Minio:SecretKey"])
        .Build();

    private readonly string tempBucket = configuration["Minio:BucketTemp"] ?? "video-temp";
    private readonly string processedBucket = configuration["Minio:BucketProcessed"] ?? "video-processed";
    private readonly string audioBucket = configuration["Minio:BucketAudio"] ?? "audio-temp";
    private readonly string audioProcessedBucket = configuration["Minio:BucketAudioProcessed"] ?? "audio-processed";

    // Кэш для проверенных бакетов, чтобы не проверять каждый раз
    private readonly ConcurrentDictionary<string, bool> _bucketCache = new();
    private readonly SemaphoreSlim _bucketCheckSemaphore = new(1, 1);

    private async Task EnsureBucketAsync(string bucket)
    {
        // Если бакет уже проверен, пропускаем проверку
        if (_bucketCache.ContainsKey(bucket))
        {
            return;
        }

        // Используем семафор для предотвращения одновременных проверок одного бакета
        await _bucketCheckSemaphore.WaitAsync();
        try
        {
            // Двойная проверка после получения блокировки
            if (_bucketCache.ContainsKey(bucket))
            {
                return;
            }

            var exists = await client.BucketExistsAsync(
                new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucket)
            );
            if (!exists)
            {
                await client.MakeBucketAsync(
                    new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucket)
                );
                logger.LogInformation("Created MinIO bucket {Bucket}", bucket);
            }
            
            // Кэшируем результат
            _bucketCache[bucket] = true;
        }
        finally
        {
            _bucketCheckSemaphore.Release();
        }
    }

    public async Task DownloadFileAsync(string objectKey, string localPath)
    {
        await EnsureBucketAsync(tempBucket);
        await client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
            .WithBucket(tempBucket)
            .WithObject(objectKey)
            .WithFile(localPath));
    }

    public async Task DownloadAudioFileAsync(string objectKey, string localPath)
    {
        await EnsureBucketAsync(audioBucket);
        await client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
            .WithBucket(audioBucket)
            .WithObject(objectKey)
            .WithFile(localPath));
    }

    public async Task UploadFolderAsync(string baseKey, string folderPath)
    {
        await EnsureBucketAsync(processedBucket);

        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        
        // Загружаем файлы параллельно для лучшей производительности (но с ограничением)
        const int maxConcurrency = 5; // Максимум 5 параллельных загрузок
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var uploadTasks = files.Select(async file =>
        {
            await semaphore.WaitAsync();
            try
            {
                var rel = Path.GetRelativePath(folderPath, file).Replace("\\", "/");
                var key = $"{baseKey}/{rel}";
                await client.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
                    .WithBucket(processedBucket)
                    .WithObject(key)
                    .WithFileName(file));
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(uploadTasks);
    }

    public async Task UploadProcessedFileAsync(string objectKey, string localFilePath)
    {
        await EnsureBucketAsync(processedBucket);
        await client.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
            .WithBucket(processedBucket)
            .WithObject(objectKey)
            .WithFileName(localFilePath));
    }

    public async Task UploadProcessedAudioFileAsync(string objectKey, string localFilePath)
    {
        await EnsureBucketAsync(audioProcessedBucket);
        await client.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
            .WithBucket(audioProcessedBucket)
            .WithObject(objectKey)
            .WithFileName(localFilePath));
    }
}