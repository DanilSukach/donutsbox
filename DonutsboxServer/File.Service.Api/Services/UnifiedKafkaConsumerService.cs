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
            MaxPollIntervalMs = 300000
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
                var cr = consumer.Consume(TimeSpan.FromSeconds(5));

                if (cr == null || cr.IsPartitionEOF)
                {
                    continue;
                }

                var topic = cr.Topic;
                logger.LogInformation("Received message from topic {Topic}, partition {Partition}, offset {Offset}: {Message}",
                    topic, cr.Partition, cr.Offset, cr.Message.Value);

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
                // Если топик не существует, ждем его создания (Kafka создаст автоматически при первой публикации)
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic does not exist yet. Waiting for topic creation (will be auto-created on first publish)...");
                    await Task.Delay(5000, stoppingToken); // Ждем 5 секунд перед повторной попыткой
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
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
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("Unified Kafka consumer shutting down");
        consumer.Close();
    }

    private async Task<bool> ProcessVideoEvent(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<VideoUploadedEvent>(cr.Message.Value, JsonOptions);

            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize video message, skipping");
                return true; // Commit even if deserialization failed
            }

            logger.LogInformation("Processing video.uploaded event for VideoId={VideoId}, ObjectKey={ObjectKey}",
                evt.VideoId, evt.ObjectKey);

            // Check if video processing was already cancelled
            if (videoCancellationService.IsCancelled(evt.VideoId))
            {
                logger.LogInformation("Video {VideoId} was cancelled before processing started, skipping", evt.VideoId);
                return true; // Commit cancelled messages
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
                    return true; // Commit cancelled messages
                }

                logger.LogInformation("Video processed, publishing result for {VideoId}", evt.VideoId);
                await producer.PublishProcessedAsync(new VideoProcessedEvent(evt.VideoId, outputPath));
                return true; // Commit successful processing
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Video processing was cancelled by user, not by service shutdown
                logger.LogInformation("Video processing was cancelled: {Message}", ex.Message);
                return true; // Commit cancelled messages
            }
            finally
            {
                videoCancellationService.UnregisterVideo(evt.VideoId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing video event");
            return false; // Don't commit on error, will retry
        }
    }

    private async Task<bool> ProcessAudioEvent(ConsumeResult<Ignore, string> cr, CancellationToken stoppingToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AudioUploadedEvent>(cr.Message.Value, JsonOptions);

            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize audio message, skipping");
                return true; // Commit even if deserialization failed
            }

            logger.LogInformation("Processing audio.uploaded event for AudioId={AudioId}, ObjectKey={ObjectKey}",
                evt.AudioId, evt.ObjectKey);

            // Check if audio processing was already cancelled
            if (audioCancellationService.IsCancelled(evt.AudioId))
            {
                logger.LogInformation("Audio {AudioId} was cancelled before processing started, skipping", evt.AudioId);
                return true; // Commit cancelled messages
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
                    return true; // Commit cancelled messages
                }

                logger.LogInformation("Audio processed, publishing result for {AudioId}", evt.AudioId);
                await producer.PublishAudioProcessedAsync(new AudioProcessedEvent(evt.AudioId, outputPath));
                return true; // Commit successful processing
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Audio processing was cancelled by user, not by service shutdown
                logger.LogInformation("Audio processing was cancelled: {Message}", ex.Message);
                return true; // Commit cancelled messages
            }
            finally
            {
                audioCancellationService.UnregisterAudio(evt.AudioId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audio event");
            return false; // Don't commit on error, will retry
        }
    }
}

