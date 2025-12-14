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
            // Оптимизация производительности для параллельной обработки
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

        using var consumer = new ConsumerBuilder<Ignore, string>(conf)
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
        logger.LogInformation("Unified Kafka consumer subscribed to topics: {VideoTopic}, {AudioTopic} with GroupId={GroupId}", 
            videoTopic, audioTopic, groupId);
        
        // Логируем конфигурацию для диагностики
        logger.LogInformation("Consumer configuration - VideoTopic: {VideoTopic}, AudioTopic: {AudioTopic}, BootstrapServers: {BootstrapServers}", 
            videoTopic, audioTopic, bootstrapServers);

        var lastConsumeTime = DateTime.UtcNow;
        var consumeTimeout = TimeSpan.FromSeconds(1); // Уменьшаем timeout для более частых проверок
        var reconnectDelay = TimeSpan.FromSeconds(1); // Начальная задержка переподключения
        const int maxReconnectDelay = 10; // Максимальная задержка в секундах
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(consumeTimeout);
                
                // Сбрасываем задержку переподключения при успешном получении сообщения
                reconnectDelay = TimeSpan.FromSeconds(1);

                if (cr == null || cr.IsPartitionEOF)
                {
                    // Периодически логируем, что consumer работает (каждые 30 секунд)
                    if ((DateTime.UtcNow - lastConsumeTime).TotalSeconds > 30)
                    {
                        logger.LogDebug("Consumer is waiting for messages from topics: {VideoTopic}, {AudioTopic}", 
                            videoTopic, audioTopic);
                        lastConsumeTime = DateTime.UtcNow;
                    }
                    // Добавляем небольшую задержку при отсутствии сообщений для снижения нагрузки
                    await Task.Delay(100, stoppingToken).ConfigureAwait(false);
                    continue;
                }
                
                lastConsumeTime = DateTime.UtcNow;

                var topic = cr.Topic;
                var receivedTime = DateTime.UtcNow;
                logger.LogInformation("Received message from topic {Topic}, partition {Partition}, offset {Offset}: {Message}",
                    topic, cr.Partition, cr.Offset, cr.Message.Value);
                
                // Проверяем, что топик соответствует ожидаемым
                if (topic != videoTopic && topic != audioTopic)
                {
                    logger.LogWarning("Received message from unexpected topic {Topic} (expected {VideoTopic} or {AudioTopic}), skipping", 
                        topic, videoTopic, audioTopic);
                    consumer.Commit(cr);
                    continue;
                }

                // Коммитим сообщение сразу, чтобы не блокировать обработку других сообщений
                // Обработка будет происходить параллельно в фоне
                consumer.Commit(cr);
                logger.LogInformation("Message committed immediately for topic {Topic}, offset {Offset}", topic, cr.Offset);

                // Запускаем обработку в фоне без блокировки основного цикла
                // Используем Task.Run с TaskCreationOptions.LongRunning для CPU-bound операций
                // Это создает отдельный поток вместо использования ThreadPool, что предотвращает блокировку
                logger.LogInformation("Comparing topic '{Topic}' with videoTopic '{VideoTopic}' and audioTopic '{AudioTopic}'", 
                    topic, videoTopic, audioTopic);
                
                if (topic == videoTopic)
                {
                    logger.LogInformation("Routing video.uploaded message to background processor (offset {Offset})", cr.Offset);
                    // Используем LongRunning для создания отдельного потока вместо ThreadPool
                    _ = Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            var processingStartTime = DateTime.UtcNow;
                            var queueDelay = (processingStartTime - receivedTime).TotalMilliseconds;
                            if (queueDelay > 100)
                            {
                                logger.LogWarning("Video processing queued for {Delay}ms before starting (offset {Offset})", 
                                    queueDelay, cr.Offset);
                            }
                            
                            logger.LogInformation("Background video processing started for offset {Offset} (queued {QueueDelay}ms)", 
                                cr.Offset, queueDelay);
                            await ProcessVideoEventBackground(cr, stoppingToken).ConfigureAwait(false);
                            logger.LogInformation("Background video processing completed for offset {Offset}", cr.Offset);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error in background video processing for offset {Offset}", cr.Offset);
                        }
                    }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                }
                else if (topic == audioTopic)
                {
                    logger.LogInformation("Routing audio.uploaded message to background processor (offset {Offset})", cr.Offset);
                    // Используем LongRunning для создания отдельного потока вместо ThreadPool
                    _ = Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            var processingStartTime = DateTime.UtcNow;
                            var queueDelay = (processingStartTime - receivedTime).TotalMilliseconds;
                            if (queueDelay > 100)
                            {
                                logger.LogWarning("Audio processing queued for {Delay}ms before starting (offset {Offset})", 
                                    queueDelay, cr.Offset);
                            }
                            
                            logger.LogInformation("Background audio processing started for offset {Offset} (queued {QueueDelay}ms)", 
                                cr.Offset, queueDelay);
                            await ProcessAudioEventBackground(cr, stoppingToken).ConfigureAwait(false);
                            logger.LogInformation("Background audio processing completed for offset {Offset}", cr.Offset);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error in background audio processing for offset {Offset}", cr.Offset);
                        }
                    }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                }
                else
                {
                    logger.LogWarning("Unknown topic {Topic} (expected {VideoTopic} or {AudioTopic}), skipping message", 
                        topic, videoTopic, audioTopic);
                }
            }
            catch (ConsumeException ex)
            {
                // Если топик не существует, ждем его создания (Kafka создаст автоматически при первой публикации)
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic does not exist yet. Waiting for topic creation (will be auto-created on first publish)...");
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
        var processingStartTime = DateTime.UtcNow;
        try
        {
            var evt = JsonSerializer.Deserialize<VideoUploadedEvent>(cr.Message.Value, JsonOptions);

            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize video message, skipping");
                return;
            }

            var deserializeTime = DateTime.UtcNow;
            logger.LogInformation("Processing video.uploaded event for VideoId={VideoId}, ObjectKey={ObjectKey} (deserialized in {DeserializeMs}ms)",
                evt.VideoId, evt.ObjectKey, (deserializeTime - processingStartTime).TotalMilliseconds);

            // Check if video processing was already cancelled
            if (videoCancellationService.IsCancelled(evt.VideoId))
            {
                logger.LogInformation("Video {VideoId} was cancelled before processing started, skipping", evt.VideoId);
                return;
            }

            var scopeStartTime = DateTime.UtcNow;
            using var scope = provider.CreateScope();
            var scopeCreatedTime = DateTime.UtcNow;
            var ffmpeg = scope.ServiceProvider.GetRequiredService<FfmpegService>();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();
            var servicesReadyTime = DateTime.UtcNow;
            
            if ((scopeCreatedTime - scopeStartTime).TotalMilliseconds > 10 || 
                (servicesReadyTime - scopeCreatedTime).TotalMilliseconds > 10)
            {
                logger.LogWarning("Service resolution took {ScopeMs}ms + {ServiceMs}ms for VideoId={VideoId}", 
                    (scopeCreatedTime - scopeStartTime).TotalMilliseconds,
                    (servicesReadyTime - scopeCreatedTime).TotalMilliseconds,
                    evt.VideoId);
            }

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
                try
                {
                    await producer.PublishProcessedAsync(new VideoProcessedEvent(evt.VideoId, outputPath));
                    logger.LogInformation("Successfully published video.processed for {VideoId}", evt.VideoId);
                }
                catch (Exception publishEx)
                {
                    logger.LogError(publishEx, "Failed to publish video.processed for {VideoId}", evt.VideoId);
                    // Не пробрасываем исключение, чтобы не блокировать обработку других сообщений
                }
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
                try
                {
                    await producer.PublishAudioProcessedAsync(new AudioProcessedEvent(evt.AudioId, outputPath));
                    logger.LogInformation("Successfully published audio.processed for {AudioId}", evt.AudioId);
                }
                catch (Exception publishEx)
                {
                    logger.LogError(publishEx, "Failed to publish audio.processed for {AudioId}", evt.AudioId);
                    // Не пробрасываем исключение, чтобы не блокировать обработку других сообщений
                }
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

