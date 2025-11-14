// Features/AppManager.cs
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class AppManager : IAgentFeature
    {
        public string Action => "app_list";

        public void Execute(JsonElement request, AgentController controller)
        {
            var apps = new List<string>();

            // Lấy từ Program Files
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            foreach (var dir in Directory.GetDirectories(programFiles))
                if (Directory.GetFiles(dir, "*.exe").Any()) apps.Add(Path.GetFileName(dir));

            foreach (var dir in Directory.GetDirectories(programFilesX86))
                if (Directory.GetFiles(dir, "*.exe").Any()) apps.Add(Path.GetFileName(dir));

            // Lấy từ Registry (Uninstall)
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    var displayName = subKey?.GetValue("DisplayName")?.ToString();
                    if (!string.IsNullOrEmpty(displayName))
                        apps.Add(displayName);
                }
            }

            controller.SendResponse("app_list", apps.Distinct().Take(50).ToArray());
        }
    }

    // Start/Stop App (có thể mở rộng)
    public static class AppControl
    {
        public static void Start(string name)
        {
            try { Process.Start(name); }
            catch { }
        }

        public static void Stop(string name)
        {
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name)))
                try { p.Kill(); } catch { }
        }
    }
}