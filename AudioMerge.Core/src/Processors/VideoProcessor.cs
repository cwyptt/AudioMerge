using AudioMerge.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace AudioMerge.Core.Processors
{
    public interface IVideoProcessor
    {
        Task<List<AudioTrack>> GetAudioTracks(string inputFile, CancellationToken cancellationToken = default);

        Task ProcessVideo(string inputFile, string outputFile, List<AudioTrack> audioTracks,
            ProcessingOptions options, IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }

    public class VideoProcessor : IVideoProcessor
    {
        private readonly ILogger<VideoProcessor> _logger;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;

        public VideoProcessor(ILogger<VideoProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
            _ffprobePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffprobe.exe");

            ValidateFFmpegInstallation();
        }

        private void ValidateFFmpegInstallation()
        {
            if (!File.Exists(_ffmpegPath))
            {
                _logger.LogError("FFmpeg executable not found at {Path}", _ffmpegPath);
                throw new FileNotFoundException("FFmpeg executable not found.", _ffmpegPath);
            }

            if (!File.Exists(_ffprobePath))
            {
                _logger.LogError("FFprobe executable not found at {Path}", _ffprobePath);
                throw new FileNotFoundException("FFprobe executable not found.", _ffprobePath);
            }

            _logger.LogInformation("FFmpeg installation validated successfully");
        }

        public async Task<List<AudioTrack>> GetAudioTracks(string inputFile,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(inputFile))
                throw new ArgumentNullException(nameof(inputFile));
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input video file not found.", inputFile);

            _logger.LogInformation("Analyzing audio tracks for file: {InputFile}", inputFile);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_streams -select_streams a \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1)); // Timeout after 1 minute

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFprobe failed with error: {Error}", error);
                    throw new InvalidOperationException($"FFprobe failed: {error}");
                }

                var tracks = ParseAudioTracks(output);
                _logger.LogInformation("Found {Count} audio tracks", tracks.Count);
                return tracks;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to analyze audio tracks for file: {InputFile}", inputFile);
                throw new InvalidOperationException("Failed to analyze audio tracks.", ex);
            }
        }

        private List<AudioTrack> ParseAudioTracks(string jsonOutput)
        {
            var tracks = new List<AudioTrack>();

            try
            {
                var jsonDoc = JsonDocument.Parse(jsonOutput);
                var streams = jsonDoc.RootElement.GetProperty("streams");

                int index = 0;
                foreach (var stream in streams.EnumerateArray())
                {
                    var track = new AudioTrack
                    {
                        Index = index++,
                        Name = GetStreamProperty(stream, "tags", "title") ?? $"Audio Track {index}",
                        Language = GetStreamProperty(stream, "tags", "language") ?? "und",
                        IsEnabled = true,
                        Volume = 1.0
                    };
                    tracks.Add(track);

                    _logger.LogDebug("Parsed audio track: Index={Index}, Name={Name}, Language={Language}",
                        track.Index, track.Name, track.Language);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse FFprobe output");
                throw new InvalidOperationException("Failed to parse audio track information.", ex);
            }

            return tracks;
        }

        private static string? GetStreamProperty(JsonElement stream, params string[] propertyPath)
        {
            JsonElement current = stream;
            foreach (var prop in propertyPath)
            {
                if (!current.TryGetProperty(prop, out current))
                    return null;
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }

        public async Task ProcessVideo(
            string inputFile,
            string outputFile,
            List<AudioTrack> audioTracks,
            ProcessingOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ValidateProcessingParameters(inputFile, outputFile, audioTracks, options);

            _logger.LogInformation("Starting video processing: Input={InputFile}, Output={OutputFile}",
                inputFile, outputFile);

            var (filterComplex, audioMaps) = BuildAudioFilterChain(audioTracks, options);
            string arguments = BuildFFmpegArguments(inputFile, outputFile, filterComplex, audioMaps, options);

            _logger.LogDebug("FFmpeg arguments: {Arguments}", arguments);

            try
            {
                await ExecuteFFmpegProcess(arguments, progress, cancellationToken);
                _logger.LogInformation("Video processing completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Video processing was cancelled");
                CleanupIncompleteFile(outputFile);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video processing failed");
                CleanupIncompleteFile(outputFile);
                throw new InvalidOperationException("Video processing failed. See inner exception for details.", ex);
            }
        }

        private void ValidateProcessingParameters(string inputFile, string outputFile, List<AudioTrack> audioTracks,
            ProcessingOptions options)
        {
            if (string.IsNullOrEmpty(inputFile))
                throw new ArgumentNullException(nameof(inputFile));
            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentNullException(nameof(outputFile));
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input video file not found.", inputFile);
            if (audioTracks == null || !audioTracks.Any())
                throw new ArgumentException("At least one audio track must be provided.", nameof(audioTracks));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.MasterVolume <= 0 || options.MasterVolume > 10)
                throw new ArgumentOutOfRangeException(nameof(options), "Master volume must be between 0 and 10.");

            var outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                _logger.LogInformation("Creating output directory: {Directory}", outputDir);
                Directory.CreateDirectory(outputDir);
            }
        }

        private (string filterComplex, List<string> audioMaps) BuildAudioFilterChain(List<AudioTrack> audioTracks,
            ProcessingOptions options)
        {
            var filterComplex = new List<string>();
            var audioMaps = new List<string>();

            // Build individual track processing
            for (int i = 0; i < audioTracks.Count; i++)
            {
                if (audioTracks[i].IsEnabled)
                {
                    filterComplex.Add($"[0:a:{i}]volume={audioTracks[i].Volume:F2}[a{i}]");
                    audioMaps.Add($"[a{i}]");
                    _logger.LogDebug("Added audio track {Index} with volume {Volume}", i, audioTracks[i].Volume);
                }
            }

            // Mix tracks if needed
            if (audioMaps.Count > 1)
            {
                filterComplex.Add($"{string.Join("", audioMaps)}amix=inputs={audioMaps.Count}:duration=longest[aout]");
                audioMaps.Clear();
                audioMaps.Add("[aout]");
                _logger.LogDebug("Mixing {Count} audio tracks", audioMaps.Count);
            }

            // Apply master volume
            if (Math.Abs(options.MasterVolume - 1.0) > 0.01)
            {
                filterComplex.Add($"{audioMaps[0]}volume={options.MasterVolume:F2}[final]");
                audioMaps[0] = "[final]";
                _logger.LogDebug("Applied master volume: {Volume}", options.MasterVolume);
            }

            return (string.Join(";", filterComplex), audioMaps);
        }

        private string BuildFFmpegArguments(string inputFile, string outputFile, string filterComplex,
            List<string> audioMaps, ProcessingOptions options)
        {
            string qualityParams = options.Quality switch
            {
                VideoQuality.Highest => "-crf 18 -preset slow",
                VideoQuality.Medium => "-crf 23 -preset medium",
                VideoQuality.Lowest => "-crf 28 -preset fast",
                _ => throw new ArgumentException("Invalid video quality option.")
            };

            string formatParams = options.OutputFormat switch
            {
                VideoFormat.MP4 => "-c:v libx264 -movflags +faststart",
                VideoFormat.MOV => "-c:v libx264 -movflags +faststart",
                VideoFormat.MKV => "-c:v libx264",
                _ => throw new ArgumentException("Invalid output format option.")
            };

            _logger.LogDebug("Quality parameters: {QualityParams}", qualityParams);
            _logger.LogDebug("Format parameters: {FormatParams}", formatParams);

            return $"-i \"{inputFile}\" -filter_complex \"{filterComplex}\" " +
                   $"-map 0:v:0 -map {audioMaps[0]} {qualityParams} {formatParams} " +
                   $"-c:a aac -b:a 192k -y \"{outputFile}\"";
        }

        private async Task ExecuteFFmpegProcess(string arguments, IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            var durationTicks = await GetVideoDuration(startInfo.Arguments);
            var progressRegex = new System.Text.RegularExpressions.Regex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogTrace("FFmpeg output: {Output}", e.Data);

                    var match = progressRegex.Match(e.Data);
                    if (match.Success && progress != null && durationTicks > 0)
                    {
                        var hours = int.Parse(match.Groups[1].Value);
                        var minutes = int.Parse(match.Groups[2].Value);
                        var seconds = int.Parse(match.Groups[3].Value);
                        var hundredths = int.Parse(match.Groups[4].Value);

                        var currentTicks = new TimeSpan(0, hours, minutes, seconds, hundredths * 10).Ticks;
                        var progressValue = (double)currentTicks / durationTicks;
                        progress.Report(Math.Min(1.0, Math.Max(0.0, progressValue)));
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        _logger.LogInformation("Cancelling FFmpeg process");
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while attempting to kill FFmpeg process");
                }
            });

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg process failed with exit code: {process.ExitCode}");
            }
        }

        private async Task<long> GetVideoDuration(string inputFile)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_format \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var jsonDoc = JsonDocument.Parse(output);
                var duration = jsonDoc.RootElement
                    .GetProperty("format")
                    .GetProperty("duration")
                    .GetString();

                if (double.TryParse(duration, out var seconds))
                {
                    return TimeSpan.FromSeconds(seconds).Ticks;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get video duration. Progress reporting may be inaccurate.");
            }

            return 0;
        }

        private void CleanupIncompleteFile(string outputFile)
        {
            try
            {
                if (File.Exists(outputFile))
                {
                    _logger.LogInformation("Cleaning up incomplete output file: {OutputFile}", outputFile);
                    File.Delete(outputFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup incomplete output file: {OutputFile}", outputFile);
            }
        }
    }
}
