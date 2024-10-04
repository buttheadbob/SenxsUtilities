using System;
using System.Collections.Generic;
using Torch;

namespace S_Utilities.Settings
{
    public partial class S_Config : ViewModel
    {
        private bool _MasterSwitch = true;
        public bool MasterSwitch { get => _MasterSwitch; set => SetValue(ref _MasterSwitch, value); }
        
        private bool _Log2Discord = true;
        public bool Log2Discord { get => _Log2Discord; set => SetValue(ref _Log2Discord, value); }
        
        private bool _LocalCommandServer = false;
        public bool LocalCommandServer { get => _LocalCommandServer; set => SetValue(ref _LocalCommandServer, value); }
        
        private bool _EjectorLimiter = false;
        public bool EjectorLimiter { get => _EjectorLimiter; set => SetValue(ref _EjectorLimiter, value); }
    }
}
