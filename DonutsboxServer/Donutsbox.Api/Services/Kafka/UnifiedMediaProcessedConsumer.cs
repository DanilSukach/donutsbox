using Confluent.Kafka;
using Donutsbox.Api.Hubs;
using Donutsbox.Api.Services.CreatorPostService;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace Donutsbox.Api.Services.Kafka;

/// <summary>
/// Unified Kafka consumer service that handles both video.processed and audio.processed events
/// </summary>
public class UnifiedMediaProcessedConsumer(
    ILogger<UnifiedMediaProcessedConsumer> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    IHostApplicationLifetime appLifetime,
    IMinioService minioService,
    IHubContext<MediaProcessingHub> hubContext
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

        logger.LogInformation("UnifiedMediaProcessedConsumer starting...");

        var bootstrap = config["Kafka:BootstrapServers"];
        var videoTopic = config["Kafka:Topics:VideoProcessed"] ?? "video.processed";
        var audioTopic = config["Kafka:Topics:AudioProcessed"] ?? "audio.processed";
        var groupId = config["Kafka:GroupIdApi"] ?? "media-processed-updater";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000,
            // Оптимизация производительности
            FetchMinBytes = 1,
            FetchWaitMaxMs = 100, // Уменьшаем время ожидания для более быстрого получения сообщений
            MaxPartitionFetchBytes = 1048576, // 1MB на партицию
            // Настройки для автоматического переподключения
            ReconnectBackoffMs = 1000, // Начальная задержка переподключения: 1 секунда
            ReconnectBackoffMaxMs = 10000, // Максимальная задержка переподключения: 10 секунд
            SocketKeepaliveEnable = true, // Включаем keepalive для поддержания соединения
            MetadataMaxAgeMs = 300000, // 5 минут - максимальный возраст метаданных
            SocketTimeoutMs = 60000 // 60 секунд - timeout для сокета
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .SetErrorHandler((_, e) =>
            {
                // Логируем ошибки подключения как предупреждения, т.к. они будут автоматически обработаны
                if (e.Code == ErrorCode.Local_Transport)
                {
                    logger.LogWarning("Kafka Consumer connection issue: {Reason} (Code: {Code}). Will retry automatically.", 
                        e.Reason, e.Code);
                }
                else if (e.IsFatal)
                {
                    logger.LogError("Kafka Consumer Fatal Error: Code={Code}, Reason={Reason}", e.Code, e.Reason);
                }
                else
                {
                    logger.LogWarning("Kafka Consumer Error: Code={Code}, Reason={Reason}", e.Code, e.Reason);
                }
            })
            .Build();

        // Подписываемся на оба топика
        consumer.Subscribe([videoTopic, audioTopic]);
        logger.LogInformation("Subscribed to topics: {VideoTopic}, {AudioTopic}", videoTopic, audioTopic);

        var reconnectDelay = TimeSpan.FromSeconds(1); // Начальная задержка переподключения
        const int maxReconnectDelay = 10; // Максимальная задержка в секундах
        var consumeTimeout = TimeSpan.FromSeconds(1); // Уменьшаем timeout для более частых проверок

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(consumeTimeout);
                
                // Сбрасываем задержку переподключения при успешном получении сообщения
                reconnectDelay = TimeSpan.FromSeconds(1);

                if (cr == null || cr.IsPartitionEOF)
                {
                    // Добавляем небольшую задержку при отсутствии сообщений для снижения нагрузки
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                var topic = cr.Topic;
                logger.LogInformation("Received message from topic {Topic}: {Message}", topic, cr.Message.Value);

                // Определяем тип события по топику
                bool shouldCommit = false;
                if (topic == videoTopic)
                {
                    shouldCommit = await ProcessVideoEvent(cr, stoppingToken);
                }
                else if (topic == audioTopic)
                {
                    shouldCommit = await ProcessAudioEvent(cr, stoppingToken);
                }
                else
                {
                    logger.LogWarning("Unknown topic {Topic}, skipping message", topic);
                    shouldCommit = true;
                }

                if (shouldCommit)
                {
                    consumer.Commit(cr);
                    logger.LogInformation("Message committed successfully for topic {Topic}, offset {Offset}", topic, cr.Offset);
                }
            }
            catch (ConsumeException ex)
            {
                // Если топик не существует, логируем предупреждение и ждем
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic does not exist yet. Waiting for topic creation...");
                    await Task.Delay(5000, stoppingToken).ConfigureAwait(false); // Ждем 5 секунд перед повторной попыткой
                    reconnectDelay = TimeSpan.FromSeconds(1); // Сбрасываем задержку
                }
                else if (ex.Error.Code == ErrorCode.Local_Transport)
                {
                    // Ошибки подключения - переподключаемся с экспоненциальной задержкой
                    logger.LogWarning("Kafka connection error: {Reason} (Code: {Code}). Reconnecting in {Delay}s...", 
                        ex.Error.Reason, ex.Error.Code, reconnectDelay.TotalSeconds);
                    await Task.Delay(reconnectDelay, stoppingToken).ConfigureAwait(false);
                    
                    // Увеличиваем задержку экспоненциально, но не более maxReconnectDelay
                    var nextDelay = reconnectDelay.TotalSeconds * 2;
                    reconnectDelay = TimeSpan.FromSeconds(Math.Min(nextDelay, maxReconnectDelay));
                    
                    // Пытаемся переподписаться на топики
                    try
                    {
                        consumer.Subscribe([videoTopic, audioTopic]);
                        logger.LogInformation("Re-subscribed to topics after connection error");
                    }
                    catch (Exception subEx)
                    {
                        logger.LogError(subEx, "Failed to re-subscribe to topics");
                    }
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error: {Reason} (Code: {Code})", ex.Error.Reason, ex.Error.Code);
                    // Небольшая задержка при других ошибках потребления
                    await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing media.processed event");
                // Небольшая задержка при ошибках обработки
                await Task.Delay(2000, stoppingToken);
            }
        }

        logger.LogInformation("UnifiedMediaProcessedConsumer stopping");
        consumer.Close();
    }

    private async Task<bool> ProcessVideoEvent(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<VideoProcessedEvent>(cr.Message.Value, JsonOptions);
            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize VideoProcessedEvent");
                return true; // Commit even if deserialization failed
            }

            logger.LogInformation("Processing event for video {VideoId}", evt.VideoId);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DonutsboxDbContext>();

            var video = await db.Videos
                .Include(v => v.ContentPost)
                .FirstOrDefaultAsync(v => v.Id == evt.VideoId, stoppingToken);
            if (video == null)
            {
                logger.LogWarning("Video {VideoId} not found", evt.VideoId);
                return true; // Commit even if video not found
            }

            var postId = video.ContentPostId;

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

            logger.LogInformation("Video {VideoId} marked as READY, sending SignalR notification immediately", evt.VideoId);

            // Отправляем уведомление через SignalR СРАЗУ после обновления статуса
            try
            {
                await hubContext.Clients.Group($"user-{video.UserId}").SendAsync("VideoProcessed", new
                {
                    videoId = evt.VideoId,
                    status = "READY",
                    processedPath = evt.OutputPath,
                    postId
                }, stoppingToken);
                logger.LogInformation("✅ Sent SignalR notification for video {VideoId} to user {UserId}", evt.VideoId, video.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to send SignalR notification for video {VideoId}", evt.VideoId);
            }

            // Проверяем, можно ли автоматически опубликовать пост ПОСЛЕ отправки уведомления
            try
            {
                var creatorPostService = scope.ServiceProvider.GetRequiredService<ICreatorPostService>();
                var wasPublished = await creatorPostService.TryPublishPostAfterMediaProcessingAsync(postId);
                if (wasPublished)
                {
                    logger.LogInformation("✅ Post {PostId} automatically published after video {VideoId} processing", postId, evt.VideoId);
                }
                else
                {
                    logger.LogInformation("⏳ Post {PostId} not published yet, waiting for other media (video {VideoId} is ready)", postId, evt.VideoId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to check auto-publish for post {PostId} after video {VideoId} processing", postId, evt.VideoId);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing video.processed event");
            return false; // Don't commit on error, will retry
        }
    }

    private async Task<bool> ProcessAudioEvent(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AudioProcessedEvent>(cr.Message.Value, JsonOptions);
            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize AudioProcessedEvent");
                return true; // Commit even if deserialization failed
            }

            logger.LogInformation("Processing event for audio {AudioId}", evt.AudioId);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DonutsboxDbContext>();

            var audio = await db.Audios
                .Include(a => a.ContentPost)
                .FirstOrDefaultAsync(a => a.Id == evt.AudioId, stoppingToken);
            if (audio == null)
            {
                logger.LogWarning("Audio {AudioId} not found", evt.AudioId);
                return true; // Commit even if audio not found
            }

            var postId = audio.ContentPostId;
            
            // Логируем текущий статус перед обновлением
            logger.LogInformation("Audio {AudioId} current status: {Status}, ProcessedPath: {ProcessedPath}", 
                evt.AudioId, audio.Status, audio.ProcessedPath);
            
            // Логируем информацию о посте
            if (audio.ContentPost != null)
            {
                logger.LogInformation("Audio {AudioId} belongs to post {PostId}, IsPublished: {IsPublished}, IsPendingPublish: {IsPendingPublish}", 
                    evt.AudioId, postId, audio.ContentPost.IsPublished, audio.ContentPost.IsPendingPublish);
            }

            // File.Service.Api уже загружает обработанное аудио в audioProcessedBucket,
            // поэтому просто сохраняем путь как есть
            audio.ProcessedPath = evt.OutputPath;

            // Обновляем статус
            var oldStatus = audio.Status;
            audio.Status = "READY";

            await db.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Audio {AudioId} status updated from {OldStatus} to {NewStatus}, ProcessedPath: {ProcessedPath}", 
                evt.AudioId, oldStatus, audio.Status, audio.ProcessedPath);

            // Отправляем уведомление через SignalR СРАЗУ после обновления статуса
            try
            {
                await hubContext.Clients.Group($"user-{audio.UserId}").SendAsync("AudioProcessed", new
                {
                    audioId = evt.AudioId,
                    status = "READY",
                    processedPath = evt.OutputPath,
                    postId
                }, stoppingToken);
                logger.LogInformation("✅ Sent SignalR notification for audio {AudioId} to user {UserId}", evt.AudioId, audio.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to send SignalR notification for audio {AudioId}", evt.AudioId);
            }

            // Проверяем, можно ли автоматически опубликовать пост ПОСЛЕ отправки уведомления
            try
            {
                var creatorPostService = scope.ServiceProvider.GetRequiredService<ICreatorPostService>();
                var wasPublished = await creatorPostService.TryPublishPostAfterMediaProcessingAsync(postId);
                if (wasPublished)
                {
                    logger.LogInformation("✅ Post {PostId} automatically published after audio {AudioId} processing", postId, evt.AudioId);
                }
                else
                {
                    logger.LogInformation("⏳ Post {PostId} not published yet, waiting for other media (audio {AudioId} is ready)", postId, evt.AudioId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to check auto-publish for post {PostId} after audio {AudioId} processing", postId, evt.AudioId);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audio.processed event");
            return false; // Don't commit on error, will retry
        }
    }
}

