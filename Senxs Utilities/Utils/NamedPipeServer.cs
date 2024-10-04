using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using S_Utilities.Settings;
using Sandbox.Game;
using Sandbox.Game.World;

namespace S_Utilities.Utils
{
    public static class NamedPipeServer
    {
        private static readonly Logger Log = LogManager.GetLogger("SenX Utilities: Local Command Server");
        public static byte Status { get; private set; }  // 0 == Stopped, 1 == Running, 3 == StopRequested, 4 == Error
        public static CancellationTokenSource cts = new();
        public static Action<byte>? GUI_StatusUpdater { get; set; }
        private static S_Config? Config => Senxs_Utilities.Config;
        private static bool CancelRestart = false;
        
        public static void StartServer()
        {
            if (string.IsNullOrWhiteSpace(Senxs_Utilities.InstName))
            {
                Log.Warn("No instance name found. Cannot start server.  Please edit the Torch.cfg file and set the instance name.  Each instance needs a unique name.");
                return;
            }
            
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
            
            Task.Run(() => _StartServer(Senxs_Utilities.InstName));
        }
        
        public static string GetPipeName() => Senxs_Utilities.InstName;

        public static void StopServer()
        {
            Status = 3;
            cts.Cancel();
            GUI_StatusUpdater?.Invoke(Status);
        }

        private static async Task _StartServer(string pipeName)
        {
            // See if one already exists...
            if (PipeExistsAlready(pipeName))
            {
                Log.Warn("Pipe already exists. Cannot start server.  Do you have another Torch running with the same instance name?");
                GUI_StatusUpdater?.Invoke(4);
                return;
            }

            Log.Info($"Pipe Server started on {pipeName}");
            Status = 1;
            GUI_StatusUpdater?.Invoke(Status);
            
            while (Status == 1)
            {
                using NamedPipeServerStream server = new(pipeName, PipeDirection.InOut);
                await server.WaitForConnectionAsync(cts.Token);
                if (!server.IsConnected) continue;

                try
                {
                    byte[] buffer = new byte[4096]; // Adjust buffer size as needed
                    StringBuilder message = new ();
                    int bytesRead;

                    // Read the stream directly
                    while ((bytesRead = await server.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        message.Append(text);

                        if (text.Contains("\n")) 
                        {
                            string completedMessage = message.ToString();
                            RunMessagedCommand(completedMessage.Trim());
                            message.Clear();
                            server.Disconnect();
                            break;
                        }
                    }
                }
                catch (IOException e)
                {
                    GUI_StatusUpdater?.Invoke(4);
                    Log.Error($"ERROR: {e.Message} => {e.InnerException} => {e.StackTrace}");
                }
                finally
                {
                    if (server.IsConnected)
                    {
                        server.Disconnect();
                    }
                }
            }
            
            Status = 0;
            GUI_StatusUpdater?.Invoke(0);
            Log.Info("Pipe Server stopped");
        }

        private static bool PipeExistsAlready(string pipeName)
        {
            try
            {
                using NamedPipeClientStream client = new (pipeName);
                client.Connect(250); // Use a short timeout to avoid hanging
                return true; // If no exception is thrown, the pipe exists
            }
            catch (TimeoutException)
            {
                return false; // Couldn't connect/find the pipe, so it doesn't exist... hopefully.
            }
            catch (IOException)
            {
                Log.Warn("IO Exception could indicate that the pipe is being connected to or disconnected by another process");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Log.Info("It likely means the pipe exists but we lack permission.  Cannot start server.  Please edit your Torch.cfg file and set a unique instance name.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info("Exception occurred" + ex.Message + ex.InnerException + ex.StackTrace);
                return false;
            }
        }
        
        private static void RunMessagedCommand(string command)
        {
            Log.Info($"Received command: {command}");

            switch (command)
            {
                case "RestartTorchStoppedState":
                    Task.Run(RestartTorchStoppedState) ;
                    break;
                
                case "RestartTorchRunningState":
                    Task.Run(RestartTorchRunningState);
                    break;
                
                case "SaveAndCloseTorch":
                    Task.Run(SaveAndCloseTorch);
                    break;

                case "RestartTorchStoppedStateNOW":
                    Task.Run(RestartTorchStoppedStateNOW);
                    break;

                case "RestartTorchRunningStateNOW": 
                    Task.Run(RestartTorchRunningStateNOW);
                    break;

                case "SaveAndCloseTorchNOW":
                    Task.Run(SaveAndCloseTorchNOW);
                    break;
                
                case "CancelRestart":
                    Config!.RestartState = 0;
                    RunCommandManager.ProcessCommand($"!restart cancel");
                    CancelRestart = true;
                    Log.Info("Restart Cancelled.");
                    break;
                
                case "Ping":
                    Log.Info("Ping received");
                    break;
                
                default:
                    StringBuilder sb = new();
                    sb.AppendLine();
                    sb.AppendLine("Unknown command received. Ignoring.");
                    sb.AppendLine("Command: " + command);
                    
                    Log.Warn(sb.ToString);
                    break;
            }
        }
        
        private static async void RestartTorchStoppedState()
        {
            if (Senxs_Utilities.Instance!.Torch.Config.Autostart)
            {
                Senxs_Utilities.Instance.Torch.Config.Autostart = false;
                Config!.RestartState = 1;
            }
            
            Status = 3;
            cts.Cancel();
            Senxs_Utilities.Instance.Save();
            int count = Config!.SaveAndShutDownNoRestartTime;
            
            while (count > 0)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll(Config.MessageToPlayers.Replace("{s}", count.ToString()), count == 1 ? 50000 : 60000);
                count--;
                await Task.Delay(60000);
            }
            RunCommandManager.ProcessCommand($"!restart");
        }

        private static void RestartTorchStoppedStateNOW()
        {
            StopServer();
            Status = 3;
            cts.Cancel();
            Config!.RestartState = 1;
            Senxs_Utilities.Instance?.Save();
            RunCommandManager.ProcessCommand($"!restart 1");
        }

        private static async void RestartTorchRunningState()
        {
            int count = Config!.SaveAndShutDownNoRestartTime;

            while (count > 0 && !CancelRestart)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll(Config.MessageToPlayers.Replace("{s}", count.ToString()), count == 1 ? 50000 : 60000);
                MyVisualScriptLogicProvider.SendChatMessage(Config.MessageToPlayers.Replace("{s}", count.ToString()), "Server Restart");
                count--;
                await Task.Delay(60000);
            }

            if (!CancelRestart)
            {
                CancelRestart = false;
                return;
            }
            
            StopServer();
            RunCommandManager.ProcessCommand($"!restart");
            Status = 3;
            cts.Cancel();
            Config!.RestartState = 2;
            Senxs_Utilities.Instance?.Save();
        }
        
        private static void RestartTorchRunningStateNOW()
        {
            Status = 3;
            cts.Cancel();
            Config!.RestartState = 2;
            StopServer();
            Senxs_Utilities.Instance?.Save();
            RunCommandManager.ProcessCommand($"!restart 1");
        }
        
        private static async Task SaveAndCloseTorch()
        {
            Status = 3;
            cts.Cancel();
            int count = Config!.SaveAndShutDownNoRestartTime;

            while (count > 0)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll(Config.MessageToPlayers.Replace("{s}", count.ToString()), 60000);
                count--;
                Thread.Sleep(60000);
            }

            RunCommandManager.ProcessCommand($"!save");
            await Task.Delay(5000);
            MySession.OnSaved += ShutDown;
            StopServer();
            ShutDown(false, "");
        }

        private static async Task SaveAndCloseTorchNOW()
        {
            Status = 3;
            cts.Cancel();
            Config!.RestartState = 2;
            Senxs_Utilities.Instance?.Save();
            RunCommandManager.ProcessCommand($"!save");
            await Task.Delay(5000);
            MySession.OnSaved += ShutDown;
            ShutDown(false, "");
        }

        private static async void ShutDown(bool arg1, string arg2)
        {
            MySession.OnSaved -= ShutDown;
            RunCommandManager.ProcessCommand($"!stop");
            while (Senxs_Utilities.IsOnline && !Senxs_Utilities.IsUnloaded)
            {
                await Task.Delay(100);
            }

            Log.Info("Session Offloaded, Server Stopped, Exiting now....");
            Environment.Exit(0);
        }
    }
}