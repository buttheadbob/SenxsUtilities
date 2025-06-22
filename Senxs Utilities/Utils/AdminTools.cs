using System;
using System.Collections.Generic;
using System.Timers;
using S_Utilities.Settings;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace S_Utilities.Utils
{
    public class AdminTools : IDisposable 
    {
        private Timer SuitConsumableRefillTimer { get; set; } = new (30000);
        private static S_Config? MyConfig => Senxs_Utilities.Config;
        private static Dictionary<ulong, DateTime> PlayerRefill { get; set; } = new();

        public AdminTools()
        {
            SuitConsumableRefillTimer.Elapsed += SuitConsumableRefillTimer_Elapsed;
            SuitConsumableRefillTimer.Start();
        }

        private void SuitConsumableRefillTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Senxs_Utilities.IsOnline) return;
            if(MyConfig is null) return;
            
            List<IMyPlayer> players = new ();
            MyAPIGateway.Players.GetPlayers(players);
            for (int index = players.Count - 1; index >= 0; index--)
            {
                MySession.Static.Players.TryGetPlayerId(players[index].IdentityId, out MyPlayer.PlayerId playerId);
                MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player);
                
                if (!MyConfig.IsLobby)
                    if (!PlayerRefill.ContainsKey(player.Id.SteamId) && (int)players[index].PromoteLevel < 4) continue;
                
                if (MyConfig.AdminRefillOxygen)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersOxygenLevel(players[index].Identity.IdentityId, 1);

                if (MyConfig.AdminRefillHydrogen)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(players[index].Identity.IdentityId, 1);

                if (MyConfig.AdminRefillEnergy)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersEnergyLevel(players[index].Identity.IdentityId, 1);
            }
        }
        
        public void AddPlayerToRefillList(ulong steamId, int minutes)
        {
            PlayerRefill[steamId] = DateTime.Now + TimeSpan.FromMinutes(minutes);
        }

        public void Dispose()
        {
            SuitConsumableRefillTimer.Elapsed -= SuitConsumableRefillTimer_Elapsed;
            SuitConsumableRefillTimer.Dispose();
        }
    }
}