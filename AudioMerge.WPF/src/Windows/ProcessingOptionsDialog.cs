using System;
using System.Windows;
using System.Windows.Controls;
using AudioMerge.Core.Models;

namespace AudioMerge.WPF.Windows
{
    public class ProcessingOptionsDialog : Window
    {
        public ProcessingOptions Options { get; private set; }

        public ProcessingOptionsDialog()
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = "Processing Options";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            Options = new ProcessingOptions();

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Format Selection
            var formatLabel = new TextBlock 
            { 
                Text = "Output Format:", 
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Bold 
            };
            Grid.SetRow(formatLabel, 0);

            var formatCombo = new ComboBox
            {
                ItemsSource = Enum.GetValues(typeof(VideoFormat)),
                SelectedItem = Options.OutputFormat,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(formatCombo, 1);

            // Quality Selection
            var qualityLabel = new TextBlock 
            { 
                Text = "Quality:", 
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Bold 
            };
            Grid.SetRow(qualityLabel, 2);

            var qualityCombo = new ComboBox
            {
                ItemsSource = Enum.GetValues(typeof(VideoQuality)),
                SelectedItem = Options.Quality,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(qualityCombo, 3);

            // Volume Slider
            var volumeLabel = new TextBlock 
            { 
                Text = "Output Volume:", 
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Bold 
            };
            Grid.SetRow(volumeLabel, 4);

            var volumeSlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = Options.MasterVolume,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(volumeSlider, 5);

            // Volume percentage display
            var volumePercentage = new TextBlock
            {
                Text = $"{Options.MasterVolume * 100:F0}%",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(volumePercentage, 6);

            // Update percentage when slider changes
            volumeSlider.ValueChanged += (s, e) =>
            {
                volumePercentage.Text = $"{volumeSlider.Value * 100:F0}%";
            };

            // Button Panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            Grid.SetRow(buttonPanel, 7);

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            
            okButton.Click += (s, e) =>
            {
                Options.OutputFormat = (VideoFormat)formatCombo.SelectedItem;
                Options.Quality = (VideoQuality)qualityCombo.SelectedItem;
                Options.MasterVolume = volumeSlider.Value;
                DialogResult = true;
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                IsCancel = true
            };
            
            cancelButton.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            // Add all controls to grid
            grid.Children.Add(formatLabel);
            grid.Children.Add(formatCombo);
            grid.Children.Add(qualityLabel);
            grid.Children.Add(qualityCombo);
            grid.Children.Add(volumeLabel);
            grid.Children.Add(volumeSlider);
            grid.Children.Add(volumePercentage);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}