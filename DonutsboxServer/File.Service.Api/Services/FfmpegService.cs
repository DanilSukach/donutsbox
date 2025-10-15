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

            var ffmpegArgs = string.Join(" ",
            [
                "-i", $"\"{inputFile}\"",
                "-c:v", "libx264",
                "-pix_fmt", "yuv420p",      
                "-c:a", "aac",
                "-b:a", "128k",
                "-ar", "44100",
                "-profile:v", "high",            
                "-level", "4.0",
                "-preset", "fast",
                "-movflags", "+faststart",
                "-hls_time", "6",
                "-hls_list_size", "0",
                "-hls_segment_filename", $"\"{Path.Combine(outputDir, "segment%03d.ts")}\"",
                "-f", "hls",
                $"\"{outputIndex}\""
            ]);

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = ffmpegArgs,
                WorkingDirectory = tempDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

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
            catch (System.ComponentModel.Win32Exception)
            {
                logger.LogError("ffmpeg executable not found. Set 'FFmpeg:Path' in appsettings or FFMPEG_PATH env var, or add ffmpeg to PATH.");
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

        // Снимаем атрибуты (на Windows может блокировать удаление)
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try { System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal); } catch { /* ignore */ }
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