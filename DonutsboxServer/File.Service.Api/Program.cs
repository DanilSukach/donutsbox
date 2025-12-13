using File.Service.Api.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoCancellationService>();
builder.Services.AddSingleton<AudioCancellationService>();

builder.Services.AddHostedService<KafkaCancellationConsumerService>();
builder.Services.AddHostedService<UnifiedKafkaConsumerService>();

builder.Services.AddSingleton<MinioService>();
builder.Services.AddSingleton<FfmpegService>();
builder.Services.AddSingleton<AudioProcessingService>();
builder.Services.AddSingleton<KafkaProducerService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseHttpMetrics();
app.MapMetrics();


app.Run();
