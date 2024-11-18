using AudioMerge.Core.Models;

namespace AudioMerge.Core.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Linq;

    public class OldVideoProcessor
    {
        private string FFmpegPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        private string FFprobePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffprobe.exe");
        
        public async Task<List<AudioTrack>> GetAudioTracks(string inputFile)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input video file not found.");

            var startInfo = new ProcessStartInfo
            {
                FileName = FFprobePath,
                Arguments = $"-v quiet -print_format json -show_streams -select_streams a \"{inputFile}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var tracks = new List<AudioTrack>();
            var jsonDoc = JsonDocument.Parse(output);
            var streams = jsonDoc.RootElement.GetProperty("streams");

            int index = 0;
            foreach (var stream in streams.EnumerateArray())
            {
                var track = new AudioTrack
                {
                    Index = index++,
                    Name = stream.TryGetProperty("tags", out var tags) && 
                           tags.TryGetProperty("title", out var title) ? 
                           title.GetString() : $"Audio Track {index}",
                    Language = stream.TryGetProperty("tags", out var langTags) && 
                              langTags.TryGetProperty("language", out var lang) ? 
                              lang.GetString() : "und"
                };
                tracks.Add(track);
            }

            return tracks;
        }

        public async Task ProcessVideo(
            string inputFile,
            string outputFile,
            List<AudioTrack> audioTracks,
            ProcessingOptions options)
        {
            // Build FFmpeg filter complex for audio mixing
            var filterComplex = new List<string>();
            var audioMaps = new List<string>();
            
            for (int i = 0; i < audioTracks.Count; i++)
            {
                if (audioTracks[i].IsEnabled)
                {
                    filterComplex.Add($"[0:a:{i}]volume={audioTracks[i].Volume}[a{i}]");
                    audioMaps.Add($"[a{i}]");
                }
            }

            // If we have multiple enabled tracks, mix them
            string audioMix = "";
            if (audioMaps.Count > 1)
            {
                audioMix = $"{string.Join("", audioMaps)}amix=inputs={audioMaps.Count}:duration=longest[aout]";
                filterComplex.Add(audioMix);
                audioMaps.Clear();
                audioMaps.Add("[aout]");
            }

            // Apply master volume
            if (options.MasterVolume != 1.0)
            {
                filterComplex.Add($"{audioMaps[0]}volume={options.MasterVolume}[final]");
                audioMaps[0] = "[final]";
            }

            // Build quality parameters based on selected quality
            string qualityParams = options.Quality switch
            {
                VideoQuality.Highest => "-crf 18",
                VideoQuality.Medium => "-crf 23",
                VideoQuality.Lowest => "-crf 28"
            };

            // Build format-specific parameters
            string formatParams = options.OutputFormat switch
            {
                VideoFormat.MP4 => "-c:v libx264 -preset slow -movflags +faststart",
                VideoFormat.MOV => "-c:v libx264 -preset slow -movflags +faststart",
                VideoFormat.MKV => "-c:v libx264 -preset slow"
            };

            var arguments = $"-i \"{inputFile}\" -filter_complex \"{string.Join(";", filterComplex)}\" " +
                          $"-map 0:v:0 -map {audioMaps[0]} {qualityParams} {formatParams} " +
                          $"-c:a aac -b:a 192k \"{outputFile}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = FFmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg process failed with exit code: {process.ExitCode}");
            }
        }
    }
}
