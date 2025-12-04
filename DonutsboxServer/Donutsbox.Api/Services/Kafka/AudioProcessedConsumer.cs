using Confluent.Kafka;
using Donutsbox.Domain.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Donutsbox.Api.Services.Kafka;

public class AudioProcessedConsumer(
    ILogger<AudioProcessedConsumer> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    IHostApplicationLifetime appLifetime
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

        logger.LogInformation("AudioProcessedConsumer starting...");

        var bootstrap = config["Kafka:BootstrapServers"];
        var topic = config["Kafka:Topics:AudioProcessed"] ?? "audio.processed";
        var groupId = config["Kafka:GroupIdApi"] ?? "audio-processed-updater";

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

                logger.LogInformation("Raw audio.processed: {Message}", cr.Message.Value);

                var evt = JsonSerializer.Deserialize<AudioProcessedEvent>(cr.Message.Value, JsonOptions);
                if (evt == null)
                {
                    logger.LogWarning("Failed to deserialize AudioProcessedEvent");
                    consumer.Commit(cr);
                    continue;
                }

                logger.LogInformation("Processing event for audio {AudioId}", evt.AudioId);

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DonutsboxDbContext>();

                var audio = await db.Audios.FirstOrDefaultAsync(a => a.Id == evt.AudioId, stoppingToken);
                if (audio == null)
                {
                    logger.LogWarning("Audio {AudioId} not found", evt.AudioId);
                    consumer.Commit(cr);
                    continue;
                }

                // Обновляем статус и путь к обработанному файлу
                audio.Status = "READY";
                audio.ProcessedPath = evt.OutputPath;

                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Audio {AudioId} marked as READY with path {Path}", evt.AudioId, evt.OutputPath);

                consumer.Commit(cr);
            }
            catch (ConsumeException ex)
            {
                // Если топик не существует, логируем предупреждение и ждем
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Topic {Topic} does not exist yet. Waiting for topic creation...", topic);
                    await Task.Delay(5000, stoppingToken); // Ждем 5 секунд перед повторной попыткой
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing audio.processed event");
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("AudioProcessedConsumer stopping");
        consumer.Close();
    }
}

