using System;
using System.Windows;
using System.Windows.Controls;
using S_Utilities.Settings;
using S_Utilities.Utils;

namespace Senxs_Utilities.UI
{
    public partial class Log2Discord_Settings
    {
        private readonly S_Config? Config;
        private readonly Action SaveConfig;
        
        public Log2Discord_Settings(S_Config? config, Action saveMethod)
        {
            InitializeComponent();
            Config = config;
            DataContext = Config;
            SaveConfig = saveMethod;
        }
        
        private void EditHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            Log2Discord_AddEditHandler unused = new Log2Discord_AddEditHandler(AddEditHandlerCallback, (LogHandler) ((Button) sender).DataContext);
        }
        
        private void DeleteHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.DataContext is LogHandler handler)) return;
            
            MessageBoxResult results = MessageBox.Show(
                $"You are about to delete LogHandler [{handler.Name}].\n" + "Are you sure you want to continue?",
                "VERIFY DELETE ACTION", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (results != MessageBoxResult.Yes) return;
            
            Config?.LogHandlers.Remove(handler);
            SaveConfig();
        }
        
        private void AddEditHandlerCallback(LogHandler? handler)
        {
            if (handler == null) return;
            
            if (Config!.LogHandlers.Contains(handler))
            {
                MessageBoxResult result = MessageBox.Show($"A Log2Discord handler already exists with the name {handler.Name}.\nWould you like to overwrite the existing one with this new one?", "Log2Discord Name Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result != MessageBoxResult.Yes) return;
                Config.LogHandlers.Remove(handler);
                Config.LogHandlers.Add(handler);
                return;
            }
            
            Config.LogHandlers.Add(handler);
            SaveConfig();
        }
    }
}