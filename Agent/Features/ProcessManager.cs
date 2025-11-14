// Features/ProcessManager.cs
using System.Diagnostics;
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class ProcessManager : IAgentFeature
    {
        public string Action => "process_list";

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int i = 0;
            double dbl = bytes;
            while (dbl >= 1024 && i < suffixes.Length - 1)
            {
                dbl /= 1024;
                i++;
            }
            return $"{dbl:0.##} {suffixes[i]}";
        }

        public void Execute(JsonElement request, AgentController controller)
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .Take(100)
                .Select(p => new
                {
                    pid = p.Id,
                    name = p.ProcessName + ".exe",
                    cpu = Math.Round(p.TotalProcessorTime.TotalMilliseconds / 1000, 1) + "s",
                    mem = FormatBytes(p.WorkingSet64)
                }).ToArray();

            controller.SendResponse("process_list", processes);
        }
    }

    public static class ProcessControl
    {
        public static void Start(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch { }
        }

        public static void Kill(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                p.Kill();
            }
            catch { }
        }

    
}
}
