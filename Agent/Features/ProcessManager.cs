// Features/ProcessManager.cs
using System.Diagnostics;

public static class ProcessManager
{
    public static async Task ListProcesses(HubConnection connection)
    {
        var processes = Process.GetProcesses()
            .Select(p => new
            {
                pid = p.Id,
                name = p.ProcessName,
                cpu = Math.Round(p.TotalProcessorTime.TotalMilliseconds / 1000, 1),
                mem = $"{p.WorkingSet64 / 1024 / 1024}MB"
            })
            .ToArray();

        await connection.SendAsync("SendToClient", Context.ConnectionId, new
        {
            response = processes
        });
    }
}