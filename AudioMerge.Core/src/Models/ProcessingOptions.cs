namespace AudioMerge.Core.Models
{
    public enum VideoQuality
    {
        Highest,
        Medium,
        Lowest
    }

    public enum VideoFormat
    {
        MP4,
        MOV,
        MKV
    }

    public class ProcessingOptions
    {
        public VideoFormat OutputFormat { get; set; } = VideoFormat.MP4;
        public VideoQuality Quality { get; set; } = VideoQuality.Highest;
        public double MasterVolume { get; set; } = 1.0;
    }
}