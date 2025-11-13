// Features/AppManager.cs
using System.Diagnostics;

public static class AppManager
{
    public static async Task ListApps(HubConnection connection)
    {
        var apps = Process.GetProcesses()
            .Select(p => p.ProcessName)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        await connection.SendAsync("SendToClient", Context.ConnectionId, new
        {
            response = apps
        });
    }
}