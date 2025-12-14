using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topicVideoProcessed;
    private readonly string _topicAudioProcessed;

    public KafkaProducerService(IConfiguration cfg, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _topicVideoProcessed = cfg["Kafka:Topics:VideoProcessed"]!;
        _topicAudioProcessed = cfg["Kafka:Topics:AudioProcessed"] ?? "audio.processed";
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = cfg["Kafka:BootstrapServers"]
        }).Build();
    }

    public async Task PublishProcessedAsync(VideoProcessedEvent evt)
    {
        try
        {
            var json = JsonSerializer.Serialize(evt);
            var message = new Message<string, string> { Key = evt.VideoId.ToString(), Value = json };
            
            var result = await _producer.ProduceAsync(_topicVideoProcessed, message);
            
            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));
            
            _logger.LogInformation("Published video.processed for {VideoId} (partition {Partition}, offset {Offset})", 
                evt.VideoId, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish video.processed for {VideoId}: {Error} (Code: {Code})",
                evt.VideoId, ex.Error.Reason, ex.Error.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing video.processed for {VideoId}", evt.VideoId);
            throw;
        }
    }

    public async Task PublishAudioProcessedAsync(AudioProcessedEvent evt)
    {
        try
        {
            var json = JsonSerializer.Serialize(evt);
            var message = new Message<string, string> { Key = evt.AudioId.ToString(), Value = json };
            
            var result = await _producer.ProduceAsync(_topicAudioProcessed, message);
            
            // Принудительная отправка
            _producer.Flush(TimeSpan.FromSeconds(10));
            
            _logger.LogInformation("Published audio.processed for {AudioId} (partition {Partition}, offset {Offset})", 
                evt.AudioId, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish audio.processed for {AudioId}: {Error} (Code: {Code})",
                evt.AudioId, ex.Error.Reason, ex.Error.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing audio.processed for {AudioId}", evt.AudioId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
