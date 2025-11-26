using Confluent.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace Donutsbox.Api.Services.Kafka;

public class VideoProcessedConsumer(
    ILogger<VideoProcessedConsumer> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    IHostApplicationLifetime appLifetime,
    IMinioService minioService
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ждём полного старта приложения (чтобы не блокировать Swagger)
        var startedTcs = new TaskCompletionSource();
        appLifetime.ApplicationStarted.Register(() => startedTcs.SetResult());
        await startedTcs.Task;

        logger.LogInformation("VideoProcessedConsumer starting...");

        var bootstrap = config["Kafka:BootstrapServers"];
        var topic = config["Kafka:Topics:VideoProcessed"];
        var groupId = config["Kafka:GroupIdApi"] ?? "video-processed-updater";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .SetErrorHandler((_, e) => logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        consumer.Subscribe(topic);
        logger.LogInformation("Subscribed to {Topic}", topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(TimeSpan.FromSeconds(1));

                if (cr == null || cr.IsPartitionEOF)
                    continue;

                logger.LogInformation("Raw video.processed: {Message}", cr.Message.Value);

                var evt = JsonSerializer.Deserialize<VideoProcessedEvent>(cr.Message.Value, JsonOptions);
                if (evt == null)
                {
                    logger.LogWarning("Failed to deserialize VideoProcessedEvent");
                    consumer.Commit(cr);
                    continue;
                }

                logger.LogInformation("Processing event for video {VideoId}", evt.VideoId);

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DonutsboxDbContext>();

                var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == evt.VideoId, stoppingToken);
                if (video == null)
                {
                    logger.LogWarning("Video {VideoId} not found", evt.VideoId);
                    consumer.Commit(cr);
                    continue;
                }

                // Перемещаем превью из временного бакета в обработанный, если оно есть
                if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                {
                    try
                    {
                        var tempBucket = minioService.GetTempBucket();
                        var processedBucket = minioService.GetProcessedBucket();
                        var oldThumbnailKey = video.ThumbnailUrl;
                        
                        // Новый ключ для превью в обработанном бакете
                        var newThumbnailKey = $"processed/{evt.VideoId}/thumbnail{Path.GetExtension(oldThumbnailKey)}";
                        
                        // Копируем превью из tempBucket в processedBucket
                        await minioService.CopyObjectAsync(
                            oldThumbnailKey,
                            tempBucket,
                            newThumbnailKey,
                            processedBucket
                        );
                        
                        // Обновляем ThumbnailUrl на новое местоположение
                        video.ThumbnailUrl = newThumbnailKey;
                        
                        // Удаляем старое превью из временного бакета
                        await minioService.DeleteObjectAsync(oldThumbnailKey, tempBucket);
                        
                        logger.LogInformation("Thumbnail moved for video {VideoId} from {OldKey} to {NewKey}",
                            evt.VideoId, oldThumbnailKey, newThumbnailKey);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to move thumbnail for video {VideoId}", evt.VideoId);
                        // Продолжаем обработку даже если не удалось переместить превью
                    }
                }

                video.ProcessedPath = evt.OutputPath;
                video.Status = "READY";
                await db.SaveChangesAsync(stoppingToken);

                consumer.Commit(cr);
                logger.LogInformation("Updated video {VideoId} as READY with path {Path}", evt.VideoId, evt.OutputPath);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling video.processed");
                await Task.Delay(1000, stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("VideoProcessedConsumer stopped");
    }
}