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
        private Timer SuitConsumableRefillTimer { get; set; } = new (10000);
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
            if (!MyConfig.MasterSwitch) return;
            if (!MyConfig.AdminToolsEnable) return;
            
            List<IMyPlayer> players = new ();
            MyAPIGateway.Players.GetPlayers(players);
            for (int index = players.Count - 1; index >= 0; index--)
            {
                MySession.Static.Players.TryGetPlayerId(players[index].IdentityId, out MyPlayer.PlayerId playerId);
                MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player);

                if (!MyConfig.IsLobby)
                    if (!PlayerRefill.ContainsKey(player.Id.SteamId) && (int)players[index].PromoteLevel < 4)
                        continue;

                if (MyConfig.AdminRefillOxygen)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersOxygenLevel(players[index].Identity.IdentityId, 1);

                if (MyConfig.AdminRefillHydrogen)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(players[index].Identity.IdentityId, 1);

                if (MyConfig.AdminRefillEnergy)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersEnergyLevel(players[index].Identity.IdentityId, 1);
                
                if (MyConfig.AdminRefillHealth)
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersHealth(players[index].Identity.IdentityId, 100);
                
                if (players[index].Identity is MyIdentity mIdent)
                {
                    if (MyConfig.AdminResetRadiation)
                    {
                        // Prevent the radiation panel from disappearing and reappearing annoyingly while admin is working.
                        if (mIdent.Character.StatComp.Radiation.Value > 5f)
                            mIdent.Character.StatComp.Radiation.Value = 2f;
                    }
                    
                    if (MyConfig.AdminResetRadiationImmunity)
                        mIdent.Character.StatComp.RadiationImmunity.Value = 100f;
                    
                    if (MyConfig.AdminRefillFood)
                        mIdent.Character.StatComp.Food.Value = 100f;
                }
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