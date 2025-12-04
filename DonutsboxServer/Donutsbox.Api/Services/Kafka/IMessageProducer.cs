namespace Donutsbox.Api.Services.Kafka;

public interface IMessageProducer
{
    void Dispose();
    Task PublishVideoUploadedAsync(VideoUploadedEvent evt);
    Task PublishVideoProcessingCancelledAsync(VideoProcessingCancelledEvent evt);
    Task PublishAudioUploadedAsync(AudioUploadedEvent evt);
}
