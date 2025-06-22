using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace S_Utilities.Commands
{
    [Category("sutils")]
    public class Senxs_UtilitiesCommands : CommandModule
    {
        public Senxs_Utilities Plugin => (Senxs_Utilities)Context.Plugin;
        
        [Command("refillSuit", "Refills the suit of the player with Hydrogen, Oxygen, and Energy for the amount in minutes given, 30 minutes if no time given, or until next restart, which ever comes first.")]
        [Permission(MyPromoteLevel.Admin)]
        public void refillplayer(ulong steamId, int minutes = 30)
        {
            Senxs_Utilities.Instance?._adminTools.AddPlayerToRefillList(steamId, minutes);
        }

    }
}
