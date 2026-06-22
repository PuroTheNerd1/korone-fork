using System.Diagnostics;

namespace Korone.RccServiceArbiter.Processes;

public sealed class RccProcessLauncher : IRccProcessLauncher
{
    public IManagedProcess Start(string fileName, string arguments, string? workingDirectory = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? AppContext.BaseDirectory
                    : workingDirectory,
            },
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Failed to start process {fileName}");
        }

        return new ManagedProcess(process);
    }

    private sealed class ManagedProcess : IManagedProcess
    {
        private readonly Process _process;

        public ManagedProcess(Process process)
        {
            _process = process;
        }

        public int? Id => _process.Id;

        public bool HasExited
        {
            get
            {
                try
                {
                    return _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }
        }

        public void KillTree()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true, );
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
