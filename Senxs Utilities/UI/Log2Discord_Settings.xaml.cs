using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NLog;
using S_Utilities.Settings;
using S_Utilities.Utils;

namespace Senxs_Utilities.UI
{
    public partial class Log2Discord_Settings
    {
        private readonly S_Config? Config;
        private readonly Action SaveConfig;
        private bool _spamlog = false;
        
        // Do not use this for normal logging!!!
        private static readonly Logger Log = LogManager.GetLogger("Test Logger");
        
        public Log2Discord_Settings(S_Config? config, Action saveMethod)
        {
            InitializeComponent();
            Config = config;
            DataContext = Config;
            SaveConfig = saveMethod;
            LogLevelInfo.Text = "LogLevels: [0 \u2192 Trace]    [1 \u2192 Debug]    [2 \u2192 Info]    [3 \u2192 Warn]    [4 \u2192 Error]    [5 \u2192 Fatal]";
        }
        
        private void EditHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Image button) return;
            if (button.DataContext is not LogHandler handler) return;
            
            Log2Discord_AddEditHandler unused = new(AddEditHandlerCallback, handler);
            unused.Show();
        }
        
        private void DeleteHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Image button) return;
            if (button.DataContext is not LogHandler handler) return;
            
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
            
            if (Config!.LogHandlers.Any(x => x.Name == handler.Name))
            {
                MessageBoxResult result = MessageBox.Show($"A Log2Discord handler already exists with the name {handler.Name}.\nWould you like to overwrite the existing one with this new one?", "Log2Discord Name Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result != MessageBoxResult.Yes) return;
                LogHandler[] tempLogHandlers = Config.LogHandlers.ToArray();
                for (int index = tempLogHandlers.Length - 1; index >= 0; index--)
                {
                    LogHandler logHandler = tempLogHandlers[index];
                    if (logHandler.Name == handler.Name)
                        Config.LogHandlers.Remove(logHandler);
                }

                Config.LogHandlers.Add(handler);
                SaveConfig();
                return;
            }
            
            Config.LogHandlers.Add(handler);
            SaveConfig();
        }

        private void CreateNewHandler_ButtonClick(object sender, RoutedEventArgs e)
        {
            Log2Discord_AddEditHandler unused = new(AddEditHandlerCallback);
            unused.Show();
        }

        private void CreateTestLog_ButtonClick(object? sender, RoutedEventArgs? e)
        {
            Log.Info("Creating test log events:");
            Log.Debug("Test Debug Log");
            Log.Trace("Test Trace Log");
            Log.Info("Test Info Log");
            Log.Warn("Test Warn Log");
            Log.Error("Test Error Log");
            Log.Fatal("Test Fatal Log");
        }

        private async void START_CreateTestLogSPAM_ButtonClick(object sender, RoutedEventArgs e)
        {
            _spamlog = true;
            while (_spamlog)
            {
                for (int x = 0; x < 50; x++)
                    CreateTestLog_ButtonClick(null, null);
                await Sleeper(5);
            }
        }
        
        private void STOP_CreateTestLogSPAM_ButtonClick(object sender, RoutedEventArgs e)
        {
            _spamlog = false;
        }

        private static async Task Sleeper(int seconds)
        {
            await Task.Delay(seconds * 1000);
        }
    }
}