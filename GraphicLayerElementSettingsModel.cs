/*
  Copyright © 2018 ASCON-Design Systems LLC. All rights reserved.
  This sample is licensed under the MIT License.
*/
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Ascon.Pilot.SDK.Controls.Commands;
using Microsoft.Win32;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    public class GraphicLayerElementSettingsModel : INotifyPropertyChanged
    {
        private readonly DelegateCommand _selectImageCommand;
        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set            
            {
                _filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        private bool _includeStamp;
        public bool IncludeStamp
        {
            get => _includeStamp;
            set
            {
                _includeStamp = value;
                OnPropertyChanged("IncludeStamp");
            }
        }

        public string Scale { get; set; }
        public string Angle { get; set; }

        public ICommand SelectImageCommand
        {
            get => _selectImageCommand;
        }

        public event EventHandler OnSaveSettings;

        public GraphicLayerElementSettingsModel(string filePath, string scale, string angle, 
           bool includeStamp)
        {
            FilePath = filePath;
            Scale = scale;
            Angle = angle;
            IncludeStamp = includeStamp;
            _selectImageCommand = new DelegateCommand(ShowDialog);
        }

        private void ShowDialog()
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg"
            };

            if (dialog.ShowDialog() == true)
                FilePath = dialog.FileName;
        }

        public void SaveSettings(string path, string scale, string angle, bool includeStamp)
        {
            FilePath = path;
            Scale = scale;
            Angle = angle;
            Properties.Settings.Default.Path = path;
            
            Properties.Settings.Default.Scale = Scale;
            Properties.Settings.Default.Angle = Angle;
            Properties.Settings.Default.IncludeStamp = includeStamp;
            Properties.Settings.Default.Save();

            var handler = OnSaveSettings;
            if (handler != null)
                handler.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}