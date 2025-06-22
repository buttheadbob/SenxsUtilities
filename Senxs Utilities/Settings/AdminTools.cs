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
        
        private bool _isLobby = false;
        public bool IsLobby { get => _isLobby; set => SetValue(ref _isLobby, value); }
    }
}