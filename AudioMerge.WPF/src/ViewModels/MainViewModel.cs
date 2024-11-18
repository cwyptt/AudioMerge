using System;
using System.ComponentModel;
using System.Windows.Input;
using AudioMerge.WPF.Commands;

namespace AudioMerge.WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _filePath = "Import Video";
        
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action ImportRequested;
        public event Action ProcessRequested;
        public event Action ResetRequested;

        public MainViewModel()
        {
            ImportCommand = new RelayCommand(OnImport);
            ProcessCommand = new RelayCommand(OnProcess);
            ResetCommand = new RelayCommand(OnReset);
        }

        public string FilePath
        {
            get => _filePath;
            private set
            {
                _filePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }

        public ICommand ImportCommand { get; }
        public ICommand ProcessCommand { get; }
        public ICommand ResetCommand { get; }

        public void UpdateFilePath(string path)
        {
            FilePath = path;
        }

        private void OnImport()
        {
            ImportRequested?.Invoke();
        }

        private void OnProcess()
        {
            ProcessRequested?.Invoke();
        }

        private void OnReset()
        {
            ResetRequested?.Invoke();
        }
    }
}