namespace AudioMerge.Core.Models
{
    public class AudioTrack
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public double Volume { get; set; } = 1.0;
        public string Language { get; set; }
    }
}