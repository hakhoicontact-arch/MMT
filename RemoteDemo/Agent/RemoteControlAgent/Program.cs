// Program.cs - Agent chạy WebSocket Server (demo cơ bản)
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Client đã kết nối!");

        while (true)
        {
            var buffer = new byte[1024 * 4];
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) break;

            var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var json = JsonSerializer.Deserialize<JsonElement>(msg);
            var action = json.GetProperty("action").GetString();

            switch (action)
            {
                case "app_list": await SendApps(ws); break;
                case "process_list": await SendProcesses(ws); break;
                case "screenshot": await SendScreenshot(ws); break;
                case "keylogger_start": await StartKeylogger(ws); break;
                case "keylogger_stop": await StopKeylogger(); break;
                case "shutdown": await RunCmd("shutdown /s /t 0"); await Send(ws, new { response = "ok" }); break;
                case "restart": await RunCmd("shutdown /r /t 0"); await Send(ws, new { response = "ok" }); break;
                case "webcam_on": await StartWebcam(ws); break;
                case "webcam_off": await StopWebcam(); break;
            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run("http://127.0.0.1:8080");

// === HÀM HỖ TRỢ ===
static async Task Send(WebSocket ws, object obj)
{
    var json = JsonSerializer.Serialize(obj);
    var bytes = Encoding.UTF8.GetBytes(json);
    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
}

static async Task SendBinary(WebSocket ws, byte[] data)
{
    const int chunk = 64 * 1024;
    for (int i = 0; i < data.Length; i += chunk)
    {
        var part = data.Skip(i).Take(chunk).ToArray();
        await ws.SendAsync(part, WebSocketMessageType.Binary, i + chunk >= data.Length, CancellationToken.None);
    }
}

static async Task SendApps(WebSocket ws)
{
    var apps = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
        .Concat(Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))
        .Select(d => Path.GetFileName(d))
        .Where(n => !string.IsNullOrEmpty(n))
        .Take(20).ToArray();
    await Send(ws, new { action = "app_list", response = apps });
}

static async Task SendProcesses(WebSocket ws)
{
    var procs = System.Diagnostics.Process.GetProcesses()
        .Take(30)
        .Select(p => new
        {
            pid = p.Id,
            name = p.ProcessName + ".exe",
            cpu = "N/A",
            mem = FormatBytes(p.WorkingSet64)
        }).ToArray();
    await Send(ws, new { action = "process_list", response = procs });
}

static async Task SendScreenshot(WebSocket ws)
{
    var width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
    var height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

    using var bmp = new System.Drawing.Bitmap(width, height);
    using (var g = System.Drawing.Graphics.FromImage(bmp))
        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

    using var ms = new MemoryStream();
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
    await SendBinary(ws, ms.ToArray());
}

static Task RunCmd(string cmd)
{
    return Task.Run(() =>
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {cmd}",
            CreateNoWindow = true
        });
    });
}

static string FormatBytes(long b)
{
    string[] s = { "B", "KB", "MB", "GB" };
    int i = 0; double d = b;
    while (d > 1024 && i < s.Length - 1) { d /= 1024; i++; }
    return $"{d:0.##}{s[i]}";
}

// Keylogger & Webcam giả lập
CancellationTokenSource? cts;
static async Task StartKeylogger(WebSocket ws)
{
    cts = new();
    _ = Task.Run(async () =>
    {
        var keys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(300);
            var k = keys[Random.Shared.Next(keys.Length)].ToString();
            await Send(ws, new { update = k });
        }
    });
    await Send(ws, new { response = "started" });
}

static Task StopKeylogger() { cts?.Cancel(); return Task.CompletedTask; }

static Task StartWebcam(WebSocket ws)
{
    cts = new();
    _ = Task.Run(async () =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            using var bmp = new System.Drawing.Bitmap(320, 240);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.FillRectangle(System.Drawing.Brushes.DarkBlue, 0, 0, 320, 240);
                g.DrawString("WEBCAM DEMO", new System.Drawing.Font("Arial", 18, System.Drawing.FontStyle.Bold), System.Drawing.Brushes.White, 40, 100);
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            await SendBinary(ws, ms.ToArray());
            await Task.Delay(200);
        }
    });
    return Task.CompletedTask;
}

static Task StopWebcam() { cts?.Cancel(); return Task.CompletedTask; }