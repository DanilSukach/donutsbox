using System.Diagnostics;

namespace File.Service.Api.Services;

public class FfmpegService(ILogger<FfmpegService> logger, MinioService minio, IConfiguration config)
{
    // Путь к ffmpeg: из конфига -> из переменной окружения -> из PATH
    private readonly string _ffmpegPath =
        config["FFmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? (OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    public async Task<string> ProcessVideoAsync(Guid videoId, string objectKey)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), videoId.ToString());
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, "input.mp4");
        var outputDir = Path.Combine(tempDir, "hls");
        Directory.CreateDirectory(outputDir);

        var outputIndex = Path.Combine(outputDir, "index.m3u8");

        try
        {
            // Скачиваем исходный файл
            await minio.DownloadFileAsync(objectKey, inputFile);

            var segmentFilename = Path.Combine(outputDir, "segment%03d.ts");
            
            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                WorkingDirectory = tempDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Используем ArgumentList для кроссплатформенности (правильная обработка пробелов в путях)
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(inputFile);
            psi.ArgumentList.Add("-c:v");
            psi.ArgumentList.Add("libx264");
            psi.ArgumentList.Add("-pix_fmt");
            psi.ArgumentList.Add("yuv420p");
            psi.ArgumentList.Add("-c:a");
            psi.ArgumentList.Add("aac");
            psi.ArgumentList.Add("-b:a");
            psi.ArgumentList.Add("128k");
            psi.ArgumentList.Add("-ar");
            psi.ArgumentList.Add("44100");
            psi.ArgumentList.Add("-profile:v");
            psi.ArgumentList.Add("high");
            psi.ArgumentList.Add("-level");
            psi.ArgumentList.Add("4.0");
            psi.ArgumentList.Add("-preset");
            psi.ArgumentList.Add("fast");
            psi.ArgumentList.Add("-movflags");
            psi.ArgumentList.Add("+faststart");
            psi.ArgumentList.Add("-hls_time");
            psi.ArgumentList.Add("6");
            psi.ArgumentList.Add("-hls_list_size");
            psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("-hls_segment_filename");
            psi.ArgumentList.Add(segmentFilename);
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("hls");
            psi.ArgumentList.Add(outputIndex);

            try
            {
                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ffmpeg process.");
                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

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

            // Загружаем результат
            await minio.UploadFolderAsync($"processed/{videoId}", outputDir);

            logger.LogInformation("Processed video {VideoId}", videoId);
            return $"processed/{videoId}/index.m3u8";
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