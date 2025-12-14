using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Donutsbox.Api.Services.Kafka;

public class KafkaMessageProducer : IMessageProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessageProducer> _logger;
    private readonly string _topicVideoUploaded;
    private readonly string _topicVideoProcessingCancelled;
    private readonly string _topicAudioUploaded;

    // Кэшированные JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaMessageProducer(IConfiguration configuration, ILogger<KafkaMessageProducer> logger)
    {
        _logger = logger;
        _topicVideoUploaded = configuration["Kafka:Topics:VideoUploaded"] ?? "video.uploaded";
        _topicVideoProcessingCancelled = configuration["Kafka:Topics:VideoProcessingCancelled"] ?? "video.processing.cancelled";
        _topicAudioUploaded = configuration["Kafka:Topics:AudioUploaded"] ?? "audio.uploaded";

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All,  // Ждем подтверждения от всех реплик
            EnableIdempotence = true,  // Включаем идемпотентность для надежности
            MessageTimeoutMs = 30000,  // 30 секунд
            RequestTimeoutMs = 30000,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 1000, // Увеличиваем задержку между retry до 1 секунды

            // Для немедленной отправки без буферизации
            BatchSize = 1,  // Отправлять сразу
            LingerMs = 0,   // Без задержки

            CompressionType = CompressionType.None,
            SocketTimeoutMs = 60000,
            ApiVersionRequestTimeoutMs = 10000,
            
            // Настройки для автоматического переподключения
            ReconnectBackoffMs = 1000, // Начальная задержка переподключения: 1 секунда
            ReconnectBackoffMaxMs = 10000, // Максимальная задержка переподключения: 10 секунд
            SocketKeepaliveEnable = true, // Включаем keepalive для поддержания соединения
            MetadataMaxAgeMs = 300000, // 5 минут - максимальный возраст метаданных

            // Добавим логирование для отладки
            Debug = "broker,topic,msg"
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
            {
                // Логируем ошибки подключения как предупреждения, а не ошибки
                if (e.Code == ErrorCode.Local_Transport)
                {
                    _logger.LogWarning("Kafka Producer connection issue: {Reason} (Code: {Code}). Will retry automatically.", 
                        e.Reason, e.Code);
                }
                else if (e.IsFatal)
                {
                    _logger.LogError("Kafka Producer Fatal Error: {Reason} (Code: {Code})", e.Reason, e.Code);
                }
                else
                {
                    _logger.LogWarning("Kafka Producer Error: {Reason} (Code: {Code})", e.Reason, e.Code);
                }
            })
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

    public async Task PublishVideoProcessingCancelledAsync(VideoProcessingCancelledEvent evt)
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
                "Publishing video.processing.cancelled event for video {VideoId} to topic {Topic}. Message: {Message}",
                evt.VideoId, _topicVideoProcessingCancelled, json);

            var result = await _producer.ProduceAsync(_topicVideoProcessingCancelled, msg);

            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));

            _logger.LogInformation(
                "Kafka: Successfully sent cancellation event to {Topic} (partition {Partition}, offset {Offset}) for video {VideoId}",
                _topicVideoProcessingCancelled,
                result.Partition,
                result.Offset,
                evt.VideoId
            );
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Kafka: ProduceException while sending cancellation event for video {VideoId}. Error: {Error}, Code: {Code}, IsFatal: {IsFatal}",
                evt.VideoId, ex.Error.Reason, ex.Error.Code, ex.Error.IsFatal);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka: Unexpected error while sending cancellation event for video {VideoId}", evt.VideoId);
            throw;
        }
    }

    public async Task PublishAudioUploadedAsync(AudioUploadedEvent evt)
    {
        try
        {
            var json = JsonSerializer.Serialize(evt, JsonOptions);

            var msg = new Message<string, string>
            {
                Key = evt.AudioId.ToString(),
                Value = json
            };

            _logger.LogInformation(
                "Publishing audio.uploaded event for audio {AudioId} to topic {Topic}. Message: {Message}",
                evt.AudioId, _topicAudioUploaded, json);

            var result = await _producer.ProduceAsync(_topicAudioUploaded, msg);

            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));

            _logger.LogInformation(
                "Kafka: Successfully sent event to {Topic} (partition {Partition}, offset {Offset}) for audio {AudioId}",
                _topicAudioUploaded,
                result.Partition,
                result.Offset,
                evt.AudioId
            );
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Kafka: ProduceException while sending event for audio {AudioId}. Error: {Error}, Code: {Code}, IsFatal: {IsFatal}",
                evt.AudioId, ex.Error.Reason, ex.Error.Code, ex.Error.IsFatal);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka: Unexpected error while sending event for audio {AudioId}", evt.AudioId);
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