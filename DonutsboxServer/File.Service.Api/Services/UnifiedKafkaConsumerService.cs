using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

/// <summary>
/// Unified Kafka consumer service that handles both video and audio upload events
/// </summary>
public class UnifiedKafkaConsumerService(
    ILogger<UnifiedKafkaConsumerService> logger,
    IServiceProvider provider,
    IConfiguration config,
    VideoCancellationService videoCancellationService,
    AudioCancellationService audioCancellationService) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? bootstrapServers = null;
        string? videoTopic = null;
        string? audioTopic = null;
        string? groupId = null;

        try
        {
            logger.LogInformation("UnifiedKafkaConsumerService starting...");

            bootstrapServers = config["Kafka:BootstrapServers"];
            videoTopic = config["Kafka:Topics:VideoUploaded"] ?? "video.uploaded";
            audioTopic = config["Kafka:Topics:AudioUploaded"] ?? "audio.uploaded";
            groupId = config["Kafka:GroupId"] ?? "file-processor";

            if (string.IsNullOrEmpty(bootstrapServers))
            {
                logger.LogError("Kafka BootstrapServers is not configured!");
                return;
            }

            logger.LogInformation("Initializing Unified Kafka Consumer: BootstrapServers={BootstrapServers}, VideoTopic={VideoTopic}, AudioTopic={AudioTopic}, GroupId={GroupId}",
                bootstrapServers, videoTopic, audioTopic, groupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize UnifiedKafkaConsumerService");
            throw;
        }

        var conf = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 1800000, // 30 минут для длительных операций обработки видео
            // Оптимизация производительности
            FetchMinBytes = 1,
            FetchWaitMaxMs = 500,
            MaxPartitionFetchBytes = 1048576 // 1MB на партицию
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(conf)
            .SetErrorHandler((_, e) =>
            {
                if (e.IsFatal)
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
        logger.LogInformation("Unified Kafka consumer subscribed to topics: {VideoTopic}, {AudioTopic}", videoTopic, audioTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(TimeSpan.FromSeconds(10));

                if (cr == null || cr.IsPartitionEOF)
                {
                    // Добавляем небольшую задержку при отсутствии сообщений для снижения нагрузки
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                var topic = cr.Topic;
                logger.LogInformation("Received message from topic {Topic}, partition {Partition}, offset {Offset}: {Message}",
                    topic, cr.Partition, cr.Offset, cr.Message.Value);

                // Коммитим сообщение сразу, чтобы не блокировать обработку других сообщений
                // Обработка будет происходить параллельно в фоне
                consumer.Commit(cr);
                logger.LogInformation("Message committed immediately for topic {Topic}, offset {Offset}", topic, cr.Offset);

                // Запускаем обработку в фоне без блокировки основного цикла
                // Это позволяет обрабатывать несколько видео/аудио параллельно
                if (topic == videoTopic)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessVideoEventBackground(cr, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error in background video processing for offset {Offset}", cr.Offset);
                        }
                    }, stoppingToken);
                }
                else if (topic == audioTopic)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessAudioEventBackground(cr, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error in background audio processing for offset {Offset}", cr.Offset);
                        }
                    }, stoppingToken);
                }
                else
                {
                    logger.LogWarning("Unknown topic {Topic}, skipping message", topic);
                }
            }
            catch (ConsumeException ex)
            {
                // Если топик не существует, ждем его создания (Kafka создаст автоматически при первой публикации)
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic does not exist yet. Waiting for topic creation (will be auto-created on first publish)...");
                    await Task.Delay(5000, stoppingToken); // Ждем 5 секунд перед повторной попыткой
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    // Небольшая задержка при ошибках потребления
                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Consumer cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message");
                // Небольшая задержка при ошибках обработки
                await Task.Delay(2000, stoppingToken);
            }
        }

        logger.LogInformation("Unified Kafka consumer shutting down");
        consumer.Close();
    }

    private async Task ProcessVideoEventBackground(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<VideoUploadedEvent>(cr.Message.Value, JsonOptions);

            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize video message, skipping");
                return;
            }

            logger.LogInformation("Processing video.uploaded event for VideoId={VideoId}, ObjectKey={ObjectKey}",
                evt.VideoId, evt.ObjectKey);

            // Check if video processing was already cancelled
            if (videoCancellationService.IsCancelled(evt.VideoId))
            {
                logger.LogInformation("Video {VideoId} was cancelled before processing started, skipping", evt.VideoId);
                return;
            }

            using var scope = provider.CreateScope();
            var ffmpeg = scope.ServiceProvider.GetRequiredService<FfmpegService>();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

            // Register video for cancellation tracking
            var cancellationToken = videoCancellationService.RegisterVideo(evt.VideoId);

            try
            {
                logger.LogInformation("Starting video processing for {VideoId}", evt.VideoId);
                var outputPath = await ffmpeg.ProcessVideoAsync(evt.VideoId, evt.ObjectKey, cancellationToken);

                // Check cancellation again after processing
                if (videoCancellationService.IsCancelled(evt.VideoId))
                {
                    logger.LogInformation("Video {VideoId} was cancelled during processing", evt.VideoId);
                    return;
                }

                logger.LogInformation("Video processed, publishing result for {VideoId}", evt.VideoId);
                await producer.PublishProcessedAsync(new VideoProcessedEvent(evt.VideoId, outputPath));
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Video processing was cancelled by user, not by service shutdown
                logger.LogInformation("Video processing was cancelled: {Message}", ex.Message);
            }
            finally
            {
                videoCancellationService.UnregisterVideo(evt.VideoId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing video event");
            // Не пробрасываем исключение, так как сообщение уже закоммичено
        }
    }

    private async Task ProcessAudioEventBackground(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AudioUploadedEvent>(cr.Message.Value, JsonOptions);

            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize audio message, skipping");
                return;
            }

            logger.LogInformation("Processing audio.uploaded event for AudioId={AudioId}, ObjectKey={ObjectKey}",
                evt.AudioId, evt.ObjectKey);

            // Check if audio processing was already cancelled
            if (audioCancellationService.IsCancelled(evt.AudioId))
            {
                logger.LogInformation("Audio {AudioId} was cancelled before processing started, skipping", evt.AudioId);
                return;
            }

            using var scope = provider.CreateScope();
            var audioProcessor = scope.ServiceProvider.GetRequiredService<AudioProcessingService>();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

            // Register audio for cancellation tracking
            var cancellationToken = audioCancellationService.RegisterAudio(evt.AudioId);

            try
            {
                logger.LogInformation("Starting audio processing for {AudioId}", evt.AudioId);
                var outputPath = await audioProcessor.ProcessAudioAsync(evt.AudioId, evt.ObjectKey, cancellationToken);

                // Check cancellation again after processing
                if (audioCancellationService.IsCancelled(evt.AudioId))
                {
                    logger.LogInformation("Audio {AudioId} was cancelled during processing", evt.AudioId);
                    return;
                }

                logger.LogInformation("Audio processed, publishing result for {AudioId}", evt.AudioId);
                await producer.PublishAudioProcessedAsync(new AudioProcessedEvent(evt.AudioId, outputPath));
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Audio processing was cancelled by user, not by service shutdown
                logger.LogInformation("Audio processing was cancelled: {Message}", ex.Message);
            }
            finally
            {
                audioCancellationService.UnregisterAudio(evt.AudioId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audio event");
            // Не пробрасываем исключение, так как сообщение уже закоммичено
        }
    }
}

