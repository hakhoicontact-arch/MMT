// Features/Screenshot.cs
using System.Windows.Forms;  // THÊM DÒNG NÀY
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class ScreenshotFeature : IAgentFeature
    {
        public string Action => "screenshot";

        public void Execute(JsonElement request, AgentController controller)
        {
            try
            {
                using var bmp = new Bitmap(
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height,
                    PixelFormat.Format32bppArgb);

                using (var graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(
                        Screen.PrimaryScreen.Bounds.X,
                        Screen.PrimaryScreen.Bounds.Y,
                        0, 0,
                        bmp.Size,
                        CopyPixelOperation.SourceCopy);
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Jpeg);
                var bytes = ms.ToArray();

                const int chunkSize = 64 * 1024; // 64KB
                controller.SendBinaryStart("screenshot");

                for (int i = 0; i < bytes.Length; i += chunkSize)
                {
                    var chunk = bytes.Skip(i).Take(chunkSize).ToArray();
                    controller.SendBinaryChunk(chunk);
                    Thread.Sleep(10);
                }

                controller.SendBinaryEnd();
            }
            catch (Exception ex)
            {
                controller.SendResponse("error", ex.Message);
            }
        }
    }
}