using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DowngradVPN
{
    public class ProcessManager
    {
        private static ProcessManager _instance;
        private static readonly object _lock = new object();
        private Process _process;

        private ProcessManager() { }

        public static ProcessManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ProcessManager();
                    }
                    return _instance;
                }
            }
        }

        public Process OurProcess
        {
            get
            {
                if (_process == null)
                {
                    _process = new Process();
                }
                return _process;
            }
        }

        public void InitializeProcess()
        {
            _process = new Process();
        }

        public async void KillProcess()
        {
            if (_process != null && !_process.HasExited)
            {
                await ClientManager.ReleaseClientAsync();
                _process.Kill();
                _process.WaitForExit();
            }
        }

        public bool IsProcessRunning()
        {
            return _process != null && !_process.HasExited;
        }
    }
}
