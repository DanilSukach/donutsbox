using File.Service.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoCancellationService>();

builder.Services.AddHostedService<KafkaCancellationConsumerService>();
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddSingleton<MinioService>();
builder.Services.AddSingleton<FfmpegService>();
builder.Services.AddSingleton<KafkaProducerService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();



app.Run();
