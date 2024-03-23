using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using S_Utilities.Utils;

namespace Senxs_Utilities.UI
{
    public partial class Log2Discord_AddEditHandler
    {
        private readonly Action<LogHandler?> _ReturnLogHandler;
        
        public Log2Discord_AddEditHandler(Action<LogHandler?> callbackAction, LogHandler? editHandler = null)
        {
            InitializeComponent();
            _ReturnLogHandler = callbackAction;

            if (editHandler != null)
            {
                NameBox.Text = editHandler.Name;
                WebhookBox.Text = editHandler.DiscordWebHook;
                MinLogLevelBox.Text = editHandler.MinLogLevel.ToString();
                MaxLogLevelBox.Text = editHandler.MaxLogLevel.ToString();
                Roles2PingBox.Text = string.Join(",", editHandler.RolesToPing);
                LogNamesBox.Text = string.Join(",", editHandler.LogNames);
                LogNames_IgnoreBox.Text = string.Join(",", editHandler.LogNames_Ignore);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Verify input
            if (string.IsNullOrEmpty(NameBox.Text))
            {
                MessageBox.Show("Name cannot be empty.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(WebhookBox.Text))
            {
                MessageBox.Show("Webhook cannot be empty.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (!int.TryParse(MinLogLevelBox.Text, out int minLogLevel))
            {
                MessageBox.Show("Min Log Level must be a number.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (!int.TryParse(MaxLogLevelBox.Text, out int maxLogLevel))
            {
                MessageBox.Show("Max Log Level must be a number.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (minLogLevel > maxLogLevel)
            {
                MessageBox.Show("Min Log Level cannot be greater than Max Log Level.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            List<ulong> rolesToPing = new List<ulong>();
            if (!string.IsNullOrEmpty(Roles2PingBox.Text))
            {
                string[] roleIds = Roles2PingBox.Text.Split(',');
                foreach (string roleId in roleIds)
                {
                    if (ulong.TryParse(roleId, out ulong id))
                    {
                        rolesToPing.Add(id);
                    }
                    else
                    {
                        MessageBox.Show("Role ID must be a number.  You can ping multiple roles by separating them with commas... \r\n" +
                            "The Role ID that caused the failure: " + roleId, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            
            List<string> logNames = new List<string>();
            if (!string.IsNullOrEmpty(LogNamesBox.Text))
            {
                string[] names = LogNamesBox.Text.Split(',');
                foreach (string name in names)
                {
                    logNames.Add(name);
                }
            }
            
            List<string> logNamesIgnore = new List<string>();
            if (!string.IsNullOrEmpty(LogNames_IgnoreBox.Text))
            {
                string[] names = LogNames_IgnoreBox.Text.Split(',');
                foreach (string name in names)
                {
                    logNamesIgnore.Add(name);
                }
            }
            
            // Save the handler
            LogHandler log2discordHandler = new LogHandler
            {
                Name = NameBox.Text,
                MinLogLevel = minLogLevel,
                MaxLogLevel = maxLogLevel,
                DiscordWebHook = WebhookBox.Text,
                RolesToPing = rolesToPing,
                LogNames = logNames,
                LogNames_Ignore = logNamesIgnore
            };
            
            _ReturnLogHandler(log2discordHandler);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DragMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Enable moving the window
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}