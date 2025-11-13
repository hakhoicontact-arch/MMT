// Features/Screenshot.cs
using System.Drawing;
using System.Drawing.Imaging;

public static class Screenshot
{
    public static async Task Capture(HubConnection connection)
    {
        var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

        using var stream = new MemoryStream();
        bmp.Save(stream, ImageFormat.Png);
        var bytes = stream.ToArray();

        // Gửi binary theo chunk
        int chunkSize = 1024 * 32; // 32KB
        await connection.SendAsync("SendToClient", Context.ConnectionId, new { binary_start = true });

        for (int i = 0; i < bytes.Length; i += chunkSize)
        {
            var chunk = bytes.Skip(i).Take(chunkSize).ToArray();
            await connection.SendAsync("SendToClient", Context.ConnectionId, new { binary_chunk = Convert.ToBase64String(chunk) });
        }

        await connection.SendAsync("SendToClient", Context.ConnectionId, new { binary_end = true });
    }
}