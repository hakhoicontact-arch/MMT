// Features/Shutdown.cs
public static class Shutdown
{
    public static async Task Execute(string mode)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = mode == "shutdown" ? "/s /t 0" : "/r /t 0",
            UseShellExecute = false
        };
        Process.Start(psi);
        await Task.Delay(1000);
    }
}