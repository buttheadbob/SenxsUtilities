using NLog;
using Torch.API.Managers;
using Torch.Commands;

namespace S_Utilities.Utils
{
    public static class RunCommandManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public static void ProcessCommand(string command)
        {
            if (!Senxs_Utilities.IsOnline)
            {
                _log.Error("Game server offline, cannot process that command!");
                return;
            }
            
            CommandManager? TorchCommandManager = Senxs_Utilities.Instance!.Torch.CurrentSession.Managers.GetManager<CommandManager>();

            if (TorchCommandManager is null)
            {
                _log.Error("TorchCommandManager is null, cannot process commands!");
                return;
            }

            TorchCommandManager.HandleCommandFromServer(command);
        }
    }
}