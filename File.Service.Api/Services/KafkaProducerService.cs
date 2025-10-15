using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topic;

    public KafkaProducerService(IConfiguration cfg, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _topic = cfg["Kafka:Topics:VideoProcessed"]!;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = cfg["Kafka:BootstrapServers"]
        }).Build();
    }

    public async Task PublishProcessedAsync(VideoProcessedEvent evt)
    {
        var json = JsonSerializer.Serialize(evt);
        await _producer.ProduceAsync(_topic, new Message<string, string> { Key = evt.VideoId.ToString(), Value = json });
        _logger.LogInformation("Published video.processed for {VideoId}", evt.VideoId);
    }

    public void Dispose() => _producer.Dispose();
}
