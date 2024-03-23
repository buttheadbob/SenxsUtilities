using System;
using System.Windows.Controls;
using S_Utilities.Settings;

namespace Senxs_Utilities.UI
{
    public partial class Ejector_Settings : UserControl
    {
        private readonly S_Config? Config;
        private readonly Action SaveConfig;
        
        public Ejector_Settings(S_Config? config, Action saveMethod)
        {
            InitializeComponent();
            Config = config;
            DataContext = Config;
            SaveConfig = saveMethod;
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveConfig();
        }
    }
}