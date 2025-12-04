using System.Diagnostics;

namespace File.Service.Api.Services;

public class AudioProcessingService(ILogger<AudioProcessingService> logger, MinioService minio, IConfiguration config)
{
    // Путь к ffmpeg: из конфига -> из переменной окружения -> из PATH
    private readonly string _ffmpegPath =
        config["FFmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? (OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    public async Task<string> ProcessAudioAsync(Guid audioId, string objectKey, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), audioId.ToString());
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, "input");
        var outputFile = Path.Combine(tempDir, "output.mp3");

        try
        {
            // Check cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            // Скачиваем исходный файл из audio bucket
            await minio.DownloadAudioFileAsync(objectKey, inputFile);

            // Определяем расширение из objectKey или по содержимому
            var ext = Path.GetExtension(objectKey).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                ext = ".wav"; // По умолчанию
            }

            var inputWithExt = Path.ChangeExtension(inputFile, ext);
            if (inputFile != inputWithExt && System.IO.File.Exists(inputFile))
            {
                System.IO.File.Move(inputFile, inputWithExt);
                inputFile = inputWithExt;
            }

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                WorkingDirectory = tempDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Конвертируем/обрабатываем аудио в MP3 с оптимальными настройками
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(inputFile);
            psi.ArgumentList.Add("-codec:a");
            psi.ArgumentList.Add("libmp3lame");
            psi.ArgumentList.Add("-b:a");
            psi.ArgumentList.Add("192k"); // Битрейт 192 kbps для хорошего качества
            psi.ArgumentList.Add("-ar");
            psi.ArgumentList.Add("44100"); // Частота дискретизации
            psi.ArgumentList.Add("-ac");
            psi.ArgumentList.Add("2"); // Стерео
            psi.ArgumentList.Add("-y"); // Перезаписать выходной файл
            psi.ArgumentList.Add(outputFile);

            try
            {
                // Check cancellation before starting ffmpeg
                cancellationToken.ThrowIfCancellationRequested();

                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ffmpeg process.");
                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                // Wait with cancellation support
                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Audio {AudioId} processing cancelled, killing ffmpeg process", audioId);
                    try
                    {
                        process.Kill(true); // Kill process tree
                    }
                    catch { /* ignore */ }
                    throw;
                }

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;

                if (process.ExitCode != 0)
                {
                    logger.LogError("ffmpeg exited with code {ExitCode}. stderr: {StdErr}", process.ExitCode, stdErr);
                    throw new InvalidOperationException($"ffmpeg failed with exit code {process.ExitCode}");
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || 
                                        ex is FileNotFoundException ||
                                        (ex.Message.Contains("No such file") || ex.Message.Contains("not found")))
            {
                logger.LogError(ex, "ffmpeg executable not found at path '{FfmpegPath}'. Set 'FFmpeg:Path' in appsettings or FFMPEG_PATH env var, or add ffmpeg to PATH.", _ffmpegPath);
                throw;
            }

            // Загружаем обработанный файл в MinIO
            var outputKey = $"processed/{audioId}/audio.mp3";
            await minio.UploadProcessedFileAsync(outputKey, outputFile);

            logger.LogInformation("Processed audio {AudioId}", audioId);
            return outputKey;
        }
        finally
        {
            // Всегда пытаемся удалить временную директорию
            await DeleteDirectorySafeAsync(tempDir, logger);
        }
    }

    private static async Task DeleteDirectorySafeAsync(string path, ILogger logger)
    {
        if (!Directory.Exists(path)) return;

        // Снимаем атрибуты (на Windows может блокировать удаление, на Linux игнорируется)
        if (OperatingSystem.IsWindows())
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal); } catch { /* ignore */ }
            }
        }

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (IOException)
            {
                await Task.Delay(500);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(500);
            }
        }

        logger.LogWarning("Could not delete temp dir {Path} after retries; skipping", path);
    }
}

