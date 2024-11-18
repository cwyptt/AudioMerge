using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioMerge.Core.Models;
using AudioMerge.Core.Processors;
using AudioMerge.WPF.ViewModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AudioMerge.WPF.Windows
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IVideoProcessor _processor;
        private readonly ILogger<VideoProcessor> _logger;
        private readonly ObservableCollection<AudioTrackViewModel> _audioTracks;
        private string _currentFilePath;

        public MainWindow()
        {
            InitializeComponent();

            // Setup logging
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<VideoProcessor>();
            
            // Initialize components
            _processor = new VideoProcessor(_logger);
            _audioTracks = new ObservableCollection<AudioTrackViewModel>();
            AudioTracksPanel.ItemsSource = _audioTracks;
            
            // Setup ViewModel
            _viewModel = new MainViewModel();
            _viewModel.ImportRequested += HandleImportRequest;
            _viewModel.ProcessRequested += HandleProcessRequest;
            _viewModel.ResetRequested += HandleResetRequest;
            DataContext = _viewModel;
        }

        private void HandleImportRequest()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Video files|*.mkv;*.mp4;*.mov",
                Title = "Select a video file"
            };

            if (dialog.ShowDialog() == true)
            {
                ProcessDroppedFile(dialog.FileName);
            }
        }

        private async void HandleProcessRequest()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("Please import a video file first.", "No File Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_audioTracks.Any(t => t.IsEnabled))
            {
                MessageBox.Show("Please enable at least one audio track.", "No Tracks Enabled", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var processDialog = new ProcessingOptionsDialog();
            if (processDialog.ShowDialog() == true)
            {
                try
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "MP4 Video|*.mp4|MOV Video|*.mov|MKV Video|*.mkv",
                        DefaultExt = ".mp4",
                        FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "_processed"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        ProcessingProgress.Visibility = Visibility.Visible;
                        ProcessingProgress.Value = 0;
                        
                        var tracks = _audioTracks.Select(vm => vm.ToAudioTrack()).ToList();
                        var progress = new Progress<double>(value => ProcessingProgress.Value = value * 100);

                        await _processor.ProcessVideo(
                            _currentFilePath, 
                            saveDialog.FileName, 
                            tracks, 
                            processDialog.Options,
                            progress);
                        
                        MessageBox.Show("Video processing completed successfully!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing video: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ProcessingProgress.Visibility = Visibility.Collapsed;
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void HandleResetRequest()
        {
            _currentFilePath = null;
            _audioTracks.Clear();
            ProcessingProgress.Value = 0;
            ProcessingProgress.Visibility = Visibility.Collapsed;
            _viewModel.UpdateFilePath("Import Video");
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".mkv" || ext == ".mp4" || ext == ".mov")
                    {
                        ProcessDroppedFile(files[0]);
                    }
                }
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".mkv" || ext == ".mp4" || ext == ".mov")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            Window_Drop(sender, e);
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            Window_DragOver(sender, e);
        }

        private async Task ProcessDroppedFile(string filePath)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ProcessingProgress.Visibility = Visibility.Visible;
                ProcessingProgress.IsIndeterminate = true;
                
                _currentFilePath = filePath;
                _viewModel.UpdateFilePath(Path.GetFileName(_currentFilePath));
            
                var tracks = await _processor.GetAudioTracks(_currentFilePath);
                _audioTracks.Clear();
            
                foreach (var track in tracks)
                {
                    _audioTracks.Add(new AudioTrackViewModel(track));
                }

                LimitVisibleTracks();
            }
            catch (Exception ex)
            {
                _viewModel.UpdateFilePath("Import Video");
                MessageBox.Show($"Error importing file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProcessingProgress.Visibility = Visibility.Collapsed;
                ProcessingProgress.IsIndeterminate = false;
                Mouse.OverrideCursor = null;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                if (e.Delta > 0)
                {
                    scrollViewer.LineUp();
                    scrollViewer.LineUp();
                }
                else
                {
                    scrollViewer.LineDown();
                    scrollViewer.LineDown();
                }
                e.Handled = true;
            }
        }

        private void LimitVisibleTracks()
        {
            if (TracksScrollViewer != null)
            {
                const double itemHeight = 100; // Height of each track item
                TracksScrollViewer.MaxHeight = Math.Min(itemHeight * 3, itemHeight * _audioTracks.Count);
                TracksScrollViewer.VerticalScrollBarVisibility = 
                    _audioTracks.Count > 3 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            }
        }
    }
}