/*
  Copyright © 2018 ASCON-Design Systems LLC. All rights reserved.
  This sample is licensed under the MIT License.
*/
using System.Windows;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    public partial class GraphicLayerElementSettingsView
    {
        public GraphicLayerElementSettingsView()
        {
            InitializeComponent();
        }

        private void OnSaveButtonClicked(object sender, RoutedEventArgs e)
        {
            
                
            var model = DataContext as GraphicLayerElementSettingsModel;
            var includeStamp = true;


            model.SaveSettings(PathButtonEdit.Text, TxbScale.Text, TxbAngle.Text, includeStamp);
            Close();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}