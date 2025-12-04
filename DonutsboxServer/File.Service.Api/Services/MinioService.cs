using Minio;

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

    private async Task EnsureBucketAsync(string bucket)
    {
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
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(folderPath, file).Replace("\\", "/");
            var key = $"{baseKey}/{rel}";
            await client.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
                .WithBucket(processedBucket)
                .WithObject(key)
                .WithFileName(file));
        }
    }

    public async Task UploadProcessedFileAsync(string objectKey, string localFilePath)
    {
        await EnsureBucketAsync(processedBucket);
        await client.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
            .WithBucket(processedBucket)
            .WithObject(objectKey)
            .WithFileName(localFilePath));
    }
}