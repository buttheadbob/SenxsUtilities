namespace S_Utilities.Settings
{
    public partial class S_Config
    {
        private bool _adminToolsEnable = true;
        public bool AdminToolsEnable { get => _adminToolsEnable; set => SetValue(ref _adminToolsEnable, value); }
        
        private bool _adminRefillOxygen = true;
        public bool AdminRefillOxygen { get => _adminRefillOxygen; set => SetValue(ref _adminRefillOxygen, value); }
        
        private bool _adminRefillHydrogen = true;
        public bool AdminRefillHydrogen { get => _adminRefillHydrogen; set => SetValue(ref _adminRefillHydrogen, value); }
        
        private bool _adminRefillEnergy = true;
        public bool AdminRefillEnergy { get => _adminRefillEnergy; set => SetValue(ref _adminRefillEnergy, value); }
        
        private bool _adminRefillHealth = true;
        public bool AdminRefillHealth { get => _adminRefillHealth; set => SetValue(ref _adminRefillHealth, value); }
        
        private bool _adminRefillFood = true;
        public bool AdminRefillFood { get => _adminRefillFood; set => SetValue(ref _adminRefillFood, value); }
        
        private bool _adminResetRadiation = true;
        public bool AdminResetRadiation { get => _adminResetRadiation; set => SetValue(ref _adminResetRadiation, value); }
        
        private bool _adminResetRadiationImmunity = true;
        public bool AdminResetRadiationImmunity { get => _adminResetRadiationImmunity; set => SetValue(ref _adminResetRadiationImmunity, value); }

        private bool _isLobby = false;
        public bool IsLobby { get => _isLobby; set => SetValue(ref _isLobby, value); }
    }
}