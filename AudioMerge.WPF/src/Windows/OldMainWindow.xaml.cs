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

namespace AudioMerge.WPF.Windows
{
    public partial class OldMainWindow : Window
    {
        private readonly OldVideoProcessor _processor;
        private readonly ObservableCollection<AudioTrackViewModel> _audioTracks;
        private string _currentFilePath;

        public OldMainWindow()
        {
            InitializeComponent();
            _processor = new OldVideoProcessor();
            _audioTracks = new ObservableCollection<AudioTrackViewModel>();
            AudioTracksPanel.ItemsSource = _audioTracks;
            FilePathText.Text = "Import Video"; // Set initial text
            
        }
        
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "MKV files (*.mkv)|*.mkv",
                Title = "Select a video file"
            };

            if (dialog.ShowDialog() == true)
            {
                await ProcessDroppedFile(dialog.FileName);
            }
        }
        

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).ToLower() == ".mkv")
                {
                    ProcessDroppedFile(files[0]);
                }
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]).ToLower() == ".mkv")
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
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
                _currentFilePath = filePath;
                FilePathText.Text = Path.GetFileName(_currentFilePath);
            
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
                FilePathText.Text = "Import Video"; // Reset text on error
                MessageBox.Show($"Error importing file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFilePath = null;
            FilePathText.Text = "Import Video"; // Reset text
            _audioTracks.Clear();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                if (e.Delta > 0) // Scrolling up
                {
                    if (scrollViewer.VerticalOffset > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 100);
                    }
                }
                else // Scrolling down
                {
                    if (scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight)
                    {
                        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 100);
                    }
                }
                e.Handled = true;
            }
        }

        private void LimitVisibleTracks()
        {
            if (TracksScrollViewer != null)
            {
                const double itemHeight = 100; // Height of each track item
                TracksScrollViewer.MaxHeight = itemHeight * 3;
                TracksScrollViewer.VerticalScrollBarVisibility = 
                    _audioTracks.Count > 3 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("Please import a video file first.", "No File Selected", 
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
                        Filter = "Video files|*.mp4;*.mov;*.mkv",
                        DefaultExt = ".mp4",
                        FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "_processed"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var tracks = _audioTracks.Select(vm => vm.ToAudioTrack()).ToList();
                        await _processor.ProcessVideo(_currentFilePath, saveDialog.FileName, 
                                                   tracks, processDialog.Options);
                        
                        MessageBox.Show("Video processing completed successfully!", "Success", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing video: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        
    }
}