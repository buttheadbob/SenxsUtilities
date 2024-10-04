namespace S_Utilities.Settings
{
    public partial class S_Config
    {
        // Settings
        private bool _startPipeOnPluginLoad;
        public bool StartPipeOnPluginLoad { get => _startPipeOnPluginLoad; set => SetValue(ref _startPipeOnPluginLoad, value); }
        
        private bool _startPipeOnServerStart;
        public bool StartPipeOnServerStart { get => _startPipeOnServerStart; set => SetValue(ref _startPipeOnServerStart, value); }
        
        private bool _stopPipeOnServerStop;
        public bool StopPipeOnServerStop { get => _stopPipeOnServerStop; set => SetValue(ref _stopPipeOnServerStop, value); }
        
        // Commands allowed
        private bool _restartTorchStoppedState;
        public bool RestartTorchStoppedState { get => _restartTorchStoppedState; set => SetValue(ref _restartTorchStoppedState, value); }
        
        private bool _restartTorchRunningState;
        public bool RestartTorchRunningState { get => _restartTorchRunningState; set => SetValue(ref _restartTorchRunningState, value); }
        
        private bool _SaveAndCloseTorch;
        public bool SaveAndCloseTorch { get => _SaveAndCloseTorch; set => SetValue(ref _SaveAndCloseTorch, value); }
        
        private int _saveAndShutDownNoRestartTime = 5;
        public int SaveAndShutDownNoRestartTime { get => _saveAndShutDownNoRestartTime; set => SetValue(ref _saveAndShutDownNoRestartTime, value); }
        
        private string _messageToPlayers = "Server is rebooting for unplanned maintenance in {s} minutes.";
        public string MessageToPlayers { get => _messageToPlayers; set => SetValue(ref _messageToPlayers, value); }
        
        // Restart State
        private byte _restartState = 0; // 0 == Normal, 1 == RestartingStopped, 2 == RestartingRunning
        public byte RestartState { get => _restartState; set => _restartState = value; }
    }
}