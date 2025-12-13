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
        var json = JsonSerializer.Serialize(evt);
        await _producer.ProduceAsync(_topicVideoProcessed, new Message<string, string> { Key = evt.VideoId.ToString(), Value = json });
        _logger.LogInformation("Published video.processed for {VideoId}", evt.VideoId);
    }

    public async Task PublishAudioProcessedAsync(AudioProcessedEvent evt)
    {
        var json = JsonSerializer.Serialize(evt);
        await _producer.ProduceAsync(_topicAudioProcessed, new Message<string, string> { Key = evt.AudioId.ToString(), Value = json });
        _logger.LogInformation("Published audio.processed for {AudioId}", evt.AudioId);
    }

    public void Dispose()
    {
        _producer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
