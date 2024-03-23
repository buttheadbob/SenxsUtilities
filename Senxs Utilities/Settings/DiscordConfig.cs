
using System.Collections.ObjectModel;
using S_Utilities.Utils;

namespace S_Utilities.Settings
{
    public partial class S_Config
    {
        private ObservableCollection<LogHandler> _LogHandlers = new ObservableCollection<LogHandler>();
        public ObservableCollection<LogHandler> LogHandlers { get => _LogHandlers; set => SetValue(ref _LogHandlers, value); }
    }
}