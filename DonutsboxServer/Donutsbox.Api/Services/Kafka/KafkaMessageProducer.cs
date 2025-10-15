using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Donutsbox.Api.Services.Kafka;

public class KafkaMessageProducer : IMessageProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessageProducer> _logger;
    private readonly string _topicVideoUploaded;

    // Кэшированные JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaMessageProducer(IConfiguration configuration, ILogger<KafkaMessageProducer> logger)
    {
        _logger = logger;
        _topicVideoUploaded = configuration["Kafka:Topics:VideoUploaded"] ?? "video.uploaded";

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All,  // Ждем подтверждения от всех реплик
            EnableIdempotence = true,  // Включаем идемпотентность для надежности
            MessageTimeoutMs = 30000,  // 30 секунд
            RequestTimeoutMs = 30000,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 100,

            // Для немедленной отправки без буферизации
            BatchSize = 1,  // Отправлять сразу
            LingerMs = 0,   // Без задержки

            CompressionType = CompressionType.None,
            SocketTimeoutMs = 60000,
            ApiVersionRequestTimeoutMs = 10000,

            // Добавим логирование для отладки
            Debug = "broker,topic,msg"
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka Producer Error: {Reason}", e.Reason))
            .SetLogHandler((_, log) =>
            {
                var logLevel = log.Level switch
                {
                    SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                    SyslogLevel.Warning => LogLevel.Warning,
                    SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                    _ => LogLevel.Debug
                };
                _logger.Log(logLevel, "Kafka: {Message}", log.Message);
            })
            .Build();

        _logger.LogInformation("Kafka Producer initialized with BootstrapServers: {BootstrapServers}",
            configuration["Kafka:BootstrapServers"]);
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);

            var msg = new Message<string, string>
            {
                Value = json
            };

            _logger.LogInformation("Attempting to send message to topic {Topic}", topic);

            var result = await _producer.ProduceAsync(topic, msg);

            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));

            _logger.LogInformation(
                "Message produced successfully to topic {Topic}, partition {Partition}, offset {Offset}",
                result.Topic, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}: {Error} (Code: {Code})",
                topic, ex.Error.Reason, ex.Error.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to topic {Topic}", topic);
            throw;
        }
    }

    public async Task PublishVideoUploadedAsync(VideoUploadedEvent evt)
    {
        try
        {
            var json = JsonSerializer.Serialize(evt, JsonOptions);

            var msg = new Message<string, string>
            {
                Key = evt.VideoId.ToString(),
                Value = json
            };

            _logger.LogInformation(
                "Publishing video.uploaded event for video {VideoId} to topic {Topic}. Message: {Message}",
                evt.VideoId, _topicVideoUploaded, json);

            var result = await _producer.ProduceAsync(_topicVideoUploaded, msg);

            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));

            _logger.LogInformation(
                "Kafka: Successfully sent event to {Topic} (partition {Partition}, offset {Offset}) for video {VideoId}",
                _topicVideoUploaded,
                result.Partition,
                result.Offset,
                evt.VideoId
            );
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Kafka: ProduceException while sending event for video {VideoId}. Error: {Error}, Code: {Code}, IsFatal: {IsFatal}",
                evt.VideoId, ex.Error.Reason, ex.Error.Code, ex.Error.IsFatal);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka: Unexpected error while sending event for video {VideoId}", evt.VideoId);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _logger.LogInformation("Flushing and disposing Kafka producer");
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing Kafka producer");
        }

        GC.SuppressFinalize(this);
    }
}