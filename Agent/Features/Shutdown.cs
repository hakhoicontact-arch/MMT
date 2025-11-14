// Features/Shutdown.cs
using System.Diagnostics;
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class ShutdownFeature : IAgentFeature
    {
        public string Action => "shutdown";

        public void Execute(JsonElement request, AgentController controller)
        {
            var action = request.GetProperty("action").GetString();

            if (action == "shutdown")
            {
                Process.Start(new ProcessStartInfo("shutdown", "/s /t 0") { CreateNoWindow = true });
                controller.SendResponse("shutdown", "ok");
            }
            else if (action == "restart")
            {
                Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true });
                controller.SendResponse("restart", "ok");
            }
        }
    }
}