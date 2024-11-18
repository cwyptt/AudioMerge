using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioMerge.Core.Models;

namespace AudioMerge.WPF.ViewModels
{
    public class AudioTrackViewModel : INotifyPropertyChanged
    {
        private AudioTrack _track;
        private bool _showVolumeSlider = true;  // Initialize to true

        public AudioTrackViewModel(AudioTrack track)
        {
            _track = track;
        }

        public string Name
        {
            get => _track.Name;
            set
            {
                _track.Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool IsEnabled
        {
            get => _track.IsEnabled;
            set
            {
                _track.IsEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public double Volume
        {
            get => _track.Volume;
            set
            {
                _track.Volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public bool ShowVolumeSlider
        {
            get => _showVolumeSlider;
            set
            {
                _showVolumeSlider = value;
                OnPropertyChanged(nameof(ShowVolumeSlider));
            }
        }

        public AudioTrack ToAudioTrack() => _track;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}