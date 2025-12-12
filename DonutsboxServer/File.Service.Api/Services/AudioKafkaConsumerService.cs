using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class AudioKafkaConsumerService(
    ILogger<AudioKafkaConsumerService> logger, 
    IServiceProvider provider, 
    IConfiguration config,
    AudioCancellationService cancellationService) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AudioKafkaConsumerService starting...");
        
        var bootstrapServers = config["Kafka:BootstrapServers"];
        var topic = config["Kafka:Topics:AudioUploaded"] ?? "audio.uploaded";
        var groupId = config["Kafka:GroupId"] ?? "file-processor";

        if (string.IsNullOrEmpty(bootstrapServers))
        {
            logger.LogError("Kafka BootstrapServers is not configured!");
            return;
        }

        if (string.IsNullOrEmpty(topic))
        {
            logger.LogError("Kafka topic AudioUploaded is not configured!");
            return;
        }

        logger.LogInformation("Initializing Audio Kafka Consumer: BootstrapServers={BootstrapServers}, Topic={Topic}, GroupId={GroupId}",
            bootstrapServers, topic, groupId);

        var conf = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 1800000, // 30 минут для длительных операций обработки
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

        consumer.Subscribe(topic);
        logger.LogInformation("Audio Kafka consumer subscribed to topic {Topic}", topic);

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

                logger.LogInformation("Received message from partition {Partition}, offset {Offset}: {Message}",
                    cr.Partition, cr.Offset, cr.Message.Value);

                var evt = JsonSerializer.Deserialize<AudioUploadedEvent>(cr.Message.Value, JsonOptions);

                if (evt == null)
                {
                    logger.LogWarning("Failed to deserialize message, skipping");
                    consumer.Commit(cr);
                    continue;
                }

                logger.LogInformation("Processing audio.uploaded event for AudioId={AudioId}, ObjectKey={ObjectKey}",
                    evt.AudioId, evt.ObjectKey);

                // Check if audio processing was already cancelled
                if (cancellationService.IsCancelled(evt.AudioId))
                {
                    logger.LogInformation("Audio {AudioId} was cancelled before processing started, skipping", evt.AudioId);
                    consumer.Commit(cr);
                    continue;
                }

                using var scope = provider.CreateScope();
                var audioProcessor = scope.ServiceProvider.GetRequiredService<AudioProcessingService>();
                var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

                // Register audio for cancellation tracking
                var cancellationToken = cancellationService.RegisterAudio(evt.AudioId);

                try
                {
                    logger.LogInformation("Starting audio processing for {AudioId}", evt.AudioId);
                    var outputPath = await audioProcessor.ProcessAudioAsync(evt.AudioId, evt.ObjectKey, cancellationToken);

                    // Check cancellation again after processing
                    if (cancellationService.IsCancelled(evt.AudioId))
                    {
                        logger.LogInformation("Audio {AudioId} was cancelled during processing", evt.AudioId);
                        consumer.Commit(cr);
                        continue;
                    }

                    logger.LogInformation("Audio processed, publishing result for {AudioId}", evt.AudioId);
                    await producer.PublishAudioProcessedAsync(new AudioProcessedEvent(evt.AudioId, outputPath));
                }
                finally
                {
                    cancellationService.UnregisterAudio(evt.AudioId);
                }

                consumer.Commit(cr);
                logger.LogInformation("Message committed successfully for offset {Offset}", cr.Offset);
            }
            catch (ConsumeException ex)
            {
                // Если топик не существует, ждем его создания (Kafka создаст автоматически при первой публикации)
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic {Topic} does not exist yet. Waiting for topic creation (will be auto-created on first publish)...", topic);
                    await Task.Delay(5000, stoppingToken); // Ждем 5 секунд перед повторной попыткой
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    // Небольшая задержка при ошибках потребления
                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Audio processing was cancelled by user, not by service shutdown
                logger.LogInformation("Audio processing was cancelled: {Message}", ex.Message);
            }
            catch (OperationCanceledException)
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

        logger.LogInformation("Kafka consumer shutting down");
        consumer.Close();
    }
}

